namespace LiveEventService.API.Configuration;

/// <summary>
/// Security options for the API.
/// </summary>
public sealed class SecurityOptions
{
	/// <summary>Gets or sets the Content Security Policy options.</summary>
	public CspOptions Csp { get; set; } = new();
}

/// <summary>
/// Content Security Policy settings.
/// </summary>
public sealed class CspOptions
{
	/// <summary>Gets or sets a value indicating whether CSP is enabled.</summary>
	public bool Enabled { get; set; }

	/// <summary>Gets or sets the CSP policy string.</summary>
	public string? Policy { get; set; }
}
