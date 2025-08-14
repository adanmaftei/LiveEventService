namespace LiveEventService.API.Configuration;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddDefaultResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler();
        return builder;
    }
}


