using LiveEventService.Core.Common;

namespace LiveEventService.Core.Registrations.EventRegistration;

public class RegistrationWaitlistedDomainEvent : DomainEvent
{
    public EventRegistration Registration { get; }

    public RegistrationWaitlistedDomainEvent(EventRegistration registration)
    {
        Registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }
}
