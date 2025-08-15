using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Microsoft.Extensions.DependencyInjection;

namespace LiveEventService.Infrastructure.HealthChecks;

/// <summary>
/// Checks connectivity to an optional Redis multiplexer when one is registered in DI.
/// Skips in the testing environment.
/// </summary>
public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly bool _isTesting;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisHealthCheck"/> class.
    /// Creates a new health check instance.
    /// </summary>
    /// <param name="services">Service provider to access registered Redis connection multiplexer.</param>
    /// <param name="configuration">Configuration for Redis settings.</param>
    /// <param name="environment">Host environment to determine if running in testing mode.</param>
    public RedisHealthCheck(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
    {
        _services = services;
        _configuration = configuration;
        _isTesting = environment.IsEnvironment("Testing");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_isTesting)
        {
            return HealthCheckResult.Healthy("Skipped in testing environment");
        }

        var mux = _services.GetService<IConnectionMultiplexer>();
        if (mux == null)
        {
            // Redis not configured in this environment
            return HealthCheckResult.Healthy("Redis not configured");
        }

        try
        {
            if (!mux.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis not connected");
            }

            // Lightweight ping to default database
            var db = mux.GetDatabase();
            var ping = await db.PingAsync();
            return ping >= TimeSpan.Zero
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Redis ping failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(ex.Message);
        }
    }
}
