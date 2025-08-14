namespace LiveEventService.API.Constants;

public static class PolicyNames
{
    public const string General = "general";
    public const string Registration = "registration";
}

public static class OutputCachePolicies
{
    public const string EventListPublic = "EventListPublic";
    public const string EventDetailPublic = "EventDetailPublic";
}

public static class RoutePaths
{
    public const string GraphQL = "/graphql";
    public const string Health = "/health";
    public const string HealthReady = "/health/ready";
    public const string HealthLive = "/health/live";
}

public static class CustomHeaderNames
{
    public const string CorrelationId = "X-Correlation-ID";
}

public static class SecurityHeaderNames
{
    public const string XFrameOptions = "X-Frame-Options";
    public const string XContentTypeOptions = "X-Content-Type-Options";
    public const string ReferrerPolicy = "Referrer-Policy";
    public const string PermissionsPolicy = "Permissions-Policy";
    public const string ContentSecurityPolicy = "Content-Security-Policy";
}


