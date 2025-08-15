using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a registration is created.
/// </summary>
public class EventRegistrationCreatedDomainEvent : DomainEvent
{
    /// <summary>Gets the newly created registration.</summary>
    public EventRegistration Registration { get; }
    public EventRegistrationCreatedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}
