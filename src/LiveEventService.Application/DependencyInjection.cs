using FluentValidation;
using LiveEventService.Application.Common.Behaviors;
using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Features.Events.DomainEventHandlers;
using LiveEventService.Core.Common;
using LiveEventService.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using LiveEventService.Application.Configuration;

namespace LiveEventService.Application;

/// <summary>
/// Provides service registration helpers for the Application layer.
/// Configures MediatR, AutoMapper, validation, background processing, and default metrics.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers Application layer services into the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration used for binding options.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register MediatR from Application assembly
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(DependencyInjection).Assembly);
        });

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register background processing services
        services.AddSingleton<IMessageQueue, InMemoryMessageQueue>();
        services.AddScoped<IDomainEventProcessor, MediatRDomainEventProcessor>();

        // Configure background processing options
        services.Configure<BackgroundProcessingOptions>(configuration.GetSection("Performance:BackgroundProcessing"));
        services.Configure<BackgroundProcessingRootOptions>(configuration.GetSection("Performance:BackgroundProcessing"));

        // Hosting/background worker registration is decided in the composition root
        // to keep the Application layer independent of hosting and vendor specifics.

        // Register domain event dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Register domain event handlers
        services.AddScoped<INotificationHandler<EventRegistrationCreatedNotification>, EventRegistrationCreatedDomainEventHandler>();
        services.AddScoped<INotificationHandler<EventRegistrationPromotedNotification>, EventRegistrationPromotedDomainEventHandler>();
        services.AddScoped<INotificationHandler<EventRegistrationCancelledNotification>, EventRegistrationCancelledDomainEventHandler>();
        services.AddScoped<INotificationHandler<WaitlistPositionChangedNotification>, WaitlistPositionChangedDomainEventHandler>();
        services.AddScoped<INotificationHandler<WaitlistRemovalNotification>, WaitlistRemovalDomainEventHandler>();
        services.AddScoped<INotificationHandler<RegistrationWaitlistedNotification>, RegistrationWaitlistedDomainEventHandler>();
        services.AddScoped<INotificationHandler<EventCapacityIncreasedNotification>, EventCapacityIncreasedDomainEventHandler>();

        // Default metrics recorder (overridden in Infrastructure)
        services.AddSingleton<IMetricRecorder, NoOpMetricRecorder>();

        return services;
    }
}
