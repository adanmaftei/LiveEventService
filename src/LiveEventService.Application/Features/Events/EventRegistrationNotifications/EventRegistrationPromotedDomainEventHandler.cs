using LiveEventService.Core.Registrations.EventRegistration;
using MediatR;

namespace LiveEventService.Application.Features.Events.EventRegistration.Notifications;

public class EventRegistrationPromotedDomainEventHandler : INotificationHandler<EventRegistrationPromotedDomainEvent>
{
    private readonly IEventRegistrationNotifier _notifier;
    public EventRegistrationPromotedDomainEventHandler(IEventRegistrationNotifier notifier)
    {
        _notifier = notifier;
    }
    public async Task Handle(EventRegistrationPromotedDomainEvent notification, CancellationToken cancellationToken)
    {
        await _notifier.NotifyAsync(notification.Registration, "promoted", cancellationToken);
    }
} 