using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Infrastructure.Data;

public sealed class OutboxProcessor
{
    private readonly LiveEventDbContext _dbContext;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(LiveEventDbContext dbContext, ILogger<OutboxProcessor> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var messages = await _dbContext.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending && (m.NextAttemptAt == null || m.NextAttemptAt <= now))
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return 0;
        }

        int processed = 0;
        foreach (var message in messages)
        {
            try
            {
                var type = Type.GetType(message.EventType, throwOnError: false);
                if (type == null)
                {
                    throw new InvalidOperationException($"Unknown event type: {message.EventType}");
                }

                _logger.LogDebug("Processing outbox message {MessageId} of type {EventType}", message.Id, type.Name);

                // Placeholder: publish to external bus / rehydrate and handle
                message.Status = OutboxStatus.Processed;
                message.LastError = null;
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                message.Status = OutboxStatus.Pending;
                message.TryCount += 1;
                message.LastError = ex.Message;
                var delaySec = Math.Min(300, 5 * Math.Max(1, message.TryCount));
                message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySec);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return processed;
    }
}


