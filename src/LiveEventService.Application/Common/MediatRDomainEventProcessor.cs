using LiveEventService.Core.Common;
using LiveEventService.Application.Common.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Application.Common;

/// <summary>
/// MediatR-based domain event processor for background processing
/// </summary>
public class MediatRDomainEventProcessor : IDomainEventProcessor
{
    private readonly IMediator _mediator;
    private readonly ILogger<MediatRDomainEventProcessor> _logger;
    private readonly Dictionary<Type, Type> _eventToNotificationMap;

    public MediatRDomainEventProcessor(IMediator mediator, ILogger<MediatRDomainEventProcessor> logger)
    {
        _mediator = mediator;
        _logger = logger;
        _eventToNotificationMap = CreateEventToNotificationMap();
    }

    public async Task ProcessAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventType = domainEvent.GetType();
        
        if (!_eventToNotificationMap.TryGetValue(eventType, out var notificationType))
        {
            _logger.LogWarning("No notification mapping found for domain event type {EventType}", eventType.Name);
            return;
        }

        try
        {
            // Convert domain event to MediatR notification
            var notification = ConvertToNotification(domainEvent, notificationType);
            if (notification == null)
            {
                _logger.LogWarning("Failed to convert domain event {EventType} to notification", eventType.Name);
                return;
            }

            // Publish notification through MediatR
            await _mediator.Publish(notification, cancellationToken);
            
            _logger.LogDebug("Successfully processed domain event {EventType} through MediatR", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain event {EventType} through MediatR", eventType.Name);
            throw;
        }
    }

    public bool CanProcess(Type domainEventType)
    {
        return _eventToNotificationMap.ContainsKey(domainEventType);
    }

    private Dictionary<Type, Type> CreateEventToNotificationMap()
    {
        return new Dictionary<Type, Type>
        {
            // Map domain events to their corresponding MediatR notifications
            // These will be processed by the existing domain event handlers
            { typeof(LiveEventService.Core.Registrations.EventRegistration.EventRegistrationCreatedDomainEvent), typeof(EventRegistrationCreatedNotification) },
            { typeof(LiveEventService.Core.Registrations.EventRegistration.EventRegistrationPromotedDomainEvent), typeof(EventRegistrationPromotedNotification) },
            { typeof(LiveEventService.Core.Registrations.EventRegistration.EventRegistrationCancelledDomainEvent), typeof(EventRegistrationCancelledNotification) },
            { typeof(LiveEventService.Core.Registrations.EventRegistration.WaitlistPositionChangedDomainEvent), typeof(WaitlistPositionChangedNotification) },
            { typeof(LiveEventService.Core.Registrations.EventRegistration.WaitlistRemovalDomainEvent), typeof(WaitlistRemovalNotification) },
            { typeof(LiveEventService.Core.Registrations.EventRegistration.RegistrationWaitlistedDomainEvent), typeof(RegistrationWaitlistedNotification) },
            { typeof(LiveEventService.Core.Events.EventCapacityIncreasedDomainEvent), typeof(EventCapacityIncreasedNotification) }
        };
    }

    private object? ConvertToNotification(DomainEvent domainEvent, Type notificationType)
    {
        try
        {
            // Use reflection to create notification from domain event
            // This is a simplified approach - in a real implementation, you might want
            // to use AutoMapper or similar for more complex mappings
            
            var constructor = notificationType.GetConstructor(new[] { domainEvent.GetType() });
            if (constructor != null)
            {
                return constructor.Invoke(new object[] { domainEvent });
            }

            // Fallback: try to create notification with default constructor and set properties
            var notification = Activator.CreateInstance(notificationType);
            if (notification != null)
            {
                // Copy properties from domain event to notification
                CopyProperties(domainEvent, notification);
                return notification;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting domain event {EventType} to notification {NotificationType}", 
                domainEvent.GetType().Name, notificationType.Name);
        }

        return null;
    }

    private void CopyProperties(object source, object target)
    {
        var sourceType = source.GetType();
        var targetType = target.GetType();

        foreach (var sourceProperty in sourceType.GetProperties())
        {
            var targetProperty = targetType.GetProperty(sourceProperty.Name);
            if (targetProperty?.CanWrite == true && sourceProperty.CanRead)
            {
                try
                {
                    var value = sourceProperty.GetValue(source);
                    targetProperty.SetValue(target, value);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error copying property {PropertyName} from {SourceType} to {TargetType}", 
                        sourceProperty.Name, sourceType.Name, targetType.Name);
                }
            }
        }
    }
} 