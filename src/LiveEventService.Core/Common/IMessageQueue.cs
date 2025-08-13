namespace LiveEventService.Core.Common;

/// <summary>
/// Interface for message queue operations used in background processing
/// </summary>
public interface IMessageQueue
{
    /// <summary>
    /// Enqueues a domain event for background processing
    /// </summary>
    /// <param name="domainEvent">The domain event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dequeues the next domain event for processing
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The next domain event to process, or null if none available</returns>
    Task<DomainEvent?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current queue length
    /// </summary>
    /// <returns>Number of items in the queue</returns>
    int GetQueueLength();
    
    /// <summary>
    /// Checks if the queue is empty
    /// </summary>
    /// <returns>True if the queue is empty, false otherwise</returns>
    bool IsEmpty();
} 
