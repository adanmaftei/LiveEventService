namespace LiveEventService.Application.Configuration;

/// <summary>
/// Root-level option controlling whether background processing is handled in-process
/// by the API/Worker host or delegated to external infrastructure.
/// </summary>
public sealed class BackgroundProcessingRootOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether when true, use in-process background workers; when false, expect external processors.
    /// </summary>
    public bool UseInProcess { get; set; } = true;
}
