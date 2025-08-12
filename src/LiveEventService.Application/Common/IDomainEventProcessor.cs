using LiveEventService.Core.Common;

namespace LiveEventService.Application.Common;

/// <summary>
/// Interface for processing domain events in the background
/// </summary>
public interface IDomainEventProcessor
{
    /// <summary>
    /// Processes a domain event asynchronously
    /// </summary>
    /// <param name="domainEvent">The domain event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ProcessAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if the processor can handle the given domain event type
    /// </summary>
    /// <param name="domainEventType">The type of domain event</param>
    /// <returns>True if the processor can handle this event type, false otherwise</returns>
    bool CanProcess(Type domainEventType);
} 