namespace LiveEventService.Application.Common;

/// <summary>
/// Attribute to mark domain event handlers that should be processed asynchronously
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AsyncProcessingAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the priority of the async processing (lower numbers = higher priority)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed processing
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts in seconds
    /// </summary>
    public int RetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets whether to use exponential backoff for retries
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
}
