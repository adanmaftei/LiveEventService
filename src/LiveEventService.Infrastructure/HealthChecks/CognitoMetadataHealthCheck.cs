using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Net.Http;

namespace LiveEventService.Infrastructure.HealthChecks;

public sealed class CognitoMetadataHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly bool _isTesting;

    public CognitoMetadataHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration, IHostEnvironment environment)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _isTesting = environment.IsEnvironment("Testing");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_isTesting)
        {
            return HealthCheckResult.Healthy("Skipped in testing environment");
        }

        var region = _configuration["AWS:Region"];
        var userPoolId = _configuration["AWS:UserPoolId"];
        if (string.IsNullOrWhiteSpace(region) || string.IsNullOrWhiteSpace(userPoolId))
        {
            return HealthCheckResult.Unhealthy("Cognito configuration missing");
        }

        var url = $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}/.well-known/openid-configuration";
        try
        {
            var client = _httpClientFactory.CreateClient("HealthChecks");
            client.Timeout = TimeSpan.FromSeconds(2);
            using var response = await client.GetAsync(url, cancellationToken);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy($"Status code: {(int)response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}


