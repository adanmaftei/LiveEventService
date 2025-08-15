using LiveEventService.Core.Common;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Application.Common;

/// <summary>
/// In-memory implementation of <see cref="IMessageQueue"/> for development and testing.
/// Suitable for single-instance scenarios; not for production.
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly Queue<DomainEvent> _queue = new();
    private readonly object _lockObject = new();
    private readonly ILogger<InMemoryMessageQueue> _logger;
    private readonly SemaphoreSlim _semaphore = new(0);

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryMessageQueue"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
    public InMemoryMessageQueue(ILogger<InMemoryMessageQueue> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a domain event to the in-memory queue.
    /// </summary>
    /// <param name="domainEvent">The domain event to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        lock (_lockObject)
        {
            _queue.Enqueue(domainEvent);
            _semaphore.Release();

            _logger.LogDebug("Domain event {EventType} enqueued. Queue length: {QueueLength}",
                domainEvent.GetType().Name, _queue.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes and returns the next domain event from the queue, waiting if empty.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="Task"/> containing the dequeued domain event, or null if the queue is empty.</returns>
    public async Task<DomainEvent?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        lock (_lockObject)
        {
            if (_queue.TryDequeue(out var domainEvent))
            {
                _logger.LogDebug("Domain event {EventType} dequeued. Queue length: {QueueLength}",
                    domainEvent.GetType().Name, _queue.Count);
                return domainEvent;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the current number of items in the queue.
    /// </summary>
    /// <returns>The number of items currently in the queue.</returns>
    public int GetQueueLength()
    {
        lock (_lockObject)
        {
            return _queue.Count;
        }
    }

    /// <summary>
    /// Indicates whether the queue is currently empty.
    /// </summary>
    /// <returns>True if the queue is empty; otherwise false.</returns>
    public bool IsEmpty()
    {
        lock (_lockObject)
        {
            return _queue.Count == 0;
        }
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
