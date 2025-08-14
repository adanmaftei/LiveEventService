using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Core.Common;

public interface IEventRegistrationNotifier
{
    Task NotifyAsync(EventRegistrationEntity registration, string action, CancellationToken cancellationToken = default);
}
