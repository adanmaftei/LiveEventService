namespace LiveEventService.Core.Common;

/// <summary>
/// Base type for domain events raised by aggregate roots and entities.
/// </summary>
public abstract class DomainEvent
{
    /// <summary>
    /// Gets or sets the UTC timestamp when the domain event occurred.
    /// </summary>
    public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;
}
