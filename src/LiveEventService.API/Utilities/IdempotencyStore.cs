using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Utilities;

public interface IIdempotencyStore
{
    Task<bool> TryClaimAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}

public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly IDistributedCache? distributedCache;
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LocalClaims = new();

    public IdempotencyStore(IDistributedCache? distributedCache = null)
    {
        this.distributedCache = distributedCache;
    }

    public async Task<bool> TryClaimAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        // Prefer distributed cache when available
        if (distributedCache != null)
        {
            var existing = await distributedCache.GetAsync(key, ct);
            if (existing != null && existing.Length > 0)
            {
                return false; // already claimed
            }

            // Best-effort claim marker (race window exists without atomic add/Lua)
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            await distributedCache.SetStringAsync(key, "1", options, ct);
            return true;
        }

        // Fallback to in-memory for Testing
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(ttl);
        var added = LocalClaims.TryAdd(key, expiresAt);
        if (!added)
        {
            // Clean expired and retry once
            foreach (var kv in LocalClaims.ToArray())
            {
                if (kv.Value <= now)
                {
                    LocalClaims.TryRemove(kv.Key, out _);
                }
            }
            return LocalClaims.TryAdd(key, expiresAt);
        }
        return true;
    }
}


