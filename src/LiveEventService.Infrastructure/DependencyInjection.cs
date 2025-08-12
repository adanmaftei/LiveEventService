using Microsoft.Extensions.DependencyInjection;
using LiveEventService.Core.Events;
using LiveEventService.Infrastructure.Data;
using LiveEventService.Infrastructure.Users;
using LiveEventService.Infrastructure.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure.Events.EventRegistrationNotifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MediatR;
using LiveEventService.Infrastructure.Events.WaitlistNotifications;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Users.User;

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
                        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            }
        }

        // Register repositories
        services.AddScoped(typeof(IRepository<>), typeof(RepositoryBase<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRepository, EventRepository>();

        // Register generic repository for EventRegistration
        services.AddScoped<IRepository<EventRegistration>, RepositoryBase<EventRegistration>>();
        
        // Register domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Register domain event handlers (only register once)
        services.AddScoped<INotificationHandler<EventRegistrationCreatedNotification>, EventRegistrationCreatedDomainEventHandler>();
        services.AddScoped<INotificationHandler<EventRegistrationPromotedNotification>, EventRegistrationPromotedDomainEventHandler>();
        services.AddScoped<INotificationHandler<EventRegistrationCancelledNotification>, EventRegistrationCancelledDomainEventHandler>();
        
        // Register waitlist domain event handlers
        services.AddScoped<INotificationHandler<EventCapacityIncreasedNotification>, EventCapacityIncreasedDomainEventHandler>();
        services.AddScoped<INotificationHandler<RegistrationWaitlistedNotification>, RegistrationWaitlistedDomainEventHandler>();
        services.AddScoped<INotificationHandler<WaitlistPositionChangedNotification>, WaitlistPositionChangedDomainEventHandler>();
        services.AddScoped<INotificationHandler<WaitlistRemovalNotification>, WaitlistRemovalDomainEventHandler>();

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

        // Note: S3 health check is not implemented due to missing package
        // For production, consider adding AspNetCore.HealthChecks.Aws.S3 package

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

        return services;
    }
}
