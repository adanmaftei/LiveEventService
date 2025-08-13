using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using LiveEventService.Core.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Infrastructure.Messaging;

public sealed class SqsMessageQueue : IMessageQueue, IDisposable
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<SqsMessageQueue> _logger;
    private readonly string _queueUrl;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public SqsMessageQueue(IAmazonSQS sqs, IConfiguration configuration, ILogger<SqsMessageQueue> logger)
    {
        _sqs = sqs;
        _logger = logger;
        var queueName = configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";
        // Resolve queue URL on startup; assume the queue exists (LocalStack/infra should provision).
        var urlResponse = _sqs.GetQueueUrlAsync(queueName).GetAwaiter().GetResult();
        _queueUrl = urlResponse.QueueUrl;
    }

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
    public Task<DomainEvent?> DequeueAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<DomainEvent?>(null);

    public int GetQueueLength() => 0; // Not supported cheaply; could use ApproximateNumberOfMessages

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

