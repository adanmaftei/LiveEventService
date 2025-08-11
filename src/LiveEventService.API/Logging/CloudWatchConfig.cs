using Amazon.CloudWatchLogs;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.AwsCloudWatch;

namespace LiveEventService.API.Logging;

public static class CloudWatchConfig
{
    public static ILoggingBuilder AddCloudWatchLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        var logGroup = configuration["AWS:CloudWatch:LogGroup"] ?? "/live-event-service/logs";
        var region = configuration["AWS:Region"] ?? "us-east-1";
        var logLevel = configuration["Logging:LogLevel:Default"] ?? "Information";
        
        var logEventLevel = logLevel switch
        {
            "Trace" => LogEventLevel.Verbose,
            "Debug" => LogEventLevel.Debug,
            "Information" => LogEventLevel.Information,
            "Warning" => LogEventLevel.Warning,
            "Error" => LogEventLevel.Error,
            "Critical" => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };

        var client = new AmazonCloudWatchLogsClient(new AmazonCloudWatchLogsConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
        });

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(logEventLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "LiveEventService")
            .Enrich.WithMachineName()
            .WriteTo.Console()
            .WriteTo.AmazonCloudWatch(
                logGroup: logGroup,
                logStreamPrefix: "live-event-service-",
                cloudWatchClient: client,
                textFormatter: new Serilog.Formatting.Json.JsonFormatter(),
                createLogGroup: true,
                logGroupRetentionPolicy: LogGroupRetentionPolicy.OneYear,
                batchSizeLimit: 100,
                queueSizeLimit: 10000)
            .CreateLogger();

        return logging.AddSerilog(Log.Logger);
    }
}
