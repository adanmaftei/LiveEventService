using Amazon.Extensions.NETCore.Setup;
using Amazon.XRay;
using Amazon.XRay.Recorder.Core;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using LiveEventService.API.Events;
using LiveEventService.API.Users;
using LiveEventService.API.Middleware;
using LiveEventService.Application;
using LiveEventService.Application.Features.Events.EventRegistration.Notifications;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure;
using LiveEventService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var isTesting = builder.Environment.IsEnvironment("Testing");

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
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, isTesting);
builder.Services.AddScoped<IEventRegistrationNotifier, EventRegistrationNotifier>();

// Configure AWS X-Ray
AWSXRayRecorder.InitializeInstance(configuration: builder.Configuration);
AWSSDKHandler.RegisterXRayForAllServices();

// Add X-Ray middleware
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());

// Configure AWS services to use LocalStack in development
if (builder.Environment.IsDevelopment())
{
    var serviceUrl = builder.Configuration["AWS:ServiceURL"];
    if (!string.IsNullOrEmpty(serviceUrl))
    {
        builder.Services.Configure<AWSOptions>(options =>
        {
            options.DefaultClientConfig.ServiceURL = serviceUrl;
            options.DefaultClientConfig.UseHttp = true;
            options.DefaultClientConfig.AuthenticationRegion = builder.Configuration["AWS:Region"];
        });
    }
}

builder.Services.AddAWSService<IAmazonXRay>();

// Add AWS Cognito authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = $"https://cognito-idp.{builder.Configuration["AWS:Region"]}.amazonaws.com/{builder.Configuration["AWS:UserPoolId"]}";
    options.TokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://cognito-idp.{builder.Configuration["AWS:Region"]}.amazonaws.com/{builder.Configuration["AWS:UserPoolId"]}",
        ValidateLifetime = true,
        LifetimeValidator = (_, expires, _, _) => expires > DateTime.UtcNow,
        ValidateAudience = false,
        RoleClaimType = "cognito:groups"
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleNames.Admin, policy => 
        policy.RequireRole(RoleNames.Admin));
    options.AddPolicy(RoleNames.Participant, policy => 
        policy.RequireRole(RoleNames.Participant));
});

// Add health checks for PostgreSQL (RDS) and AWS services
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

var healthChecksBuilder = builder.Services.AddHealthChecks();

// Only add PostgreSQL health check if connection string is available
if (!string.IsNullOrEmpty(defaultConnection))
{
    healthChecksBuilder.AddNpgSql(
        defaultConnection,
        name: "PostgreSQL (RDS)",
        tags: new[] { "db", "rds", "postgresql" });
}
else if (!builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException("DefaultConnection string is missing in configuration.");
}

// Only add S3 health check in production or if properly configured
if (!builder.Environment.IsDevelopment())
{
    healthChecksBuilder.AddS3(options =>
    {
        options.BucketName = builder.Configuration["AWS:S3BucketName"] ?? string.Empty;
    },
    name: "AWS S3",
    tags: new[] { "aws", "s3" });
}

healthChecksBuilder.AddCheck("AWS Cognito", () =>
    {
        // Simple check for Cognito config presence
        var region = builder.Configuration["AWS:Region"]!;
        var userPoolId = builder.Configuration["AWS:UserPoolId"]!;
        return !string.IsNullOrEmpty(region) && !string.IsNullOrEmpty(userPoolId)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cognito config missing");
    }, tags: new[] { "aws", "cognito" });

// Add GraphQL
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
    .AddType<LiveEventService.API.Events.EventType>()
    .AddType<UserType>()
    .AddAuthorization()
    .AddInMemorySubscriptions()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Live Event Service API", Version = "v1" });
    // Add JWT Bearer Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
    // Enable XML comments for Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath!);
    }
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        string[] allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

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

// Add global exception handling middleware (excluding GraphQL)
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/graphql"), 
    appBuilder => appBuilder.UseMiddleware<GlobalExceptionMiddleware>());

app.UseCors();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

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
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new()
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new()
{
    Predicate = _ => false
});

// Configure GraphQL endpoint
app.MapGraphQL("/graphql");

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
