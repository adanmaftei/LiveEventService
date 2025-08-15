namespace LiveEventService.API.Configuration;

/// <summary>
/// Database initialization options for development scenarios.
/// </summary>
public sealed class DatabaseOptions
{
    public bool InitializeOnStartup { get; set; } = true;
}
