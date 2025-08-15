using LiveEventService.Core.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveEventService.Application.Common;

/// <summary>
/// Background service for processing domain events asynchronously.
/// </summary>
public class DomainEventBackgroundService : BackgroundService
{
    private readonly IMessageQueue _messageQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventBackgroundService> _logger;
    private readonly BackgroundProcessingOptions _options;
    private readonly SemaphoreSlim _semaphore;

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainEventBackgroundService"/> class.
    /// </summary>
    /// <param name="messageQueue">Queue from which domain events are dequeued for processing.</param>
    /// <param name="serviceProvider">Service provider used to resolve scoped processors.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="options">Options controlling concurrency and retry behavior.</param>
    public DomainEventBackgroundService(
        IMessageQueue messageQueue,
        IServiceProvider serviceProvider,
        ILogger<DomainEventBackgroundService> logger,
        IOptions<BackgroundProcessingOptions> options)
    {
        _messageQueue = messageQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
        _semaphore = new SemaphoreSlim(_options.MaxConcurrency);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Domain event background service started with max concurrency: {MaxConcurrency}",
            _options.MaxConcurrency);

        var tasks = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for available semaphore slot
                await _semaphore.WaitAsync(stoppingToken);

                // Start processing task
                var task = ProcessEventsAsync(stoppingToken);
                tasks.Add(task);

                // Clean up completed tasks
                tasks.RemoveAll(t => t.IsCompleted);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in domain event background service main loop");
                await Task.Delay(1000, stoppingToken); // Brief delay before retry
            }
        }

        // Wait for all remaining tasks to complete
        await Task.WhenAll(tasks);
        _logger.LogInformation("Domain event background service stopped");
    }

    /// <summary>
    /// Worker loop that continuously dequeues and processes domain events.
    /// </summary>
    private async Task ProcessEventsAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var domainEvent = await _messageQueue.DequeueAsync(stoppingToken);
                if (domainEvent == null)
                {
                    // No events in queue, brief delay before checking again
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                await ProcessDomainEventAsync(domainEvent, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal cancellation, no logging needed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain events in background service");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Processes a single domain event by delegating to a matching <see cref="IDomainEventProcessor"/>.
    /// </summary>
    private async Task ProcessDomainEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogDebug("Processing domain event {EventType} in background", eventType.Name);

            // Find the appropriate processor for this event type
            using var scope = _serviceProvider.CreateScope();
            var processors = scope.ServiceProvider.GetServices<IDomainEventProcessor>();

            var processor = processors.FirstOrDefault(p => p.CanProcess(eventType));
            if (processor == null)
            {
                _logger.LogWarning("No processor found for domain event type {EventType}", eventType.Name);
                return;
            }

            // Process with retry logic
            await ProcessWithRetryAsync(processor, domainEvent, cancellationToken);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogDebug("Successfully processed domain event {EventType} in {Duration}ms",
                eventType.Name, duration.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to process domain event {EventType} after {Duration}ms",
                eventType.Name, duration.TotalMilliseconds);

            // Could implement dead letter queue here for failed events
            throw;
        }
    }

    /// <summary>
    /// Executes processing with retry semantics and optional exponential backoff.
    /// </summary>
    private async Task ProcessWithRetryAsync(IDomainEventProcessor processor, DomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var retryCount = 0;
        var delay = _options.RetryDelaySeconds;

        while (retryCount <= _options.MaxRetryAttempts)
        {
            try
            {
                await processor.ProcessAsync(domainEvent, cancellationToken);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (retryCount < _options.MaxRetryAttempts)
            {
                retryCount++;
                _logger.LogWarning(ex, "Attempt {RetryCount} failed for domain event {EventType}, retrying in {Delay}s",
                    retryCount, eventType.Name, delay);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken);

                if (_options.UseExponentialBackoff)
                {
                    delay *= 2; // Exponential backoff
                }
            }
        }

        // All retries exhausted
        throw new InvalidOperationException($"Failed to process domain event {eventType.Name} after {_options.MaxRetryAttempts} attempts");
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _semaphore?.Dispose();
        base.Dispose();
    }
}

/// <summary>
/// Configuration options for background processing.
/// </summary>
public class BackgroundProcessingOptions
{
    /// <summary>
    /// Gets or sets maximum number of concurrent domain event processing tasks.
    /// </summary>
    public int MaxConcurrency { get; set; } = 4;

    /// <summary>
    /// Gets or sets maximum number of retry attempts for failed processing.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets delay between retry attempts in seconds.
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether whether to use exponential backoff for retries.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}
