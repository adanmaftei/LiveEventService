namespace LiveEventService.API.Configuration;

public sealed class GraphQLOptions
{
    public int MaxExecutionDepth { get; set; } = 10;
    public int ExecutionTimeoutSeconds { get; set; } = 10;
    public bool StrictValidation { get; set; } = true;
}


