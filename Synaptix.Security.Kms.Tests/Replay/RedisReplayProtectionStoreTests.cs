using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Infrastructure.Redis;

namespace Synaptix.Security.Kms.Tests.Replay;

public sealed class RedisReplayProtectionStoreTests
{
    private static RedisReplayProtectionStore BuildStore()
    {
        IDistributedCache cache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions()));
        return new RedisReplayProtectionStore(cache);
    }

    [Fact]
    public async Task FirstCall_NewSeqAndNonce_ReturnsTrue()
    {
        var store = BuildStore();
        var sessionId = Guid.NewGuid();
        var result = await store.TryAcceptAsync(sessionId, 1L, "nonce-abc", TimeSpan.FromMinutes(5), default);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReplayedSeqAndNonce_ReturnsFalse()
    {
        var store = BuildStore();
        var sessionId = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(5);

        await store.TryAcceptAsync(sessionId, 1L, "nonce-xyz", ttl, default);
        var replay = await store.TryAcceptAsync(sessionId, 1L, "nonce-xyz", ttl, default);

        replay.Should().BeFalse();
    }

    [Fact]
    public async Task SameSeq_DifferentNonce_ReturnsFalse()
    {
        var store = BuildStore();
        var sessionId = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(5);

        await store.TryAcceptAsync(sessionId, 5L, "nonce-aaa", ttl, default);
        // Sequence already seen — must reject even with a new nonce
        var result = await store.TryAcceptAsync(sessionId, 5L, "nonce-bbb", ttl, default);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReusedNonce_NewSeq_ReturnsFalse()
    {
        var store = BuildStore();
        var sessionId = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(5);

        await store.TryAcceptAsync(sessionId, 1L, "nonce-dup", ttl, default);
        // Nonce already seen — must reject even with a new sequence
        var result = await store.TryAcceptAsync(sessionId, 2L, "nonce-dup", ttl, default);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DifferentSessions_SameSeqAndNonce_IndependentAcceptance()
    {
        var store = BuildStore();
        var session1 = Guid.NewGuid();
        var session2 = Guid.NewGuid();
        var ttl = TimeSpan.FromMinutes(5);

        var r1 = await store.TryAcceptAsync(session1, 1L, "nonce-shared", ttl, default);
        var r2 = await store.TryAcceptAsync(session2, 1L, "nonce-shared", ttl, default);

        r1.Should().BeTrue();
        r2.Should().BeTrue();
    }
}
