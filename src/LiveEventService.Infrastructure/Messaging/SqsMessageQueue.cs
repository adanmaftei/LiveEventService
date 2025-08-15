using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using LiveEventService.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Infrastructure.Messaging;

/// <summary>
/// Amazon SQS-based implementation of <see cref="IMessageQueue"/> used to publish domain events.
/// Resolves or creates the target queue at startup; enqueue serializes events to JSON.
/// </summary>
public sealed class SqsMessageQueue : IMessageQueue, IDisposable
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsMessageQueue> _logger;
    private readonly string _queueUrl;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsMessageQueue"/> class.
    /// Initializes the producer and resolves or creates the SQS queue.
    /// </summary>
    /// <param name="sqs">Amazon SQS client.</param>
    /// <param name="configuration">Application configuration (reads queue name from AWS:SQS:QueueName).</param>
    /// <param name="logger">Logger instance.</param>
    public SqsMessageQueue(IAmazonSQS sqs, IConfiguration configuration, ILogger<SqsMessageQueue> logger)
    {
        _sqs = sqs;
        _logger = logger;
        var queueName = configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";

        // Resolve or create queue on startup (handles LocalStack race or missing queue)
        const int maxAttempts = 5;
        var attempt = 0;
        Exception? lastException = null;

        while (attempt < maxAttempts)
        {
            attempt++;
            try
            {
                var urlResponse = _sqs.GetQueueUrlAsync(queueName).GetAwaiter().GetResult();
                _queueUrl = urlResponse.QueueUrl;
                if (attempt > 1)
                {
                    _logger.LogInformation("Resolved SQS queue '{QueueName}' on attempt {Attempt}", queueName, attempt);
                }
                return;
            }
            catch (QueueDoesNotExistException)
            {
                try
                {
                    _logger.LogWarning("SQS queue '{QueueName}' not found. Creating it...", queueName);
                    _ = _sqs.CreateQueueAsync(new CreateQueueRequest
                    {
                        QueueName = queueName
                    }).GetAwaiter().GetResult();

                    var urlResponse = _sqs.GetQueueUrlAsync(queueName).GetAwaiter().GetResult();
                    _queueUrl = urlResponse.QueueUrl;
                    _logger.LogInformation("Created and resolved SQS queue '{QueueName}'", queueName);
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            // brief backoff before retrying
            Thread.Sleep(TimeSpan.FromSeconds(Math.Min(2 * attempt, 5)));
        }

        _logger.LogError(lastException, "Failed to resolve or create SQS queue '{QueueName}' after {Attempts} attempts", queueName, maxAttempts);
        throw lastException ?? new Exception($"Failed to resolve or create SQS queue '{queueName}'");
    }

    /// <inheritdoc />
    public async Task EnqueueAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is null) throw new ArgumentNullException(nameof(domainEvent));

        var envelope = new DomainEventEnvelope
        {
            EventType = domainEvent.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), _jsonOptions)
        };

        var body = JsonSerializer.Serialize(envelope, _jsonOptions);
        var request = new SendMessageRequest
        {
            QueueUrl = _queueUrl,
            MessageBody = body
        };
        var response = await _sqs.SendMessageAsync(request, cancellationToken);
        _logger.LogDebug("Enqueued domain event {EventType} to SQS with MessageId {MessageId}", domainEvent.GetType().Name, response.MessageId);
    }

    // Not used in SQS producer mode; worker will poll. Kept to satisfy interface.

    /// <inheritdoc />
    public Task<DomainEvent?> DequeueAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<DomainEvent?>(null);

    /// <inheritdoc />
    public int GetQueueLength() => 0; // Not supported cheaply; could use ApproximateNumberOfMessages

    /// <inheritdoc />
    public bool IsEmpty() => false;

    public void Dispose()
    {
    }

    private sealed class DomainEventEnvelope
    {
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
