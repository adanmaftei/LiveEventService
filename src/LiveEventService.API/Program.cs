using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Amazon;
using Amazon.CloudWatchLogs;
using HotChocolate.AspNetCore;
using LiveEventService.API.Constants;
using LiveEventService.API.Configuration;
using LiveEventService.API.Events;
using LiveEventService.API.GraphQL.DataLoaders;
using LiveEventService.API.GraphQL.Types;
using LiveEventService.API.Logging;
using LiveEventService.API.Middleware;
using LiveEventService.API.Users;
using LiveEventService.Application;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Infrastructure.Telemetry;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch;
using StackExchange.Redis;
using Microsoft.AspNetCore.Authentication.JwtBearer;

// Entry point and composition root for the Live Event Service API.
// Configures hosting, observability, authentication/authorization,
// output caching, rate limiting, GraphQL, and minimal API endpoints.
var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

// Harden Kestrel: remove Server header and cap request body size
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

// Configure Serilog (Console always; CloudWatch in Production)
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();

    var isProd = context.HostingEnvironment.IsProduction();
    if (isProd && !isTesting)
    {
        configuration.WriteToCloudWatch(context.Configuration);
    }
});

Log.Information("Starting web application");

// Trust proxy headers when running behind ALB/API Gateway
var knownProxies = builder.Configuration.GetSection("Networking:KnownProxies").Get<string[]>() ?? Array.Empty<string>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    foreach (var ip in knownProxies)
    {
        if (IPAddress.TryParse(ip, out var parsed))
        {
            options.KnownProxies.Add(parsed);
        }
    }
});

// Bind options
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Security:Cors"));
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<LiveEventService.Infrastructure.Configuration.AwsOptions>(builder.Configuration.GetSection("AWS"));
builder.Services.Configure<RedisOptions>(opt =>
{
    opt.ConnectionString = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["ConnectionStrings:Redis"];
});
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));

// Add services to the container.
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration, isTesting);

// Decide hosting-time domain event processing here (composition root)
var bgRootOptions = builder.Configuration.GetSection("Performance:BackgroundProcessing").Get<LiveEventService.Application.Configuration.BackgroundProcessingRootOptions>() ?? new LiveEventService.Application.Configuration.BackgroundProcessingRootOptions();
var useInProcess = bgRootOptions.UseInProcess && !isTesting;
var useSqsForDomainEvents = builder.Configuration.GetSection("AWS:SQS").GetValue<bool>("UseSqsForDomainEvents");
if (useSqsForDomainEvents)
{
    useInProcess = false;
}
if (useInProcess)
{
    builder.Services.AddHostedService<LiveEventService.Application.Common.DomainEventBackgroundService>();
}

// Output caching (public GETs)
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy(OutputCachePolicies.EventListPublic, b => b
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByQuery("pageNumber", "pageSize", "isPublished", "isUpcoming")
        .SetVaryByHeader("Authorization")
        .SetVaryByHeader("Cookie")
        .Tag(OutputCacheTags.Events));

    options.AddPolicy(OutputCachePolicies.EventDetailPublic, b => b
        .Expire(TimeSpan.FromSeconds(120))
        .SetVaryByRouteValue("id")
        .SetVaryByHeader("Authorization")
        .SetVaryByHeader("Cookie")
        .Tag(OutputCacheTags.EventDetail));
});

// CORS configuration (env-driven)
var corsOptions = builder.Configuration.GetSection("Security:Cors").Get<CorsOptions>() ?? new CorsOptions();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment() || isTesting || corsOptions.AllowedOrigins.Length == 0)
        {
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsOptions.AllowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

// Add API-specific services
builder.Services.AddScoped<IEventRegistrationNotifier, EventRegistrationNotifier>();

// Authentication & Authorization (moved from Infrastructure)
var awsAuth = builder.Configuration.GetSection("AWS").Get<LiveEventService.Infrastructure.Configuration.AwsOptions>() ?? new LiveEventService.Infrastructure.Configuration.AwsOptions();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = $"https://cognito-idp.{awsAuth.Region}.amazonaws.com/{awsAuth.UserPoolId}";
    var configuredAudiences = awsAuth.Jwt.Audiences ?? Array.Empty<string>();
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://cognito-idp.{awsAuth.Region}.amazonaws.com/{awsAuth.UserPoolId}",
        ValidateLifetime = true,
        LifetimeValidator = (_, expires, _, _) => expires > DateTime.UtcNow,
        ValidateAudience = configuredAudiences.Length > 0,
        ValidAudiences = configuredAudiences,
        RoleClaimType = "cognito:groups"
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleNames.Admin, policy => policy.RequireRole(RoleNames.Admin));
    options.AddPolicy(RoleNames.Participant, policy => policy.RequireRole(RoleNames.Participant));
});

