using EventRegistrationEntity = LiveEventService.Core.Registrations.EventRegistration.EventRegistration;

namespace LiveEventService.Core.Common;

/// <summary>
/// Abstraction for notifying external systems about registration lifecycle events.
/// </summary>
public interface IEventRegistrationNotifier
{
    /// <summary>
    /// Sends a notification for the specified registration action.
    /// </summary>
    /// <param name="registration">The registration entity.</param>
    /// <param name="action">The action being performed (e.g., Created, Cancelled, Promoted).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task NotifyAsync(EventRegistrationEntity registration, string action, CancellationToken cancellationToken = default);
}
