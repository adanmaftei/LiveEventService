using LiveEventService.Core.Common;

namespace LiveEventService.Core.Events;

/// <summary>
/// Repository abstraction for the <see cref="Event"/> aggregate root with event-specific queries.
/// </summary>
public interface IEventRepository : IRepository<Event>
{
    /// <summary>Checks if a user is registered for the specified event.</summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> IsUserRegisteredForEventAsync(Guid eventId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Gets the number of registrations for the specified event.</summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<int> GetRegistrationCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Gets the number of waitlisted registrations for the specified event.</summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<int> GetWaitlistCountForEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Calculates the waitlist position of a registration.</summary>
    /// <param name="eventId">The event identifier.</param>
    /// <param name="registrationId">The registration identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<int> CalculateWaitlistPositionAsync(Guid eventId, Guid registrationId, CancellationToken cancellationToken = default);
}
