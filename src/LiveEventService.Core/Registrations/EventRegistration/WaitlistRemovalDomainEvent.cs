using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class WaitlistRemovalDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }
    public string? Reason { get; }
    
    public WaitlistRemovalDomainEvent(EventRegistration registration, string? reason = null)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        Reason = reason;
    }
}
