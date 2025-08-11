using LiveEventService.Core.Registrations.EventRegistration;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Notifications;

public class EventRegistrationCancelledDomainEventHandler : INotificationHandler<EventRegistrationCancelledDomainEvent>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationCancelledDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationCancelledDomainEvent notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.Registration, "cancelled", cancellationToken);
    }
} 