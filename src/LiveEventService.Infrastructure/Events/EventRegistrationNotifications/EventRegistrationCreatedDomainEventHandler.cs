using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.Infrastructure.Events.EventRegistrationNotifications;

public class EventRegistrationCreatedDomainEventHandler : INotificationHandler<EventRegistrationCreatedNotification>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationCreatedDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationCreatedNotification notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.DomainEvent.Registration, "created", cancellationToken);
    }
}