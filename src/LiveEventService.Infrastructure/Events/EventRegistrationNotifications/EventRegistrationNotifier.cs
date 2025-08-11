using LiveEventService.Core.Common;
using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Infrastructure.Events.EventRegistrationNotifications;

public class EventRegistrationNotifier : IEventRegistrationNotifier
{
    public async Task NotifyAsync(EventRegistrationEntity registration, string action, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual notification logic
        // This could be email, SMS, push notification, etc.
        // For now, we'll just log the notification
        await Task.CompletedTask;
    }
}