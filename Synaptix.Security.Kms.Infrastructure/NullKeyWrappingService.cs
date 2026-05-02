using System.Security.Cryptography;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Infrastructure;

/// No-op key wrapping used when Vault is not configured (local dev / testing only).
internal sealed class NullKeyWrappingService : IKeyWrappingService
{
    public Task<WrappedDataKey> GenerateDataKeyAsync(string keyName, CancellationToken ct)
    {
        var key = RandomNumberGenerator.GetBytes(32);
        var encoded = Convert.ToBase64String(key);
        return Task.FromResult(new WrappedDataKey(key, $"null:{encoded}", "v0"));
    }

    public Task<byte[]> UnwrapDataKeyAsync(string keyName, string encryptedDataKey, CancellationToken ct)
    {
        var b64 = encryptedDataKey.StartsWith("null:") ? encryptedDataKey[5..] : encryptedDataKey;
        return Task.FromResult(Convert.FromBase64String(b64));
    }
}
