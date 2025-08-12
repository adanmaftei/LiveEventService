using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class EventRegistrationCreatedDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationCreatedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}
