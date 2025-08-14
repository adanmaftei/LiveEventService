namespace LiveEventService.API.Configuration;

public sealed class SecurityOptions
{
    public CspOptions Csp { get; set; } = new();
}

public sealed class CspOptions
{
    public bool Enabled { get; set; }
    public string? Policy { get; set; }
}


