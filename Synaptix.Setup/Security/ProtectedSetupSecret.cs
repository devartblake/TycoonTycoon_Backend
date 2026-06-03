namespace Synaptix.Setup.Security;

/// <summary>
/// A setup secret that has been wrapped/protected by a secret protector.
/// For PlaintextLocal mode the Ciphertext is the raw value; Provider is "Plaintext".
/// For KMS mode the Ciphertext is an opaque KMS-wrapped blob.
/// </summary>
public sealed record ProtectedSetupSecret(
    string Name,
    string Ciphertext,
    string Provider,
    string KeyName,
    string KeyVersion,
    DateTimeOffset ProtectedAtUtc);
