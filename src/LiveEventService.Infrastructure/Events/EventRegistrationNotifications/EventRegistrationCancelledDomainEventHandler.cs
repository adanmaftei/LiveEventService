using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.Infrastructure.Events.EventRegistrationNotifications;

public class EventRegistrationCancelledDomainEventHandler : INotificationHandler<EventRegistrationCancelledNotification>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationCancelledDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationCancelledNotification notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.DomainEvent.Registration, "cancelled", cancellationToken);
    }
}