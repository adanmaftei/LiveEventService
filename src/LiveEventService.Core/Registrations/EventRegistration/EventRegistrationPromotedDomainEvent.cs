using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a registration moves from waitlisted/pending to confirmed.
/// </summary>
public class EventRegistrationPromotedDomainEvent : DomainEvent
{
    /// <summary>Gets the promoted registration.</summary>
    public EventRegistration Registration { get; }
    public EventRegistrationPromotedDomainEvent(EventRegistration registration)
    {
        Registration = registration;
    }
}
