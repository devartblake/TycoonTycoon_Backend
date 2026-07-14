namespace Synaptix.Security.Kms.Application.Options;

/// <summary>
/// KMS application options bound from the <c>Kms</c> configuration section.
/// </summary>
public sealed class KmsOptions
{
    public const string SectionName = "Kms";

    public KmsSuitesOptions Suites { get; set; } = new();
}

/// <summary>
/// Suite-negotiation flags under <c>Kms:Suites</c>.
/// </summary>
public sealed class KmsSuitesOptions
{
    /// <summary>
    /// When true and ML-KEM is supported, HybridPqV1 is offered during suite negotiation.
    /// Requires independent cryptographic review before enabling in production.
    /// </summary>
    public bool EnableHybridPq { get; set; } = false;
}
