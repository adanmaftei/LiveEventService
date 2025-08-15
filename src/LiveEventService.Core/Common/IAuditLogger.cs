namespace LiveEventService.Core.Common;

/// <summary>
/// Abstraction for writing audit log entries.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Writes the specified audit log entry.
    /// </summary>
    /// <param name="entry">The audit log entry to be written.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single audit log entry describing a user action on an entity.
/// </summary>
public sealed class AuditLogEntry
{
    /// <summary>Gets or sets action verb, e.g. "Create", "Update", "Delete".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets CLR or domain type name of the target entity.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Gets or sets identifier of the target entity.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Gets or sets identifier of the user who performed the action.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets UTC timestamp for the action.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets optional metadata providing additional context.</summary>
    public IDictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();
}
