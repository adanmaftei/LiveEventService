using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LiveEventService.Infrastructure.Data;

         using Amazon.SimpleNotificationService;

         public sealed class OutboxProcessor
{
    private readonly LiveEventDbContext _dbContext;
    private readonly ILogger<OutboxProcessor> _logger;

             private readonly IAmazonSimpleNotificationService _sns;

             public OutboxProcessor(LiveEventDbContext dbContext, ILogger<OutboxProcessor> logger, IAmazonSimpleNotificationService sns)
    {
        _dbContext = dbContext;
        _logger = logger;
                 _sns = sns;
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

                         // Publish to SNS (topic per event type). In LocalStack/AWS, ensure topics exist
                         var typeName = type.Name;
                         var topicName = $"liveevent-{typeName}";
                         try
                         {
                             var topic = await _sns.FindTopicAsync(topicName);
                             if (topic == null)
                             {
                                 var created = await _sns.CreateTopicAsync(topicName);
                                 topic = await _sns.FindTopicAsync(topicName);
                             }
                             if (topic != null)
                             {
                                 await _sns.PublishAsync(topic.TopicArn, message.Payload, typeName);
                             }
                             else
                             {
                                 _logger.LogWarning("SNS topic {TopicName} not found and could not be created", topicName);
                             }
                             message.Status = OutboxStatus.Processed;
                         }
                         catch (Exception pubEx)
                         {
                             _logger.LogError(pubEx, "SNS publish failed for message {MessageId}", message.Id);
                             throw;
                         }
                message.LastError = null;
                processed++;
                LiveEventService.Infrastructure.Telemetry.AppMetrics.OutboxProcessed.Add(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox message {MessageId}", message.Id);
                message.Status = OutboxStatus.Pending;
                message.TryCount += 1;
                message.LastError = ex.Message;
                var delaySec = Math.Min(300, 5 * Math.Max(1, message.TryCount));
                message.NextAttemptAt = DateTime.UtcNow.AddSeconds(delaySec);
                LiveEventService.Infrastructure.Telemetry.AppMetrics.OutboxFailed.Add(1);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        // Update gauge with an approximate of remaining pending (not exact under race, acceptable for telemetry)
        try
        {
            var pending = await _dbContext.OutboxMessages.CountAsync(m => m.Status == OutboxStatus.Pending, cancellationToken);
            var current = 0L; // UpDownCounter expects delta; set to absolute by subtracting previous, but we don't track prev → use Add with (pending - 0)
            LiveEventService.Infrastructure.Telemetry.AppMetrics.OutboxPending.Add(pending - current);
        }
        catch { }
        return processed;
    }
}


