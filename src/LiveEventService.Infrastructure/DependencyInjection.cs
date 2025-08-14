using Microsoft.Extensions.DependencyInjection;
using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Infrastructure.Users;
using LiveEventService.Infrastructure.Events;
using LiveEventService.Infrastructure.Registrations;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using LiveEventService.Infrastructure.Configuration;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using Amazon.S3;
using Amazon;
using Amazon.SimpleNotificationService;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure.Telemetry;
using Amazon.SQS;
using LiveEventService.Infrastructure.Messaging;
// using Microsoft.Extensions.Http.Resilience;

namespace LiveEventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isTesting = false)
    {
        services.Configure<AwsOptions>(configuration.GetSection("AWS"));
        // Shared HttpClient for health and infrastructure probes
        services.AddHttpClient("HealthChecks");
        // Configure Npgsql to handle DateTime properly
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

        // Add DbContext - only configure if not already configured (for test scenarios)
        if (!services.Any(s => s.ServiceType == typeof(DbContextOptions<LiveEventDbContext>)))
        {
            if (isTesting == false)
            {
                services.AddDbContext<LiveEventDbContext>(options =>
                {
                    options.UseNpgsql(
                        configuration.GetConnectionString("DefaultConnection"),
                        npgsqlOptions =>
                        {
                            // Ensure all migrations are tracked in this assembly and a single history table
                            npgsqlOptions.MigrationsAssembly(typeof(LiveEventDbContext).Assembly.FullName);
                            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
                            // Enable resilient execution for transient faults
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorCodesToAdd: null);
                            // Set a sensible command timeout for long-running operations
                            npgsqlOptions.CommandTimeout(30);
                        });
                });
            }
        }

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        // Field encryption service (no-op unless keys configured)
        services.AddSingleton<Security.IFieldEncryptionService, Security.FieldEncryptionService>();


        // Register repository for EventRegistration with navigation safety
        services.AddScoped<IRepository<EventRegistration>, EventRegistrationRepository>();

        // Register domain event dispatcher
        // Note: The actual implementation is in the Application layer
        // This will be registered by the Application layer's DI configuration

        // Authentication and authorization are registered in the API project to keep Infrastructure framework-agnostic.

        // Add basic health checks
        var healthChecksBuilder = services.AddHealthChecks();

        // Add PostgreSQL health check only if not in testing mode and connection string is available
        if (!isTesting)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecksBuilder.AddNpgSql(
                    connectionString,
                    name: "PostgreSQL (RDS)",
                    tags: new[] { "db", "rds", "postgresql", "ready" });
            }
        }

        // Cognito metadata check (skip in Testing)
        healthChecksBuilder.AddCheck<HealthChecks.CognitoMetadataHealthCheck>("AWS Cognito", tags: new[] { "aws", "cognito" });

        // Configure Amazon S3 client (LocalStack-aware) and health check
        var awsOptions = configuration.GetSection("AWS").Get<AwsOptions>() ?? new AwsOptions();
        var awsRegion = awsOptions.Region ?? "us-east-1";
        var s3BucketName = awsOptions.S3BucketName;
        var serviceUrl = awsOptions.ServiceURL; // when set, points to LocalStack

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion),
            };

            if (!string.IsNullOrWhiteSpace(serviceUrl))
            {
                // Local development with LocalStack
                config.ServiceURL = serviceUrl;
                config.ForcePathStyle = true; // required for LocalStack
                config.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }

            return new AmazonS3Client(config);
        });

        // Configure Amazon SNS (LocalStack-aware)
        services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
        {
            var cfg = new AmazonSimpleNotificationServiceConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion),
            };
            if (!string.IsNullOrWhiteSpace(serviceUrl))
            {
                cfg.ServiceURL = serviceUrl;
                cfg.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            return new AmazonSimpleNotificationServiceClient(cfg);
        });

        // Configure Amazon SQS (LocalStack-aware)
        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var cfg = new AmazonSQSConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(awsRegion),
            };
            if (!string.IsNullOrWhiteSpace(serviceUrl))
            {
                cfg.ServiceURL = serviceUrl;
                cfg.UseHttp = serviceUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
            }
            return new AmazonSQSClient(cfg);
        });

        // Optionally replace in-memory queue with SQS producer based on configuration
        var useSqs = awsOptions.Sqs.UseSqsForDomainEvents;
        if (useSqs)
        {
            services.AddSingleton<IMessageQueue, SqsMessageQueue>();
        }

        if (!string.IsNullOrWhiteSpace(s3BucketName))
        {
            healthChecksBuilder.AddCheck<HealthChecks.S3BucketHealthCheck>("AWS S3", tags: new[] { "aws", "s3", "ready" });
        }

        // Add SQS readiness check when SQS is enabled
        if (awsOptions.Sqs.UseSqsForDomainEvents)
        {
            healthChecksBuilder.AddCheck<HealthChecks.SqsHealthCheck>("AWS SQS", tags: new[] { "aws", "sqs", "ready" });
        }

        // Add Redis health check if a multiplexer is registered
        healthChecksBuilder.AddCheck<HealthChecks.RedisHealthCheck>("Redis", tags: new[] { "redis", "ready" });

        // CORS is configured in the API project. Do not configure it here to avoid duplication.

        // Register outbox processor background service (disabled in Testing)
        if (!isTesting)
        {
            services.AddHostedService<OutboxProcessorBackgroundService>();
            // Data retention job (disabled by default; enable via configuration)
            services.Configure<RetentionOptions>(configuration.GetSection("Security:Retention"));
            var retentionEnabled = configuration.GetValue<bool?>("Security:Retention:Enabled") ?? false;
            if (retentionEnabled)
            {
                services.AddHostedService<RetentionBackgroundService>();
            }
        }

        // Override metrics recorder with infrastructure implementation
        services.AddSingleton<IMetricRecorder, MetricRecorder>();

        return services;
    }
}
