using LiveEventService.Core.Common;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Application.Common;

/// <summary>
/// In-memory implementation of IMessageQueue for development and testing
/// </summary>
public class InMemoryMessageQueue : IMessageQueue
{
    private readonly Queue<DomainEvent> _queue = new();
    private readonly object _lockObject = new();
    private readonly ILogger<InMemoryMessageQueue> _logger;
    private readonly SemaphoreSlim _semaphore = new(0);

    public InMemoryMessageQueue(ILogger<InMemoryMessageQueue> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent == null)
            throw new ArgumentNullException(nameof(domainEvent));

        lock (_lockObject)
        {
            _queue.Enqueue(domainEvent);
            _semaphore.Release();

            _logger.LogDebug("Domain event {EventType} enqueued. Queue length: {QueueLength}",
                domainEvent.GetType().Name, _queue.Count);
        }

        return Task.CompletedTask;
    }

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

    public int GetQueueLength()
    {
        lock (_lockObject)
        {
            return _queue.Count;
        }
    }

    public bool IsEmpty()
    {
        lock (_lockObject)
        {
            return _queue.Count == 0;
        }
    }

    public void Dispose()
    {
        _semaphore?.Dispose();
    }
}
