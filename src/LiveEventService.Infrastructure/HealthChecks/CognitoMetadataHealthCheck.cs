using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace LiveEventService.Infrastructure.HealthChecks;

/// <summary>
/// Verifies that AWS Cognito OIDC metadata for the configured user pool is reachable.
/// Skips in the testing environment.
/// </summary>
public sealed class CognitoMetadataHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly bool _isTesting;

    /// <summary>
    /// Initializes a new instance of the <see cref="CognitoMetadataHealthCheck"/> class.
    /// Creates a new health check instance.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients to check Cognito metadata.</param>
    /// <param name="configuration">Configuration containing AWS region and user pool ID.</param>
    /// <param name="environment">Host environment to determine if running in testing mode.</param>
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
