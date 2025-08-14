namespace LiveEventService.API.Configuration;

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}