// Dedicated audit logger sink (e.g., CloudWatch Logs) separate from application logs
if (builder.Environment.IsProduction() && !isTesting)
{
    var aws = builder.Configuration.GetSection("AWS").Get<LiveEventService.Infrastructure.Configuration.AwsOptions>() ?? new LiveEventService.Infrastructure.Configuration.AwsOptions();
    var region = aws.CloudWatch.Region ?? aws.Region ?? "us-east-1";
    var auditLogGroup = aws.CloudWatch.AuditLogGroup ?? "/live-event-service/audit";

    var cwClient = new AmazonCloudWatchLogsClient(RegionEndpoint.GetBySystemName(region));
    var auditSerilog = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.AmazonCloudWatch(
            logGroup: auditLogGroup,
            logStreamPrefix: "audit-",
            cloudWatchClient: cwClient,
            textFormatter: new JsonFormatter(),
            createLogGroup: true)
        .CreateLogger();

    builder.Services.AddSingleton<IAuditLogger>(sp => new SerilogAuditLogger(auditSerilog));
}
else
{
    builder.Services.AddSingleton<IAuditLogger>(sp => new SerilogAuditLogger(Log.Logger));
}
builder.Services.AddSingleton<LiveEventService.API.Utilities.IIdempotencyStore, LiveEventService.API.Utilities.IdempotencyStore>();

// Centralized HTTP resilience policy and named clients
builder.Services.AddHttpClient("resilient-default").AddDefaultResilience();
builder.Services.AddHttpClient("resilient-short")
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(5))
    .AddDefaultResilience();

// OpenTelemetry metrics & tracing (Prometheus + OTLP)
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddMeter(AppMetrics.MeterName)
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("LiveEventService"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter();
    });

// Distributed cache (Redis in non-testing; no-op in Testing)
if (!isTesting)
{
    var redisOptions = builder.Configuration.GetSection("ConnectionStrings").Exists()
        ? new RedisOptions { ConnectionString = builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["ConnectionStrings:Redis"] }
        : builder.Configuration.GetSection("Redis").Get<RedisOptions>() ?? new RedisOptions();
    if (!string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisOptions.ConnectionString;
            options.InstanceName = "liveevent:";
        });

        // Wire Redis connectivity gauge
        try
        {
            var existingMuxReg = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer));
            if (existingMuxReg == null)
            {
                var mux = ConnectionMultiplexer.Connect(redisOptions.ConnectionString);
                AppMetrics.SetRedisConnectivityProvider(() => mux.IsConnected ? 1 : 0);
                builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
            }
            else
            {
                var mux = existingMuxReg.ImplementationInstance as IConnectionMultiplexer;
                AppMetrics.SetRedisConnectivityProvider(() => mux != null && mux.IsConnected ? 1 : 0);
            }
        }
        catch
        {
            AppMetrics.SetRedisConnectivityProvider(() => 0);
        }
    }
}
else
{
    builder.Services.AddSingleton<IDistributedCache, DisabledDistributedCache>();
}

// Add Swagger services with JWT Bearer support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Live Event Service API", Version = "v1" });
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter 'Bearer {token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });

    // Include XML comments for better schema and endpoint documentation
    var apiXml = System.IO.Path.Combine(AppContext.BaseDirectory, "LiveEventService.API.xml");
    if (System.IO.File.Exists(apiXml))
    {
        c.IncludeXmlComments(apiXml, includeControllerXmlComments: true);
    }
    var applicationXml = System.IO.Path.Combine(AppContext.BaseDirectory, "LiveEventService.Application.xml");
    if (System.IO.File.Exists(applicationXml))
    {
        c.IncludeXmlComments(applicationXml, includeControllerXmlComments: true);
    }
});

// ProblemDetails for standardized error responses
builder.Services.AddProblemDetails();

// Response compression for text-based responses
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Add GraphQL services
var graphQlBuilder = builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
        .AddTypeExtension<EventQueries>()
        .AddTypeExtension<UserQueries>()
        .AddTypeExtension<EventAdminQueries>()
    .AddMutationType(d => d.Name("Mutation"))
        .AddTypeExtension<EventMutations>()
        .AddTypeExtension<UserMutations>()
    .AddSubscriptionType(d => d.Name("Subscription"))
        .AddTypeExtension<EventSubscriptions>()
    .AddType<EventType>()
    .AddType<UserType>()
    .AddAuthorization();

// Subscriptions backplane: prefer Redis when configured and not in Testing; otherwise fall back to in-memory
var redisCfg = builder.Configuration.GetSection("ConnectionStrings").Exists()
    ? builder.Configuration.GetConnectionString("Redis") ?? builder.Configuration["ConnectionStrings:Redis"]
    : builder.Configuration.GetSection("Redis").Get<RedisOptions>()?.ConnectionString ?? string.Empty;
if (!isTesting && !string.IsNullOrWhiteSpace(redisCfg))
{
    // Reuse a single ConnectionMultiplexer registered in DI if present; otherwise create once and register
    var existingMux = builder.Services.FirstOrDefault(s => s.ServiceType == typeof(IConnectionMultiplexer))?.ImplementationInstance as IConnectionMultiplexer;
    if (existingMux == null)
    {
        var mux = ConnectionMultiplexer.Connect(redisCfg);
        builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
        existingMux = mux;
    }
    graphQlBuilder = graphQlBuilder.AddRedisSubscriptions(_ => existingMux!);
}
else
{
    graphQlBuilder = graphQlBuilder.AddInMemorySubscriptions();
}

