using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace LiveEventService.API.Configuration;

public static class HttpClientExtensions
{
    public static IHttpClientBuilder AddDefaultResilience(this IHttpClientBuilder builder)
    {
        builder.AddStandardResilienceHandler();
        return builder;
    }
}


