using LiveEventService.Core.Common;
using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Infrastructure.Events.EventRegistrationNotifications;
using MediatR;

namespace LiveEventService.Infrastructure.Data;

public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;
    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
            {
                // Create appropriate notification adapter based on domain event type
                INotification? notification = domainEvent switch
                {
                    EventRegistrationCreatedDomainEvent created => new EventRegistrationCreatedNotification(created),
                    EventRegistrationPromotedDomainEvent promoted => new EventRegistrationPromotedNotification(promoted),
                    EventRegistrationCancelledDomainEvent cancelled => new EventRegistrationCancelledNotification(cancelled),
                    _ => null
                };

                if (notification != null)
                {
                    await _mediator.Publish(notification, cancellationToken);
                }
            }
        }
    }
} 