// HotChocolate v15: use validation rules to limit depth/complexity if needed
var gqlOptions = builder.Configuration.GetSection("GraphQL").Get<GraphQLOptions>() ?? new GraphQLOptions();
graphQlBuilder
    .AddFiltering()
    .AddSorting()
    .AddProjections()
    .AddMaxExecutionDepthRule(gqlOptions.MaxExecutionDepth)
    .ModifyOptions(opt =>
    {
        // Keep validation strict by default; can be tuned via options
        opt.StrictValidation = gqlOptions.StrictValidation;
    })
    .ModifyRequestOptions(opt =>
    {
        opt.ExecutionTimeout = TimeSpan.FromSeconds(gqlOptions.ExecutionTimeoutSeconds);
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    })
    .AddDataLoader<UserByIdentityIdDataLoader>();

// Add rate limiting (disabled in Testing environment)
if (!isTesting)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.Headers["Retry-After"] = "60";
            var payload = System.Text.Json.JsonSerializer.Serialize(new { error = "rate_limited", message = "Too many requests. Please retry later." });
            await context.HttpContext.Response.WriteAsync(payload, token);
        };

        // General API limiter: token bucket per IP
        options.AddPolicy(PolicyNames.General, httpContext =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
            var key = $"{PolicyNames.General}:{ip}";
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
        options.AddPolicy(PolicyNames.Registration, httpContext =>
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

// Initialize database only in Development (migrations handled by CI/CD)
try
{
    if (!isTesting && builder.Environment.IsDevelopment())
    {
        var dbOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<DatabaseOptions>>().Value;
        var initializeOnStartup = dbOptions.InitializeOnStartup;
        if (initializeOnStartup)
        {
            Log.Information("Starting database initialization (Development only)...");
            await DatabaseInitializer.InitializeDatabaseAsync(app.Services);
            Log.Information("Database initialization completed successfully.");
        }
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
    app.UseExceptionHandler(); // Standardize errors via ProblemDetails
}

// Redirect HTTP to HTTPS only outside dev/testing
if (!app.Environment.IsDevelopment() && !isTesting)
{
    app.UseHttpsRedirection();
}

// Apply forwarded headers early so RemoteIp reflects original client IP behind proxies
app.UseForwardedHeaders();

// Add correlation ID middleware
app.Use(async (context, next) =>
{
    // Add correlation ID to the request if not present
    var correlationId = context.Request.Headers[CustomHeaderNames.CorrelationId].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Items[CustomHeaderNames.CorrelationId] = correlationId;

    // Add correlation ID to the response headers
    context.Response.Headers.Append(CustomHeaderNames.CorrelationId, correlationId);

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
        diagnosticContext.Set("RequestHost", (object)(httpContext.Request.Host.Value ?? string.Empty));
        diagnosticContext.Set("RequestScheme", (object)(httpContext.Request.Scheme ?? string.Empty));
        var ua = httpContext.Request.Headers.UserAgent.ToString();
        diagnosticContext.Set("UserAgent", (object)(ua ?? string.Empty));
        diagnosticContext.Set("RemoteIpAddress", (object)(httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty));

        // Add user identifiers when available
        var user = httpContext.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var nameId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value
                         ?? user.Identity?.Name;
            var userName = user.Identity?.Name
                           ?? user.FindFirst("cognito:username")?.Value
                           ?? user.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(nameId))
            {
                diagnosticContext.Set("UserId", nameId);
            }

            if (!string.IsNullOrEmpty(userName))
            {
                diagnosticContext.Set("UserName", userName);
            }
        }

        // Add correlation ID if available
        if (httpContext.Request.Headers.TryGetValue(CustomHeaderNames.CorrelationId, out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", (object)correlationId.ToString());
        }
    };
});

// Add global exception handling middleware (excluding GraphQL)
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/graphql"),
    appBuilder => appBuilder.UseMiddleware<GlobalExceptionMiddleware>());

// Apply security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// Enable response compression
app.UseResponseCompression();

app.UseCors();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Apply rate limiting (if enabled)
if (!isTesting)
{
    app.UseRateLimiter();
}

// Enable output caching middleware
app.UseOutputCache();

// Traces are emitted via OpenTelemetry OTLP exporter to the ADOT Collector (configured via env)

// Expose Prometheus metrics
app.MapPrometheusScrapingEndpoint("/metrics");

// Configure health check endpoints
app.MapHealthChecks(RoutePaths.Health, new()
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
});

app.MapHealthChecks(RoutePaths.HealthReady, new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks(RoutePaths.HealthLive, new()
{
    Predicate = _ => false
});

// Configure GraphQL endpoint
var graphQLEndpoint = app.MapGraphQL(RoutePaths.GraphQL);
graphQLEndpoint.WithOptions(new GraphQLServerOptions
{
    Tool = { Enable = app.Environment.IsDevelopment() }
});
if (!isTesting)
{
    graphQLEndpoint.RequireRateLimiting(PolicyNames.General);
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

/// <summary>
/// Exposes the entry point type to integration tests.
/// </summary>
public partial class Program { }
