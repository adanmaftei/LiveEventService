using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using LiveEventService.Infrastructure.Data;
using Npgsql;
using LiveEventService.Core.Common;

namespace LiveEventService.IntegrationTests.Infrastructure.Sqs;

public class SqsTestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SemaphoreSlim s_containerStartLock = new(1, 1);

    private static PostgreSqlContainer? s_postgresContainer;
    private static LocalStackContainer? s_localStackContainer;
    private static bool s_containersStarted;

    private readonly string _databaseName = $"LiveEventSqsTestDB_{Guid.NewGuid():N}";
    private string? _databaseConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace EF DbContext with isolated DB per test factory
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LiveEventDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<LiveEventDbContext>(options =>
            {
                options.UseNpgsql(_databaseConnectionString!);
            });

            // Ensure schema exists in the isolated DB
            var sp = services.BuildServiceProvider();
            using (var scope = sp.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
                db.Database.EnsureCreated();

                // Seed required test users so CreateEvent (organizer check) passes
                var adminExists = db.Users.Any(u => u.IdentityId == "admin-user");
                if (!adminExists)
                {
                    var adminUser = TestDataBuilder.CreateUser("admin-user", "admin@test.com", "Admin", "User");
                    db.Users.Add(adminUser);
                }

                var participantExists = db.Users.Any(u => u.IdentityId == "participant-user");
                if (!participantExists)
                {
                    var participantUser = TestDataBuilder.CreateUser("participant-user", "participant@test.com", "Participant", "User");
                    db.Users.Add(participantUser);
                }

                // Seed a third participant for multi-waitlist scenarios
                var user3Exists = db.Users.Any(u => u.IdentityId == "user3");
                if (!user3Exists)
                {
                    var thirdUser = TestDataBuilder.CreateUser("user3", "user3@test.com", "Third", "User");
                    db.Users.Add(thirdUser);
                }

                db.SaveChanges();
            }

            // Enable SQS producer and disable in-proc background workers
            services.PostConfigure<Dictionary<string, string>>(config =>
            {
                config["AWS:SQS:UseSqsForDomainEvents"] = "true";
                config["AWS:SQS:QueueName"] = "liveevent-domain-events";
                config["Performance:BackgroundProcessing:UseInProcess"] = "false";
            });

            // Add minimal in-process SQS worker for tests
            services.AddHostedService<TestSqsWorker>();

            // Replace real auth with test auth so Authorization: Test <user|role|email> is honored
            var authenticationServices = services.Where(s =>
                s.ServiceType.FullName != null &&
                (s.ServiceType.FullName.Contains("Authentication") ||
                 s.ServiceType.FullName.Contains("JwtBearer"))).ToList();

            foreach (var svc in authenticationServices)
            {
                services.Remove(svc);
            }

            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole(RoleNames.Admin));
                options.AddPolicy("Participant", policy => policy.RequireRole(RoleNames.Participant));
            });
        });

        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _databaseConnectionString!,
                ["AWS:ServiceURL"] = s_localStackContainer!.GetConnectionString(),
                ["AWS:Region"] = "us-east-1",
                ["AWS:UserPoolId"] = "us-east-1_000000001",
                ["AWS:S3BucketName"] = "test-bucket",
                ["AWS:SQS:UseSqsForDomainEvents"] = "true",
                ["AWS:SQS:QueueName"] = "liveevent-domain-events",
                ["Performance:BackgroundProcessing:UseInProcess"] = "false"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await s_containerStartLock.WaitAsync();
        try
        {
            if (!s_containersStarted)
            {
                s_postgresContainer ??= new PostgreSqlBuilder()
                    .WithImage("postgres:14")
                    .WithDatabase("postgres")
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithCleanUp(true)
                    .WithWaitStrategy(Wait.ForUnixContainer()
                        .UntilPortIsAvailable(5432)
                        .UntilCommandIsCompleted("pg_isready -U postgres"))
                    .Build();

                s_localStackContainer ??= new LocalStackBuilder()
                    .WithImage("localstack/localstack:3.0")
                    .WithEnvironment("SERVICES", "cognito-idp,s3,xray,cloudwatch,logs,sqs")
                    .WithEnvironment("DEFAULT_REGION", "us-east-1")
                    .WithEnvironment("AWS_DEFAULT_REGION", "us-east-1")
                    .WithEnvironment("AWS_ACCESS_KEY_ID", "test")
                    .WithEnvironment("AWS_SECRET_ACCESS_KEY", "test")
                    .WithEnvironment("DEBUG", "1")
                    .WithEnvironment("PERSISTENCE", "0")
                    .WithPortBinding(4566, true)
                    .WithCleanUp(true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4566))
                    .Build();

                await s_postgresContainer.StartAsync();
                await s_localStackContainer.StartAsync();
                await ProvisionSqsAsync();
                s_containersStarted = true;
            }

            // Create isolated database
            var serverConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer!.GetConnectionString())
            { Database = "postgres" };
            await using (var conn = new NpgsqlConnection(serverConnBuilder.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", conn);
                await cmd.ExecuteNonQueryAsync();
            }

            var dbConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer.GetConnectionString())
            { Database = _databaseName };
            _databaseConnectionString = dbConnBuilder.ConnectionString;
        }
        finally
        {
            s_containerStartLock.Release();
        }
    }

    public new async Task DisposeAsync()
    {
        try
        {
            var serverConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer!.GetConnectionString())
            { Database = "postgres" };
            await using var conn = new NpgsqlConnection(serverConnBuilder.ConnectionString);
            await conn.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", conn);
            await drop.ExecuteNonQueryAsync();
        }
        catch { }

        await base.DisposeAsync();
    }

    private static async Task ProvisionSqsAsync()
    {
        static async Task<string> ExecAsync(string[] cmd)
        {
            var result = await s_localStackContainer!.ExecAsync(cmd);
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"LocalStack exec failed: {string.Join(' ', cmd)}\n{result.Stderr}");
            }
            return result.Stdout.Trim();
        }

        // Create DLQ
        await ExecAsync(new[] { "awslocal", "sqs", "create-queue", "--queue-name", "liveevent-domain-events-dlq" });
        // Resolve DLQ ARN
        var dlqUrl = await ExecAsync(new[] { "awslocal", "sqs", "get-queue-url", "--queue-name", "liveevent-domain-events-dlq", "--query", "QueueUrl", "--output", "text" });
        var dlqArn = await ExecAsync(new[] { "awslocal", "sqs", "get-queue-attributes", "--queue-url", dlqUrl, "--attribute-names", "QueueArn", "--query", "Attributes.QueueArn", "--output", "text" });
        var redrivePolicy = JsonSerializer.Serialize(new { deadLetterTargetArn = dlqArn, maxReceiveCount = "5" });
        // Create main queue with redrive policy
        // Quote the map value so AWS CLI parses JSON correctly without shell mediation
        await ExecAsync(new[] { "awslocal", "sqs", "create-queue", "--queue-name", "liveevent-domain-events", "--attributes", $"RedrivePolicy='{redrivePolicy}'" });
    }

    private sealed class TestSqsWorker : Microsoft.Extensions.Hosting.BackgroundService
    {
        private readonly ILogger<TestSqsWorker> _logger;
        private readonly IServiceProvider _services;
        private readonly IAmazonSQS _sqs;
        private string _queueUrl = string.Empty;

        public TestSqsWorker(ILogger<TestSqsWorker> logger, IServiceProvider services, IConfiguration configuration)
        {
            _logger = logger;
            _services = services;

            // Use default LocalStack credentials; some SDK versions expect empty credentials with local ServiceURL
            // Use static, known LocalStack credentials; some SDKs require explicit region name
            var creds = new BasicAWSCredentials("test", "test");
            var clientCfg = new AmazonSQSConfig
            {
                ServiceURL = s_localStackContainer!.GetConnectionString(),
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            };
            _sqs = new AmazonSQSClient(creds, clientCfg);
            var queueName = configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";
            for (var i = 0; i < 10 && string.IsNullOrEmpty(_queueUrl); i++)
            {
                try
                {
                    _queueUrl = _sqs.GetQueueUrlAsync(queueName).GetAwaiter().GetResult().QueueUrl;
                }
                catch
                {
                    Thread.Sleep(300);
                }
            }
            if (string.IsNullOrEmpty(_queueUrl))
            {
                // Fallback: resolve by listing queues
                var list = _sqs.ListQueuesAsync(queueName).GetAwaiter().GetResult();
                _queueUrl = list.QueueUrls.FirstOrDefault() ?? string.Empty;
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Test SQS worker started");
            while (!stoppingToken.IsCancellationRequested)
            {
                var resp = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 2,
                    VisibilityTimeout = 20
                }, stoppingToken);

                foreach (var msg in resp.Messages)
                {
                    var handled = await HandleAsync(msg, stoppingToken);
                    if (handled)
                    {
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                }
            }
        }

        private async Task<bool> HandleAsync(Message msg, CancellationToken ct)
        {
            try
            {
                var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
                var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(msg.Body, options);
                if (envelope is null || string.IsNullOrWhiteSpace(envelope.EventType))
                {
                    _logger.LogWarning("Invalid message format");
                    return true;
                }

                var eventType = Type.GetType(envelope.EventType, throwOnError: false);
                if (eventType == null || !typeof(DomainEvent).IsAssignableFrom(eventType))
                {
                    _logger.LogWarning("Unknown event type: {Type}", envelope.EventType);
                    return true;
                }

                var domainEvent = (DomainEvent?)JsonSerializer.Deserialize(envelope.Payload, eventType, options);
                if (domainEvent == null)
                {
                    _logger.LogWarning("Failed to deserialize payload for type {Type}", envelope.EventType);
                    return true;
                }

                using var scope = _services.CreateScope();
                var processors = scope.ServiceProvider.GetServices<IDomainEventProcessor>();
                var processor = processors.FirstOrDefault(p => p.CanProcess(eventType));
                if (processor == null)
                {
                    _logger.LogWarning("No processor found for event type {Type}", eventType.Name);
                    return true;
                }

                await processor.ProcessAsync(domainEvent, ct);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling test SQS message");
            }
            return false;
        }

        private sealed class DomainEventEnvelope
        {
            public string EventType { get; set; } = string.Empty;
            public string Payload { get; set; } = string.Empty;
        }
    }
}


