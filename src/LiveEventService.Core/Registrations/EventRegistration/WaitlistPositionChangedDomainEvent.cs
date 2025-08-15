using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Domain event raised when a waitlisted registration's position changes.
/// </summary>
public class WaitlistPositionChangedDomainEvent : DomainEvent
{
    /// <summary>Gets the event identifier.</summary>
    public Guid EventId { get; }

    /// <summary>Gets the registration identifier.</summary>
    public Guid RegistrationId { get; }

    /// <summary>Gets previous waitlist position.</summary>
    public int? OldPosition { get; }

    /// <summary>Gets new waitlist position.</summary>
    public int? NewPosition { get; }

    public WaitlistPositionChangedDomainEvent(
        Guid eventId,
        Guid registrationId,
        int? oldPosition,
        int? newPosition)
    {
        EventId = eventId;
        RegistrationId = registrationId;
        OldPosition = oldPosition;
        NewPosition = newPosition;
    }
}
