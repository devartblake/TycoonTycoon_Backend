namespace Synaptix.Security.Kms.Client.Options;

public sealed class KmsClientOptions
{
    public const string SectionName = "KmsClient";

    /// Base URL of Synaptix.Security.Kms.Api, e.g. "https://kms-api" or "https+http://synaptix-kms-api" for Aspire.
    public required string BaseUrl { get; init; }

    /// Shared internal service token sent as X-Service-Token on every request.
    public string? ServiceToken { get; init; }

    /// Per-request timeout in seconds. Default 10.
    public int TimeoutSeconds { get; init; } = 10;

    /// Maximum retry attempts on transient failures. Default 3.
    public int MaxRetryAttempts { get; init; } = 3;

    /// When true, startup validation fails if KmsClient:BaseUrl is missing or unreachable.
    /// Mirrors VaultOptions.Required from the KMS handoff.
    public bool Required { get; init; } = true;
}
