using MediatR;
using LiveEventService.Core.Events;
using LiveEventService.Core.Registrations.EventRegistration;

namespace LiveEventService.Application.Common.Notifications;

/// <summary>
/// Adapter notifications for waitlist-related domain events so they can be published via MediatR
/// while keeping the domain model independent from infrastructure concerns.
/// </summary>
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
