using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LiveEventService.Infrastructure.HealthChecks;

/// <summary>
/// Confirms that the configured SQS queue URL can be resolved.
/// </summary>
public sealed class SqsHealthCheck : IHealthCheck
{
    private readonly IAmazonSQS sqs;
    private readonly IConfiguration configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsHealthCheck"/> class.
    /// </summary>
    /// <param name="sqs">The SQS client for checking queue availability.</param>
    /// <param name="configuration">The configuration containing SQS settings.</param>
    public SqsHealthCheck(IAmazonSQS sqs, IConfiguration configuration)
    {
        this.sqs = sqs;
        this.configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var queueName = configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";
            if (string.IsNullOrWhiteSpace(queueName))
            {
                return HealthCheckResult.Healthy("SQS not required (no queue configured)");
            }

            var response = await sqs.GetQueueUrlAsync(queueName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(response.QueueUrl))
            {
                return HealthCheckResult.Healthy("SQS reachable");
            }

            return HealthCheckResult.Unhealthy("SQS queue URL could not be resolved");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQS health check failed", ex);
        }
    }
}
