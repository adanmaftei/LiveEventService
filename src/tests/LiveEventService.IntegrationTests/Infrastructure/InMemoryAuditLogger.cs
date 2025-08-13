using LiveEventService.Core.Common;

namespace LiveEventService.IntegrationTests.Infrastructure;

public sealed class InMemoryAuditLogger : IAuditLogger
{
    private readonly List<AuditLogEntry> _entries = new();
    private readonly object _lock = new();

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _entries.Add(entry);
        }
        return Task.CompletedTask;
    }

    public IReadOnlyList<AuditLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return _entries.ToList();
            }
        }
    }
}


