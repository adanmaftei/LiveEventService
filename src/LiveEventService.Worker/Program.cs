using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using LiveEventService.Application;
using LiveEventService.Core.Common;
using LiveEventService.Infrastructure;
using LiveEventService.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection("AWS"));
builder.Services.AddHostedService<SqsWorker>();

var app = builder.Build();
await app.RunAsync();

public sealed class SqsWorker : BackgroundService
{
    private readonly ILogger<SqsWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly IAmazonSQS _sqs;
    private string _queueUrl;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AwsOptions _awsOptions;

    public SqsWorker(ILogger<SqsWorker> logger, IServiceProvider services, IAmazonSQS sqs, Microsoft.Extensions.Options.IOptions<AwsOptions> awsOptions, IConfiguration configuration)
    {
        _logger = logger;
        _services = services;
        _sqs = sqs;
        _awsOptions = awsOptions.Value;
        var queueName = _awsOptions.Sqs.QueueName ?? configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";
        _queueUrl = queueName; // resolve in StartAsync
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve queue URL asynchronously at startup
        if (!string.IsNullOrWhiteSpace(_queueUrl) && !_queueUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var resp = await _sqs.GetQueueUrlAsync(_queueUrl, cancellationToken);
                _queueUrl = resp.QueueUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve SQS queue URL for {Queue}", _queueUrl);
                throw;
            }
        }
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var resp = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 10,
                    VisibilityTimeout = 30
                }, stoppingToken);

                if (resp.Messages.Count == 0)
                {
                    continue;
                }

                foreach (var msg in resp.Messages)
                {
                    if (await HandleMessageAsync(msg, stoppingToken))
                    {
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SQS polling loop");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogInformation("SQS worker stopped");
    }

    private async Task<bool> HandleMessageAsync(Message msg, CancellationToken ct)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<DomainEventEnvelope>(msg.Body, _jsonOptions);
            if (envelope is null || string.IsNullOrWhiteSpace(envelope.EventType))
            {
                _logger.LogWarning("Invalid message format");
                return true; // drop
            }

            var type = Type.GetType(envelope.EventType, throwOnError: false);
            if (type == null || !typeof(DomainEvent).IsAssignableFrom(type))
            {
                _logger.LogWarning("Unknown event type: {Type}", envelope.EventType);
                return true; // drop
            }

            var domainEvent = (DomainEvent?)JsonSerializer.Deserialize(envelope.Payload, type, _jsonOptions);
            if (domainEvent == null)
            {
                _logger.LogWarning("Failed to deserialize payload for type {Type}", envelope.EventType);
                return true; // drop
            }

            using var scope = _services.CreateScope();
            var processors = scope.ServiceProvider.GetServices<IDomainEventProcessor>();
            var processor = processors.FirstOrDefault(p => p.CanProcess(type));
            if (processor == null)
            {
                _logger.LogWarning("No processor found for event type {Type}", type.Name);
                return true; // drop
            }

            await processor.ProcessAsync(domainEvent, ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SQS message");
            return false; // keep for retry via visibility timeout
        }
    }

    private sealed class DomainEventEnvelope
    {
        public string EventType { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}

