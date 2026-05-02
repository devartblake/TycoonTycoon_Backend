namespace Synaptix.Security.Kms.Infrastructure.Vault;

public sealed class VaultOptions
{
    public const string SectionName = "Vault";

    public required string Address { get; init; }
    public required string Token { get; init; }
    public string TransitMount { get; init; } = "transit";
    public string SessionWrapKey { get; init; } = "synaptix-session-wrap";
    public string PayloadWrapKey { get; init; } = "synaptix-payload-wrap";

    /// When true, startup fails if Vault is unreachable.
    public bool Required { get; init; } = true;
}
