using System.Text.Json;

namespace LiveEventService.Infrastructure.Data;

public enum OutboxStatus
{
    Pending = 0,
    Processed = 1,
    Failed = 2
}

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

    public static OutboxMessage FromDomainEvent(object domainEvent, JsonSerializerOptions? options = null)
    {
        if (domainEvent == null) throw new ArgumentNullException(nameof(domainEvent));
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


