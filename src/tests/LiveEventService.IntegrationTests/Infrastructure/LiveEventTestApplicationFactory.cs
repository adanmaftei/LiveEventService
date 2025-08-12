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
using LiveEventService.Infrastructure.Data;
using LiveEventService.Core.Common;
using HotChocolate.AspNetCore;
using HotChocolate.Execution.Options;
using Microsoft.AspNetCore.Http;
using HotChocolate.Execution;

namespace LiveEventService.IntegrationTests.Infrastructure;

public class LiveEventTestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly LocalStackContainer _localStackContainer;

    public LiveEventTestApplicationFactory()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:14")
            .WithDatabase("LiveEventTestDB")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();

        _localStackContainer = new LocalStackBuilder()
            .WithImage("localstack/localstack:3.0")
            .WithEnvironment("SERVICES", "cognito-idp,s3,xray,cloudwatch,logs")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("PERSISTENCE", "0")
            .WithPortBinding(4566, true)
            .WithCleanUp(true)
            .Build();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<LiveEventDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add test database context
            services.AddDbContext<LiveEventDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
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
            .ModifyRequestOptions(opt => {
                opt.IncludeExceptionDetails = true;
            })
            .ModifyOptions(opt => {
                opt.StrictValidation = false; // Allow introspection queries
            });

            // Override AWS configuration for LocalStack
            services.Configure<Dictionary<string, string>>(config =>
            {
                config["AWS:ServiceURL"] = _localStackContainer.GetConnectionString();
                config["AWS:Region"] = "us-east-1";
                config["AWS:UserPoolId"] = "us-east-1_000000001";
                config["AWS:S3BucketName"] = "test-bucket";
            });

            // Ensure the database is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LiveEventDbContext>();
            dbContext.Database.EnsureCreated();
        });

        // Set test environment
        builder.UseEnvironment("Testing");

        // Configure connection string for test
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgresContainer.GetConnectionString(),
                ["AWS:ServiceURL"] = _localStackContainer.GetConnectionString(),
                ["AWS:Region"] = "us-east-1",
                ["AWS:UserPoolId"] = "us-east-1_000000001",
                ["AWS:S3BucketName"] = "test-bucket"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _localStackContainer.StartAsync();

        // Wait a bit for LocalStack to be ready
        await Task.Delay(5000);
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _localStackContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string role = "Admin", string email = "test@example.com")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Test", $"{userId}|{role}|{email}");
        return client;
    }

    public string GetPostgresConnectionString() => _postgresContainer.GetConnectionString();
    public string GetLocalStackEndpoint() => _localStackContainer.GetConnectionString();
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










