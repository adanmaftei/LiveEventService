using LiveEventService.API.Events;
using LiveEventService.API.Users;
using LiveEventService.API.Middleware;
using LiveEventService.Application;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure;
using LiveEventService.Infrastructure.Data;
using Serilog;
using LiveEventService.Infrastructure.Events.EventRegistrationNotifications;
using LiveEventService.API.GraphQL.Types;

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
builder.Services.AddScoped<IEventRegistrationNotifier, LiveEventService.API.Events.EventRegistrationNotifier>();

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
    .AddType<EventType>()
    .AddType<UserType>()
    .AddAuthorization()
    .AddInMemorySubscriptions()
    .AddFiltering()
    .AddSorting()
    .AddProjections();

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
