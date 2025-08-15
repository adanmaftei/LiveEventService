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

/// <summary>
/// Background service that polls an AWS SQS queue for domain event messages and dispatches them
/// to an <see cref="IDomainEventProcessor"/> implementation capable of handling the deserialized event type.
/// </summary>
public sealed class SqsWorker : BackgroundService
{
    private readonly ILogger<SqsWorker> _logger;
    private readonly IServiceProvider _services;
    private readonly IAmazonSQS _sqs;
    private string _queueUrl;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly AwsOptions _awsOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqsWorker"/> class.
    /// Resolves configuration and defers SQS queue URL lookup until <see cref="StartAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="logger">Logger used for operational and error logs.</param>
    /// <param name="services">Application service provider used to create scoped processors.</param>
    /// <param name="sqs">AWS SQS client used for polling and acknowledging messages.</param>
    /// <param name="awsOptions">Typed AWS options binding for queue configuration.</param>
    /// <param name="configuration">Fallback configuration source for AWS settings.</param>
    public SqsWorker(ILogger<SqsWorker> logger, IServiceProvider services, IAmazonSQS sqs, Microsoft.Extensions.Options.IOptions<AwsOptions> awsOptions, IConfiguration configuration)
    {
        _logger = logger;
        _services = services;
        _sqs = sqs;
        _awsOptions = awsOptions.Value;
        var queueName = _awsOptions.Sqs.QueueName ?? configuration["AWS:SQS:QueueName"] ?? "liveevent-domain-events";
        _queueUrl = queueName; // resolve in StartAsync
    }

    /// <summary>
    /// Performs startup initialization including resolving the SQS queue URL if only a queue name was provided.
    /// </summary>
    /// <param name="cancellationToken">Token to observe cancellation requests.</param>
    /// <exception cref="Exception">Thrown when the queue URL cannot be resolved.</exception>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Main execution loop that long-polls SQS for messages, dispatches to processors, and deletes messages upon success.
    /// Uses short delays after transient failures and exits cleanly when cancelled.
    /// </summary>
    /// <param name="stoppingToken">Token that signals the service should stop processing.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Deserializes an incoming message into a domain event and invokes a matching <see cref="IDomainEventProcessor"/>.
    /// Returns a boolean indicating whether the message should be deleted from the queue.
    /// </summary>
    /// <param name="msg">The SQS message to process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the message was handled successfully or should be dropped; otherwise <c>false</c> to retain for retry.</returns>
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

    /// <summary>
    /// Envelope that wraps a serialized domain event. The event is identified by its type name
    /// so it can be rehydrated and routed to the appropriate processor.
    /// </summary>
    private sealed class DomainEventEnvelope
    {
        /// <summary>
        /// Gets or sets assembly-qualified or resolvable type name of the domain event.
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets jSON payload containing the serialized domain event instance.
        /// </summary>
        public string Payload { get; set; } = string.Empty;
    }
}
