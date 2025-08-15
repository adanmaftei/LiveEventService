using System.Text.Json;
using LiveEventService.Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Utilities;

/// <summary>
/// Helper for typed distributed cache get/set with JSON serialization.
/// Emits cache hit/miss/set metrics via <see cref="AppMetrics"/>.
/// </summary>
public static class CacheHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Attempts to get a value from the cache, returning a hit flag and the value.
    /// Increments cache hit/miss metrics accordingly.
    /// </summary>
    /// <typeparam name="T">The cached value type.</typeparam>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A tuple indicating whether the read was a hit and the value if present.</returns>
    public static async Task<(bool hit, T? value)> TryGetAsync<T>(IDistributedCache cache, string key, CancellationToken ct = default)
    {
        var bytes = await cache.GetAsync(key, ct);
        if (bytes == null || bytes.Length == 0)
        {
            AppMetrics.CacheMisses.Add(1);
            return (false, default);
        }
        try
        {
            var value = JsonSerializer.Deserialize<T>(bytes, JsonOptions);
            AppMetrics.CacheHits.Add(1);
            return (true, value);
        }
        catch
        {
            // Treat deserialization failure as miss
            AppMetrics.CacheMisses.Add(1);
            return (false, default);
        }
    }

    /// <summary>
    /// Sets a value in the cache with the given TTL and records a cache set metric.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="cache">The distributed cache instance.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttl">Absolute expiration relative to now.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task SetAsync<T>(IDistributedCache cache, string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await cache.SetAsync(key, bytes, options, ct);
        AppMetrics.CacheSets.Add(1);
    }
}
