using MediatR;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Infrastructure.Events.WaitlistNotifications;

// Adapter classes to make waitlist domain events implement INotification
// This follows the same pattern as EventRegistrationDomainEventAdapters

public class EventCapacityIncreasedNotification : INotification
{
    public EventCapacityIncreasedDomainEvent DomainEvent { get; }
    
    public EventCapacityIncreasedNotification(EventCapacityIncreasedDomainEvent domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}

public class RegistrationWaitlistedNotification : INotification
{
    public RegistrationWaitlistedDomainEvent DomainEvent { get; }
    
    public RegistrationWaitlistedNotification(RegistrationWaitlistedDomainEvent domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}

public class WaitlistPositionChangedNotification : INotification
{
    public WaitlistPositionChangedDomainEvent DomainEvent { get; }
    
    public WaitlistPositionChangedNotification(WaitlistPositionChangedDomainEvent domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}

public class WaitlistRemovalNotification : INotification
{
    public WaitlistRemovalDomainEvent DomainEvent { get; }
    
    public WaitlistRemovalNotification(WaitlistRemovalDomainEvent domainEvent)
    {
        DomainEvent = domainEvent ?? throw new ArgumentNullException(nameof(domainEvent));
    }
}
