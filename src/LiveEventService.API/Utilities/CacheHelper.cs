using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using LiveEventService.Infrastructure.Telemetry;

namespace LiveEventService.API.Utilities;

public static class CacheHelper
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);

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
            var value = JsonSerializer.Deserialize<T>(bytes, s_jsonOptions);
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
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, s_jsonOptions);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };
        await cache.SetAsync(key, bytes, options, ct);
        AppMetrics.CacheSets.Add(1);
    }
}


