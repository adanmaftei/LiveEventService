using System.Text.Json;
using LiveEventService.Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Utilities;

public static class CacheHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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


