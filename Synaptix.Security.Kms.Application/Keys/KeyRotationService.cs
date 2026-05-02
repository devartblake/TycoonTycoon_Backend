using Synaptix.Security.Kms.Application.Abstractions;

namespace Synaptix.Security.Kms.Application.Keys;

public sealed class KeyRotationService(IKeyWrappingService wrapping)
{
    public async Task<RotateKeyResult> RotateAsync(string keyName, CancellationToken ct)
    {
        var dataKey = await wrapping.GenerateDataKeyAsync(keyName, ct);

        return new RotateKeyResult(
            keyName,
            dataKey.KeyVersion,
            DateTimeOffset.UtcNow);
    }
}

public sealed record RotateKeyResult(
    string KeyName,
    string NewKeyVersion,
    DateTimeOffset RotatedAtUtc);
