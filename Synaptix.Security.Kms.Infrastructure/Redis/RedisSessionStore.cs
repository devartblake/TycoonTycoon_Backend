using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Infrastructure.Redis;

public sealed class RedisSessionStore(IDistributedCache cache) : ISessionStore
{
    private static readonly TimeSpan MaxTtl = TimeSpan.FromHours(2);

    public async Task SaveAsync(SecureSession session, CancellationToken ct)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(session, SessionSerializerContext.Default.SecureSession);
        var ttl = session.ExpiresAtUtc - DateTimeOffset.UtcNow;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl > TimeSpan.Zero ? ttl : MaxTtl
        };

        await cache.SetAsync(SessionKey(session.SessionId), json, options, ct);
    }

    public async Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct)
    {
        var bytes = await cache.GetAsync(SessionKey(sessionId), ct);
        if (bytes is null) return null;

        var session = JsonSerializer.Deserialize(bytes, SessionSerializerContext.Default.SecureSession);
        if (session is null) return null;

        return session.ExpiresAtUtc < DateTimeOffset.UtcNow ? null : session;
    }

    public Task DeleteAsync(Guid sessionId, CancellationToken ct)
        => cache.RemoveAsync(SessionKey(sessionId), ct);

    private static string SessionKey(Guid id) => $"synsec:session:{id:N}";
}
