using Microsoft.Extensions.DependencyInjection;
using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Infrastructure.Users;
using LiveEventService.Infrastructure.Events;
using LiveEventService.Infrastructure.Registrations;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using LiveEventService.Core.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MediatR;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;
using Amazon.S3;
using Amazon;
using Amazon.SimpleNotificationService;

namespace LiveEventService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, bool isTesting = false)
    {
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

        // Register repository for EventRegistration with navigation safety
        services.AddScoped<IRepository<EventRegistration>, EventRegistrationRepository>();
        
        // Register domain event dispatcher
        // Note: The actual implementation is in the Application layer
        // This will be registered by the Application layer's DI configuration

        // Add AWS Cognito authentication
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://cognito-idp.{configuration["AWS:Region"]}.amazonaws.com/{configuration["AWS:UserPoolId"]}";
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://cognito-idp.{configuration["AWS:Region"]}.amazonaws.com/{configuration["AWS:UserPoolId"]}",
                ValidateLifetime = true,
                LifetimeValidator = (_, expires, _, _) => expires > DateTime.UtcNow,
                ValidateAudience = false,
                RoleClaimType = "cognito:groups"
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(RoleNames.Admin, policy => 
                policy.RequireRole(RoleNames.Admin));
            options.AddPolicy(RoleNames.Participant, policy => 
                policy.RequireRole(RoleNames.Participant));
        });

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
                    tags: new[] { "db", "rds", "postgresql" });
            }
        }
        
        healthChecksBuilder.AddCheck("AWS Cognito", () =>
            {
                // Simple check for Cognito config presence
                var region = configuration["AWS:Region"]!;
                var userPoolId = configuration["AWS:UserPoolId"]!;
                return !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(userPoolId)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Cognito config missing");
            }, tags: new[] { "aws", "cognito" });

        // Configure Amazon S3 client (LocalStack-aware) and health check
        var awsRegion = configuration["AWS:Region"] ?? "us-east-1";
        var s3BucketName = configuration["AWS:S3BucketName"];
        var serviceUrl = configuration["AWS:ServiceURL"]; // when set, points to LocalStack

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

        if (!string.IsNullOrWhiteSpace(s3BucketName))
        {
            healthChecksBuilder.AddCheck<HealthChecks.S3BucketHealthCheck>("AWS S3", tags: new[] { "aws", "s3", "ready" });
        }

        // Add CORS policy
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                string[] allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        // Register outbox processor background service (disabled in Testing)
        if (!isTesting)
        {
            services.AddHostedService<OutboxProcessorBackgroundService>();
        }

        return services;
    }
}
