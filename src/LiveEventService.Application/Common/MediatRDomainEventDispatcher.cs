using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Events;
using LiveEventService.Application.Common.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Application.Common;

public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    private readonly IMessageQueue _messageQueue;
    private readonly ILogger<MediatRDomainEventDispatcher> _logger;

    public MediatRDomainEventDispatcher(
        IMediator mediator, 
        IMessageQueue messageQueue,
        ILogger<MediatRDomainEventDispatcher> logger)
    {
        _mediator = mediator;
        _messageQueue = messageQueue;
        _logger = logger;
    }

    public async Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();
            
            foreach (var domainEvent in events)
            {
                await RouteDomainEventAsync(domainEvent, cancellationToken);
            }
        }
    }

    private async Task RouteDomainEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        
        // Determine if this event should be processed synchronously or asynchronously
        if (ShouldProcessSynchronously(domainEvent))
        {
            await ProcessSynchronouslyAsync(domainEvent, cancellationToken);
        }
        else
        {
            await ProcessAsynchronouslyAsync(domainEvent, cancellationToken);
        }
    }

    private bool ShouldProcessSynchronously(DomainEvent domainEvent)
    {
        // Critical events that affect business logic should be processed synchronously
        return domainEvent switch
        {
            EventRegistrationCancelledDomainEvent => true,    // Affects waitlist promotion
            EventCapacityIncreasedDomainEvent => true,        // Affects waitlist promotion
            _ => false                                        // All others can be async
        };
    }

    private async Task ProcessSynchronouslyAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Processing domain event {EventType} synchronously", eventType.Name);

        // Create appropriate notification adapter based on domain event type
        INotification? notification = domainEvent switch
        {
            EventRegistrationCreatedDomainEvent created => new EventRegistrationCreatedNotification(created),
            EventRegistrationPromotedDomainEvent promoted => new EventRegistrationPromotedNotification(promoted),
            EventRegistrationCancelledDomainEvent cancelled => new EventRegistrationCancelledNotification(cancelled),
            WaitlistPositionChangedDomainEvent positionChanged => new WaitlistPositionChangedNotification(positionChanged),
            WaitlistRemovalDomainEvent removal => new WaitlistRemovalNotification(removal),
            RegistrationWaitlistedDomainEvent waitlisted => new RegistrationWaitlistedNotification(waitlisted),
            EventCapacityIncreasedDomainEvent capacityIncreased => new EventCapacityIncreasedNotification(capacityIncreased),
            _ => null
        };

        if (notification != null)
        {
            await _mediator.Publish(notification, cancellationToken);
        }
    }

    private async Task ProcessAsynchronouslyAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        _logger.LogDebug("Queueing domain event {EventType} for asynchronous processing", eventType.Name);

        try
        {
            await _messageQueue.EnqueueAsync(domainEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue domain event {EventType} for async processing", eventType.Name);
            
            // Fallback to synchronous processing if queuing fails
            _logger.LogWarning("Falling back to synchronous processing for domain event {EventType}", eventType.Name);
            await ProcessSynchronouslyAsync(domainEvent, cancellationToken);
        }
    }
} 
