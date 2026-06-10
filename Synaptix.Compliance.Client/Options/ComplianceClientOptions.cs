namespace Synaptix.Compliance.Client.Options;

public sealed class ComplianceClientOptions
{
    public const string SectionName = "ComplianceClient";

    public string BaseUrl { get; set; } = "http://compliance-api:5070";
    public string ServiceToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;

    /// Whether compliance is required for this service (note: not currently enforced by the client; non-success responses throw).
    public bool Required { get; set; } = false;
}
