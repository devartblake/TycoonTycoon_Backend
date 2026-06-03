namespace Synaptix.Setup.Security;

/// <summary>
/// Bridges setup-generated secrets to a protection backend (plaintext, KMS, etc.).
/// Phase 1: only PlaintextLocalSetupSecretProtector is implemented.
/// Phase 2: add KmsSetupSecretProtector backed by Synaptix.Security.Kms.Client.
/// </summary>
public interface ISetupSecretProtector
{
    string ProviderName { get; }

    Task<ProtectedSetupSecret> ProtectAsync(string name, string plaintext, CancellationToken ct = default);
    Task<string> UnprotectAsync(ProtectedSetupSecret secret, CancellationToken ct = default);
}
