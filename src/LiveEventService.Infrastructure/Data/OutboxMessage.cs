using System.Text.Json;

namespace LiveEventService.Infrastructure.Data;

/// <summary>
/// Processing state for outbox messages.
/// </summary>
public enum OutboxStatus
{
    /// <summary>
    /// Message is waiting to be processed.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Message has been successfully processed.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Message processing failed.
    /// </summary>
    Failed = 3
}

/// <summary>
/// Outbox message record used to persist domain events for reliable publication
/// to external transports (e.g., SNS/SQS) outside the transaction boundary.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
        = DateTime.UtcNow;

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int TryCount { get; set; } = 0;
    public string? LastError { get; set; }
        = null;
    public DateTime? NextAttemptAt { get; set; }
        = null;

    // Leasing/claiming fields for safe concurrent processing
    public string? ClaimedBy { get; set; }
        = null;
    public DateTime? ClaimedAt { get; set; }
        = null;

    /// <summary>
    /// Creates a new <see cref="OutboxMessage"/> from a domain event instance,
    /// capturing its type and serialized payload.
    /// </summary>
    /// <param name="domainEvent">The domain event to convert to an outbox message.</param>
    /// <param name="options">Optional JSON serialization options.</param>
    /// <returns>A new outbox message containing the serialized domain event.</returns>
    public static OutboxMessage FromDomainEvent(object domainEvent, JsonSerializerOptions? options = null)
    {
        if (domainEvent == null)
        {
            throw new ArgumentNullException(nameof(domainEvent));
        }

        return new OutboxMessage
        {
            EventType = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? string.Empty,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), options ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }),
            OccurredOn = DateTime.UtcNow,
            Status = OutboxStatus.Pending,
            TryCount = 0,
            NextAttemptAt = DateTime.UtcNow
        };
    }
}
