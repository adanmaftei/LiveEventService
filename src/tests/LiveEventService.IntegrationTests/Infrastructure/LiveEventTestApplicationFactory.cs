using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.LocalStack;
using Testcontainers.PostgreSql;
using DotNet.Testcontainers.Builders;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Core.Common;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Options;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;
using Npgsql;

namespace LiveEventService.IntegrationTests.Infrastructure;

public class LiveEventTestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly SemaphoreSlim s_containerStartLock = new(1, 1);

    // Shared containers across all test classes to avoid start/stop races and speed up parallel runs
    private static PostgreSqlContainer? s_postgresContainer;
    private static LocalStackContainer? s_localStackContainer;
    private static bool s_containersStarted;

    // Per-factory (per-class) isolated database name and connection string
    private readonly string _databaseName = $"LiveEventTestDB_{Guid.NewGuid():N}";
    private string? _databaseConnectionString;

    public LiveEventTestApplicationFactory() { }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace audit logger with in-memory implementation for assertions
            var existingAudit = services.FirstOrDefault(s => s.ServiceType.FullName == typeof(IAuditLogger).FullName);
            if (existingAudit != null)
            {
                services.Remove(existingAudit);
            }
            services.AddSingleton<IAuditLogger, InMemoryAuditLogger>();
            // Remove the real database context
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LiveEventDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context (per-class isolated database)
            services.AddDbContext<LiveEventDbContext>(options =>
            {
                options.UseNpgsql(
                    _databaseConnectionString!,
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(30);
                    });
            });

            // Remove existing authentication services
            var authenticationServices = services.Where(s =>
                s.ServiceType.FullName != null &&
                (s.ServiceType.FullName.Contains("Authentication") ||
                 s.ServiceType.FullName.Contains("JwtBearer"))).ToList();

            foreach (var service in authenticationServices)
            {
                services.Remove(service);
            }

            // Replace authentication with test authentication
            services.AddAuthentication("Test")
                .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

            // Configure authorization policies for testing
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireRole(RoleNames.Admin));
                options.AddPolicy("Participant", policy => policy.RequireRole(RoleNames.Participant));
                // Remove the default policy that requires authentication for all endpoints
                // This allows public endpoints like /api/events to be accessed without authentication
            });

            // Configure GraphQL for testing with detailed error messages and introspection
            services.Configure<GraphQLServerOptions>(options =>
            {
                options.Tool.Enable = false; // Disable Banana Cake Pop in tests
                options.EnableSchemaRequests = true; // Enable introspection for debugging
                options.EnableGetRequests = true; // Enable GET requests
            });

            // Note: GraphQL introspection is disabled by default in HotChocolate 13+
            // We cannot easily enable it for tests without significant configuration changes

            // Configure GraphQL server to include exception details for debugging
            services.PostConfigure<RequestExecutorOptions>(options =>
            {
                options.IncludeExceptionDetails = true;
            });

            // Configure HotChocolate to properly map currentUserId from test claims and enable introspection for testing
            services.AddGraphQL()
            .AddHttpRequestInterceptor<TestGraphQLInterceptor>()
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = true;
            })
            .ModifyOptions(opt =>
            {
                opt.StrictValidation = false; // Allow introspection queries
            });

            // Keep worker out-of-process for integration tests to avoid Docker dependency flakiness

            // Override AWS configuration for LocalStack (shared across tests)
            services.Configure<Dictionary<string, string>>(config =>
            {
                config["AWS:ServiceURL"] = s_localStackContainer!.GetConnectionString();
                config["AWS:Region"] = "us-east-1";
                config["AWS:UserPoolId"] = "us-east-1_000000001";
                config["AWS:S3BucketName"] = "test-bucket";
            });

            // Ensure the per-class database schema is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
            dbContext.Database.EnsureCreated();
        });

        // Set test environment
        builder.UseEnvironment("Testing");

        // Configure connection string for test (per-class DB)
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
                    .WithDatabase("postgres") // connect to server DB; we'll create per-class DBs manually
                    .WithUsername("postgres")
                    .WithPassword("postgres")
                    .WithCleanUp(true)
                    .WithWaitStrategy(Wait.ForUnixContainer()
                        .UntilPortIsAvailable(5432)
                        .UntilCommandIsCompleted("pg_isready -U postgres"))
                    .Build();

                s_localStackContainer ??= new LocalStackBuilder()
                    .WithImage("localstack/localstack:latest")
                    .WithEnvironment("SERVICES", "cognito-idp,s3,xray,cloudwatch,logs")
                    .WithEnvironment("DEBUG", "1")
                    .WithEnvironment("PERSISTENCE", "0")
                    .WithPortBinding(4566, true)
                    .WithCleanUp(true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(4566))
                    .Build();

                await s_postgresContainer.StartAsync();
                await s_localStackContainer.StartAsync();
                s_containersStarted = true;
            }

            // Create an isolated database for this test factory
            var serverConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer!.GetConnectionString())
            {
                Database = "postgres"
            };
            await using (var conn = new NpgsqlConnection(serverConnBuilder.ConnectionString))
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Build connection string to the isolated database
            var dbConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer.GetConnectionString())
            {
                Database = _databaseName
            };
            _databaseConnectionString = dbConnBuilder.ConnectionString;
        }
        finally
        {
            s_containerStartLock.Release();
        }
    }

    public new async Task DisposeAsync()
    {
        // Drop the per-class isolated database to keep the shared server clean
        try
        {
            var serverConnBuilder = new NpgsqlConnectionStringBuilder(s_postgresContainer!.GetConnectionString())
            {
                Database = "postgres"
            };
            await using var conn = new NpgsqlConnection(serverConnBuilder.ConnectionString);
            await conn.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", conn);
            await drop.ExecuteNonQueryAsync();
        }
        catch
        {
            // ignore cleanup errors
        }

        // Do NOT dispose shared containers here; they are reused by other parallel classes.
        await base.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string role = "Admin", string email = "test@example.com")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", $"{userId}|{role}|{email}");
        return client;
    }

    public string GetPostgresConnectionString() => _databaseConnectionString!;
    public string GetLocalStackEndpoint() => s_localStackContainer!.GetConnectionString();
}

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions { }

public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorizationHeader = Request.Headers.Authorization.FirstOrDefault();

        if (authorizationHeader == null)
        {
            return Task.FromResult(AuthenticateResult.Fail("No authorization header"));
        }

        var parts = authorizationHeader.Split(' ');
        if (parts.Length != 2 || parts[0] != "Test")
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid authorization header"));
        }

        var userInfo = parts[1].Split('|');
        if (userInfo.Length != 3)
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid user info"));
        }

        var userId = userInfo[0];
        var role = userInfo[1];
        var email = userInfo[2];

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim("sub", userId), // HotChocolate looks for 'sub' claim for currentUserId
            new Claim(ClaimTypes.Name, userId), // This is what the registration endpoint expects
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.Email, email)
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public sealed class TestGraphQLInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        // If user is authenticated, extract user ID (sub or NameIdentifier)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Set into global request state
                requestBuilder.SetGlobalState("currentUserId", userId);
            }
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}










