using MediatR;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Application.Common.Notifications;

// Adapter classes to make domain events implement INotification
// This keeps the Core layer pure while enabling MediatR integration

public class EventRegistrationCreatedNotification : INotification
{
    public EventRegistrationCreatedDomainEvent DomainEvent { get; }

    public EventRegistrationCreatedNotification(EventRegistrationCreatedDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}

public class EventRegistrationPromotedNotification : INotification
{
    public EventRegistrationPromotedDomainEvent DomainEvent { get; }

    public EventRegistrationPromotedNotification(EventRegistrationPromotedDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}

public class EventRegistrationCancelledNotification : INotification
{
    public EventRegistrationCancelledDomainEvent DomainEvent { get; }

    public EventRegistrationCancelledNotification(EventRegistrationCancelledDomainEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
