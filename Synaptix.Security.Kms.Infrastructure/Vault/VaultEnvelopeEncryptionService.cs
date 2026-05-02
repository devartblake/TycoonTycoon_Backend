using Microsoft.Extensions.Options;
using Synaptix.Security.Kms.Application.Abstractions;
using Synaptix.Security.Kms.Contracts.Models;

namespace Synaptix.Security.Kms.Infrastructure.Vault;

public sealed class VaultEnvelopeEncryptionService(
    VaultTransitClient vault,
    IOptions<VaultOptions> opts) : IKeyWrappingService
{
    private readonly VaultOptions _opts = opts.Value;

    public async Task<WrappedDataKey> GenerateDataKeyAsync(string keyName, CancellationToken ct)
    {
        var resolvedKey = ResolveKeyName(keyName);
        var (plaintextKey, ciphertextKey) = await vault.GenerateDataKeyAsync(resolvedKey, ct);
        var keyVersion = await vault.GetLatestKeyVersionAsync(resolvedKey, ct);

        return new WrappedDataKey(plaintextKey, ciphertextKey, keyVersion);
    }

    public Task<byte[]> UnwrapDataKeyAsync(string keyName, string encryptedDataKey, CancellationToken ct)
    {
        var resolvedKey = ResolveKeyName(keyName);
        return vault.DecryptDataKeyAsync(resolvedKey, encryptedDataKey, ct);
    }

    private string ResolveKeyName(string keyName) => keyName switch
    {
        "session" => _opts.SessionWrapKey,
        "payload" => _opts.PayloadWrapKey,
        _ => keyName
    };
}
