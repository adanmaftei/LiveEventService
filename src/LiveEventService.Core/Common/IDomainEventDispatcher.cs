namespace LiveEventService.Core.Common;

/// <summary>
/// Dispatches domain events raised by entities and clears them afterwards.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all domain events for the provided entities and clears them to avoid reprocessing.
    /// </summary>
    /// <param name="entities">The entities whose domain events should be dispatched.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DispatchAndClearEventsAsync(IEnumerable<Entity> entities, CancellationToken cancellationToken = default);
}
