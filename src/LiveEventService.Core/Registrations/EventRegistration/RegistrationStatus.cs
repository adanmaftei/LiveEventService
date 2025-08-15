namespace LiveEventService.Core.Registrations.EventRegistration;

/// <summary>
/// Represents the lifecycle status of an event registration.
/// </summary>
public enum RegistrationStatus
{
    /// <summary>
    /// Initial state when registration is created but not yet processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Registration is confirmed and user has a spot in the event.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// User is on the waitlist due to event capacity being full.
    /// </summary>
    Waitlisted = 2,

    /// <summary>
    /// Registration was cancelled by the user or administrator.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// User attended the event.
    /// </summary>
    Attended = 4,

    /// <summary>
    /// User didn't show up for the event.
    /// </summary>
    NoShow = 5
}
