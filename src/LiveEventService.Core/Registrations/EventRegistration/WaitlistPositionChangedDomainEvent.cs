using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class WaitlistPositionChangedDomainEvent : DomainEvent
{
    public Guid EventId { get; }
    public Guid RegistrationId { get; }
    public int? OldPosition { get; }
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
