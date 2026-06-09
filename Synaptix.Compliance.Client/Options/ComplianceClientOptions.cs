namespace Synaptix.Compliance.Client.Options;

public sealed class ComplianceClientOptions
{
    public const string SectionName = "ComplianceClient";

    public string BaseUrl { get; set; } = "http://compliance-api:5070";
    public string ServiceToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 10;

    /// When false, compliance calls that fail do not throw; restrictions default to empty.
    public bool Required { get; set; } = false;
}
