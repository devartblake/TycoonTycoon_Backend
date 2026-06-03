namespace Synaptix.Setup.Security;

/// <summary>
/// Phase 1 (Alpha/Beta) implementation — no actual encryption.
/// Stores secrets in the ProtectedSetupSecret envelope with Provider="Plaintext".
/// Replace with KmsSetupSecretProtector (Phase 2) for staging/production.
/// </summary>
public sealed class PlaintextLocalSetupSecretProtector : ISetupSecretProtector
{
    public string ProviderName => "Plaintext";

    public Task<ProtectedSetupSecret> ProtectAsync(string name, string plaintext, CancellationToken ct = default) =>
        Task.FromResult(new ProtectedSetupSecret(
            Name:            name,
            Ciphertext:      plaintext,
            Provider:        ProviderName,
            KeyName:         "local-plaintext",
            KeyVersion:      "v0",
            ProtectedAtUtc:  DateTimeOffset.UtcNow));

    public Task<string> UnprotectAsync(ProtectedSetupSecret secret, CancellationToken ct = default) =>
        Task.FromResult(secret.Ciphertext);
}
