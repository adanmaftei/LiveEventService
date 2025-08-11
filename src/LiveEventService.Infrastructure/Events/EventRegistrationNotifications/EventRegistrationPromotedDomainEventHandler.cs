using LiveEventService.Core.Registrations.EventRegistration;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.Infrastructure.Events.EventRegistrationNotifications;

public class EventRegistrationPromotedDomainEventHandler : INotificationHandler<EventRegistrationPromotedNotification>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationPromotedDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationPromotedNotification notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.DomainEvent.Registration, "promoted", cancellationToken);
    }
}