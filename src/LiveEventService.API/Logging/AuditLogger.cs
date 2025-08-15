using LiveEventService.Core.Common;

namespace LiveEventService.API.Logging;

public sealed class SerilogAuditLogger : IAuditLogger
{
    private readonly Serilog.ILogger logger;

    public SerilogAuditLogger(Serilog.ILogger logger)
    {
        this.logger = logger;
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        logger.Information(
            "AUDIT Action={Action} EntityType={EntityType} EntityId={EntityId} UserId={UserId} Metadata={@Metadata}",
            entry.Action,
            entry.EntityType,
            entry.EntityId,
            entry.UserId,
            entry.Metadata);
        return Task.CompletedTask;
    }
}
