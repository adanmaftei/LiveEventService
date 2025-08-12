using FluentValidation;
using LiveEventService.Application.Common.Behaviors;
using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Features.Events.DomainEventHandlers;
using LiveEventService.Core.Common;
using LiveEventService.Application.Common;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LiveEventService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR from Application assembly
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });
        
        services.AddAutoMapper(cfg => {
            cfg.AddMaps(typeof(DependencyInjection).Assembly);
        });
        
        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        // Register validation behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

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
        
        return services;
    }
} 
