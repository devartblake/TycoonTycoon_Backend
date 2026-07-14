namespace Synaptix.Security.Kms.Contracts.Suites;

public static class SecureSuites
{
    /// Initial implementation — X25519 + HKDF-SHA256 + AES-256-GCM.
    public const string ClassicalV1 = "X25519-HKDF-SHA256-AES256GCM";

    /// Compatibility suite for platforms where X25519 is not available through the OS crypto provider.
    public const string P256V1 = "P256-HKDF-SHA256-AES256GCM";

    /// Hybrid post-quantum suite (X25519 + ML-KEM-768). Live session start uses
    /// <c>HybridKeyExchange</c> (client initiator / server responder) when
    /// <c>Kms:Suites:EnableHybridPq</c> is true and the platform supports X25519 + ML-KEM.
    /// Pending independent human cryptographic review before production enablement.
    public const string HybridPqV1 = "X25519-MLKEM768-HKDF-SHA256-AES256GCM";
}
