namespace LiveEventService.Core.Registrations.EventRegistration;

public enum RegistrationStatus
{
    Pending = 0,    // Initial state
    Confirmed = 1,  // Registration is confirmed
    Waitlisted = 2, // On waitlist
    Cancelled = 3,  // Registration was cancelled
    Attended = 4,   // User attended the event
    NoShow = 5      // User didn't show up
}
