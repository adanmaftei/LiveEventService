using FluentValidation;
using LiveEventService.Application.Common.Behaviors;
using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Features.Events.DomainEventHandlers;
using LiveEventService.Core.Common;
using LiveEventService.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using LiveEventService.Application.Configuration;

namespace LiveEventService.Application;

public static class DependencyInjection
{
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

        // Register background service (can be disabled via config when using external queue workers)
        var rootOptions = configuration.GetSection("Performance:BackgroundProcessing").Get<BackgroundProcessingRootOptions>() ?? new BackgroundProcessingRootOptions();
        var useInProcess = rootOptions.UseInProcess;
        // If SQS is configured for domain events, prefer external worker and disable in-process background processing
        var useSqsForDomainEvents = configuration.GetSection("AWS:SQS").GetValue<bool>("UseSqsForDomainEvents");
        if (useSqsForDomainEvents)
        {
            useInProcess = false;
        }
        if (useInProcess)
        {
            services.AddHostedService<DomainEventBackgroundService>();
        }

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
        services.AddSingleton<LiveEventService.Core.Common.IMetricRecorder, LiveEventService.Core.Common.NoOpMetricRecorder>();

        return services;
    }
}
