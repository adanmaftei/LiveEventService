using LiveEventService.Application.Common.Notifications;
using LiveEventService.Application.Common;
using LiveEventService.Core.Common;
using MediatR;

namespace LiveEventService.Application.Features.Events.DomainEventHandlers;

[AsyncProcessing(Priority = 1, MaxRetryAttempts = 3, RetryDelaySeconds = 2)]
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
