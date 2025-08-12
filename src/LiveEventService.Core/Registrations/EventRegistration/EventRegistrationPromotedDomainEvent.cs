using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class EventRegistrationPromotedDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationPromotedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}
