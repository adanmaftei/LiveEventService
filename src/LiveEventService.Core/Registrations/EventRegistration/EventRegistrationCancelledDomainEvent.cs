using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a registration is cancelled.
/// </summary>
public class EventRegistrationCancelledDomainEvent : DomainEvent
{
    /// <summary>Gets the cancelled registration.</summary>
    public EventRegistration Registration { get; }
    public EventRegistrationCancelledDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}
