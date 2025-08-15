namespace LiveEventService.API.Configuration;

/// <summary>
/// Extensions for configuring resilient HTTP clients.
/// </summary>
public static class HttpClientExtensions
{
	/// <summary>
	/// Adds a standard resilience handler to the provided HTTP client builder.
	/// </summary>
	/// <param name="builder">The HTTP client builder.</param>
	/// <returns>The same builder for chaining.</returns>
	public static IHttpClientBuilder AddDefaultResilience(this IHttpClientBuilder builder)
	{
		builder.AddStandardResilienceHandler();
		return builder;
	}
}
