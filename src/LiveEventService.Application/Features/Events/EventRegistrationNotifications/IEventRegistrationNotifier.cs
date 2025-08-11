using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Application.Features.Events.EventRegistration.Notifications;

public interface IEventRegistrationNotifier
{
    Task NotifyAsync(EventRegistrationEntity registration, string action, CancellationToken cancellationToken = default);
} 