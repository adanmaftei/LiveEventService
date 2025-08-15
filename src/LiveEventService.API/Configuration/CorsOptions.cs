namespace LiveEventService.API.Configuration;

/// <summary>
/// CORS configuration values bound from configuration.
/// </summary>
public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
