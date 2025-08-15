namespace LiveEventService.Infrastructure.Configuration;

/// <summary>
/// Strongly-typed configuration options for AWS integrations used by the infrastructure layer.
/// Includes region, LocalStack endpoint, resource names, and nested option groups.
/// </summary>
public sealed class AwsOptions
{
    /// <summary>
    /// Gets or sets aWS region identifier (for example, "us-east-1"). When omitted, sensible defaults are used.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets optional custom endpoint (e.g., LocalStack) for AWS SDK clients.
    /// </summary>
    public string? ServiceURL { get; set; }

    /// <summary>
    /// Gets or sets default S3 bucket name used by the application.
    /// </summary>
    public string? S3BucketName { get; set; }

    /// <summary>
    /// Gets or sets cognito User Pool Id used for JWT metadata discovery in health checks.
    /// </summary>
    public string? UserPoolId { get; set; }

    /// <summary>
    /// Gets or sets cloudWatch logging configuration.
    /// </summary>
    public CloudWatchOptions CloudWatch { get; set; } = new();

    /// <summary>
    /// Gets or sets jWT configuration (audiences, etc.).
    /// </summary>
    public JwtOptions Jwt { get; set; } = new();

    /// <summary>
    /// Gets or sets amazon SQS configuration used for domain event transport.
    /// </summary>
    public SqsOptions Sqs { get; set; } = new();
}

/// <summary>
/// Options for configuring Serilog's CloudWatch sink (group and region).
/// </summary>
public sealed class CloudWatchOptions
{
    /// <summary>
    /// Gets or sets cloudWatch region. Falls back to <see cref="AwsOptions.Region"/> if omitted.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets application log group name.
    /// </summary>
    public string? LogGroup { get; set; }

    /// <summary>
    /// Gets or sets dedicated audit log group name, when audit logs are enabled.
    /// </summary>
    public string? AuditLogGroup { get; set; }
}

/// <summary>
/// JWT-related configuration values.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets or sets expected audience values accepted by the API.
    /// </summary>
    public string[] Audiences { get; set; } = Array.Empty<string>();
}

/// <summary>
/// SQS configuration used for domain event publication.
/// </summary>
public sealed class SqsOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether when true, replaces the in-memory queue with an Amazon SQS producer.
    /// </summary>
    public bool UseSqsForDomainEvents { get; set; }

    /// <summary>
    /// Gets or sets the queue name to resolve or create at startup.
    /// </summary>
    public string? QueueName { get; set; }
}
