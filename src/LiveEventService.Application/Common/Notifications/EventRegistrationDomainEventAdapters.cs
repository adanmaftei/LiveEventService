using MediatR;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Application.Common.Notifications;

/// <summary>
/// Adapter notifications that wrap domain events so they can be published via MediatR
/// without coupling the Core layer to MediatR abstractions.
/// </summary>
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
