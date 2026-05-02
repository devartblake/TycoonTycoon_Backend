using Microsoft.Extensions.Caching.Distributed;
using Synaptix.Security.Kms.Application.Abstractions;

namespace Synaptix.Security.Kms.Infrastructure.Redis;

/// Tracks nonces and sequence numbers per session to reject replay attacks.
/// Redis key pattern (matches handoff spec):
///   synsec:session:{sessionId}:seq:{sequence}   TTL = ttl
///   synsec:session:{sessionId}:nonce:{nonce}    TTL = ttl
public sealed class RedisReplayProtectionStore(IDistributedCache cache) : IReplayProtectionStore
{
    private static readonly byte[] Marker = [1];

    public async Task<bool> TryAcceptAsync(
        Guid sessionId, long sequence, string nonce, TimeSpan ttl, CancellationToken ct)
    {
        var seqKey = $"synsec:session:{sessionId:N}:seq:{sequence}";
        var nonceKey = $"synsec:session:{sessionId:N}:nonce:{nonce}";
        var opts = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl };

        // Check and set sequence
        if (await cache.GetAsync(seqKey, ct) is not null) return false;
        await cache.SetAsync(seqKey, Marker, opts, ct);

        // Check and set nonce
        if (await cache.GetAsync(nonceKey, ct) is not null)
        {
            // Sequence was new but nonce was reused — clean up the sequence entry we just set
            await cache.RemoveAsync(seqKey, ct);
            return false;
        }
        await cache.SetAsync(nonceKey, Marker, opts, ct);

        return true;
    }
}
