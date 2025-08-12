using LiveEventService.Application.Common;

namespace LiveEventService.API.Logging;

public sealed class SerilogAuditLogger : IAuditLogger
{
    private readonly Serilog.ILogger _logger;

    public SerilogAuditLogger(Serilog.ILogger logger)
    {
        _logger = logger;
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _logger.Information(
            "AUDIT Action={Action} EntityType={EntityType} EntityId={EntityId} UserId={UserId} Metadata={@Metadata}",
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.UserId,
            entry.Metadata);
        return Task.CompletedTask;
    }
}


