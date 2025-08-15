using Amazon;
using Amazon.CloudWatchLogs;
using LiveEventService.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.AwsCloudWatch;

namespace LiveEventService.Infrastructure.Telemetry;

/// <summary>
/// Serilog configuration helpers for writing structured logs to AWS CloudWatch Logs.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds Serilog CloudWatch sink based on AWS configuration.
    /// Intended to be called from the composition root in production environments.
    /// </summary>
    /// <param name="loggerConfiguration">Serilog logger configuration.</param>
    /// <param name="appConfiguration">App configuration (reads AWS settings).</param>
    /// <returns>The logger configuration for chaining.</returns>
    public static LoggerConfiguration WriteToCloudWatch(this LoggerConfiguration loggerConfiguration, IConfiguration appConfiguration)
    {
        var aws = appConfiguration.GetSection("AWS").Get<AwsOptions>() ?? new AwsOptions();
        var logGroup = aws.CloudWatch.LogGroup ?? "/live-event-service/logs";
        var region = aws.CloudWatch.Region ?? aws.Region ?? "us-east-1";
        var cwClient = new AmazonCloudWatchLogsClient(RegionEndpoint.GetBySystemName(region));

        return loggerConfiguration.WriteTo.AmazonCloudWatch(
            logGroup: logGroup,
            logStreamPrefix: "live-event-service-",
            cloudWatchClient: cwClient,
            textFormatter: new JsonFormatter(),
            createLogGroup: true);
    }
}
