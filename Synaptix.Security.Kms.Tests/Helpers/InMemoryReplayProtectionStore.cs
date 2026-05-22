using Synaptix.Security.Kms.Application.Abstractions;

namespace Synaptix.Security.Kms.Tests.Helpers;

internal sealed class InMemoryReplayProtectionStore : IReplayProtectionStore
{
    private readonly HashSet<string> _seen = [];

    public Task<bool> TryAcceptAsync(Guid sessionId, long sequence, string nonce, TimeSpan ttl, CancellationToken ct)
    {
        var seqKey = $"{sessionId:N}:seq:{sequence}";
        var nonceKey = $"{sessionId:N}:nonce:{nonce}";

        if (_seen.Contains(seqKey) || _seen.Contains(nonceKey))
            return Task.FromResult(false);

        _seen.Add(seqKey);
        _seen.Add(nonceKey);
        return Task.FromResult(true);
    }
}
