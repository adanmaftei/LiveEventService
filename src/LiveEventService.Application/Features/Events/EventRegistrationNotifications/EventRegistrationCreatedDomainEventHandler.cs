using LiveEventService.Core.Registrations.EventRegistration;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Notifications;

public class EventRegistrationCreatedDomainEventHandler : INotificationHandler<EventRegistrationCreatedDomainEvent>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationCreatedDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.Registration, "created", cancellationToken);
    }
} 