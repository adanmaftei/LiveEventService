using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Distributed;

namespace LiveEventService.API.Utilities;

/// <summary>
/// Contract for claiming idempotency keys to guard against duplicate request processing.
/// Implementations may use a distributed cache for cross-instance coordination.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Attempts to claim an idempotency key for a specified time window.
    /// </summary>
    /// <param name="key">The idempotency key to claim.</param>
    /// <param name="ttl">The time-to-live for the claim.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>True if the key was successfully claimed; otherwise false.</returns>
    Task<bool> TryClaimAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}

/// <summary>
/// Default idempotency store using <see cref="IDistributedCache"/> when available,
/// with an in-memory fallback for testing scenarios.
/// </summary>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LocalClaims = new();
    private readonly IDistributedCache? distributedCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyStore"/> class.
    /// </summary>
    /// <param name="distributedCache">Optional distributed cache used to coordinate claims across instances.</param>
    public IdempotencyStore(IDistributedCache? distributedCache = null)
    {
        this.distributedCache = distributedCache;
    }

    /// <summary>
    /// Attempts to claim an idempotency key for the given TTL.
    /// Returns false if the key is already claimed.
    /// </summary>
    /// <param name="key">The idempotency key.</param>
    /// <param name="ttl">The time-to-live for the claim.</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>True if the claim is acquired; otherwise false.</returns>
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
