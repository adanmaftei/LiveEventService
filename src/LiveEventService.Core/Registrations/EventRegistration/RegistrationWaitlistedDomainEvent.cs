using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a registration is placed on the waitlist.
/// </summary>
public class RegistrationWaitlistedDomainEvent : DomainEvent
{
    /// <summary>Gets the waitlisted registration.</summary>
    public EventRegistration Registration { get; }

    public RegistrationWaitlistedDomainEvent(EventRegistration registration)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }
}
