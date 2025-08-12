using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class EventRegistrationCancelledDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public EventRegistrationCancelledDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
} 
