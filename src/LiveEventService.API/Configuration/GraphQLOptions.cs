namespace LiveEventService.API.Configuration;

/// <summary>
/// Options controlling GraphQL server execution limits and validation behavior.
/// </summary>
public sealed class GraphQLOptions
{
	/// <summary>Gets or sets the maximum GraphQL execution depth.</summary>
	public int MaxExecutionDepth { get; set; } = 10;

	/// <summary>Gets or sets the request timeout in seconds.</summary>
	public int ExecutionTimeoutSeconds { get; set; } = 10;

	/// <summary>Gets or sets a value indicating whether strict validation is enabled.</summary>
	public bool StrictValidation { get; set; } = true;
}
