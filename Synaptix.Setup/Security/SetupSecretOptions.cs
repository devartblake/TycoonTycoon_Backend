namespace Synaptix.Setup.Security;

/// <summary>
/// Controls how setup-generated secrets are protected.
/// Bind from configuration key "SetupSecrets:ProtectionMode".
/// </summary>
public sealed class SetupSecretOptions
{
    public const string SectionKey = "SetupSecrets";

    /// <summary>
    /// Default: PlaintextLocal for Alpha. Upgrade to KmsPreferred/KmsRequired for staging/prod.
    /// </summary>
    public SetupSecretProtectionMode ProtectionMode { get; set; } = SetupSecretProtectionMode.PlaintextLocal;

    public string? KmsKeyName { get; set; } = "synaptix-setup-bootstrap";
    public string? KmsBaseUrl { get; set; }
    public string? KmsServiceToken { get; set; }
}

public enum SetupSecretProtectionMode
{
    /// <summary>Secrets written as plaintext to docker/.env. Safe for local dev only.</summary>
    PlaintextLocal,
    /// <summary>Attempt KMS wrapping; fall back to plaintext if KMS is unavailable.</summary>
    KmsPreferred,
    /// <summary>KMS wrapping required; fail if KMS is unreachable.</summary>
    KmsRequired,
    /// <summary>No secrets generated locally; all must come from an external secret manager.</summary>
    ExternalOnly,
}
