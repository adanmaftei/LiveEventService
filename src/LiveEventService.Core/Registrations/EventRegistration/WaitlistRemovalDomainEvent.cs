using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a registration is removed from the waitlist.
/// </summary>
public class WaitlistRemovalDomainEvent : DomainEvent
{
    /// <summary>Gets the registration removed from the waitlist.</summary>
    public EventRegistration Registration { get; }

    /// <summary>Gets optional reason for removal.</summary>
    public string? Reason { get; }

    public WaitlistRemovalDomainEvent(EventRegistration registration, string? reason = null)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        Reason = reason;
    }
}
