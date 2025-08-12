using LiveEventService.API.Events;
using LiveEventService.API.Users;
using LiveEventService.API.Middleware;
using LiveEventService.Application;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure;
using LiveEventService.Infrastructure.Data;
using Serilog;
using LiveEventService.API.GraphQL.Types;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Serilog.Context;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

// Harden Kestrel: remove Server header and cap request body size
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// Configure AWS X-Ray
AWSXRayRecorder.InitializeInstance(configuration: builder.Configuration);
AWSSDKHandler.RegisterXRayForAllServices();

// Configure Serilog with CloudWatch
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

Log.Information("Starting web application");

// Add services to the container.
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration, isTesting);

// Add API-specific services
builder.Services.AddScoped<IEventRegistrationNotifier, EventRegistrationNotifier>();

// Add Swagger services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add GraphQL services
builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddTypeExtension<EventQueries>()
        .AddTypeExtension<UserQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddTypeExtension<EventMutations>()
        .AddTypeExtension<UserMutations>()
    .AddSubscriptionType(d => d.Name("Subscription"))
        .AddTypeExtension<EventSubscriptions>()
    .AddType<EventType>()
    .AddType<UserType>()
    .AddAuthorization()
    .AddInMemorySubscriptions()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

// Add rate limiting (disabled in Testing environment)
if (!isTesting)
{
    builder.Services.AddRateLimiter(options =>
    {
        // General API limiter: token bucket per IP
        options.AddPolicy("general", httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
            var key = $"general:{ip}";
            return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                TokensPerPeriod = 100,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                AutoReplenishment = true,
                QueueLimit = 0
            });
        });

        // Stricter limiter for registration endpoints: per user if authenticated, else per IP
        options.AddPolicy("registration", httpContext =>
        {
            var partitionKey = httpContext.User?.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity!.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon"
                : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";

            return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
        });
    });
}

var app = builder.Build();

// Initialize database
try
{
    if (isTesting == false)
    {
        Log.Information("Starting database initialization...");
        await DatabaseInitializer.InitializeDatabaseAsync(app.Services);
        Log.Information("Database initialization completed successfully.");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred during database initialization");
    throw;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Live Event Service API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}
else
{
    // Production hardening
    app.UseHsts();
}

// Redirect HTTP to HTTPS only outside dev/testing
if (!app.Environment.IsDevelopment() && !isTesting)
{
    app.UseHttpsRedirection();
}

// Add correlation ID middleware
app.Use(async (context, next) =>
{
    // Add correlation ID to the request if not present
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Items["CorrelationId"] = correlationId;
    
    // Add correlation ID to the response headers
    context.Response.Headers.Append("X-Correlation-ID", correlationId);
    
    // Add correlation ID to all logs in this request
    using (LogContext.PushProperty("CorrelationId", correlationId))
    {
        await next(context);
    }
});

// Add enhanced request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
        
        // Add correlation ID if available
        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }
    };
});

// Add global exception handling middleware (excluding GraphQL)
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/graphql"), 
    appBuilder => appBuilder.UseMiddleware<GlobalExceptionMiddleware>());

// Apply security headers
app.UseMiddleware<LiveEventService.API.Middleware.SecurityHeadersMiddleware>();

app.UseCors();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Apply rate limiting (if enabled)
if (!isTesting)
{
    app.UseRateLimiter();
}

// Configure X-Ray tracing middleware
app.UseXRay("LiveEventService");

// Configure health check endpoints
app.MapHealthChecks("/health", new()
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            entries = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration,
                    data = entry.Value.Data,
                    description = entry.Value.Description,
                    exception = entry.Value.Exception?.Message,
                    tags = entry.Value.Tags
                })
        });
        await context.Response.WriteAsync(result);
    }
})
    .RequireRateLimiting("general");

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
})
    .RequireRateLimiting("general");

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
})
    .RequireRateLimiting("general");

// Configure GraphQL endpoint
var graphQLEndpoint = app.MapGraphQL("/graphql");
if (!isTesting)
{
    graphQLEndpoint.RequireRateLimiting("general");
}

// Configure Nitro (GraphQL Playground) in development only
if (app.Environment.IsDevelopment())
{
    app.MapNitroApp("/graphql/playground");
}

// Configure minimal API endpoints
app.MapEventEndpoints();
app.MapUserEndpoints();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration testing
public partial class Program { }
