namespace LiveEventService.Infrastructure.Configuration;

public sealed class AwsOptions
{
    public string? Region { get; set; }
    public string? ServiceURL { get; set; }
    public string? S3BucketName { get; set; }
    public string? UserPoolId { get; set; }
    public CloudWatchOptions CloudWatch { get; set; } = new();
    public JwtOptions Jwt { get; set; } = new();
    public SqsOptions Sqs { get; set; } = new();
}

public sealed class CloudWatchOptions
{
    public string? Region { get; set; }
    public string? LogGroup { get; set; }
    public string? AuditLogGroup { get; set; }
}

public sealed class JwtOptions
{
    public string[] Audiences { get; set; } = Array.Empty<string>();
}

public sealed class SqsOptions
{
    public bool UseSqsForDomainEvents { get; set; }
    public string? QueueName { get; set; }
}


 