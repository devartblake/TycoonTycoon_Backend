namespace Tycoon.Backend.Infrastructure.SidecarClient;

/// <summary>
/// Configuration options for the Personalization Sidecar HTTP client.
/// Bound to the <c>SidecarPersonalization</c> configuration section.
/// </summary>
public sealed class SidecarPersonalizationOptions
{
    public const string SectionName = "SidecarPersonalization";

    /// <summary>Base URL of the FastAPI Personalization Sidecar service.</summary>
    public string BaseUrl { get; set; } = "http://localhost:8001";

    /// <summary>Request timeout in seconds. Defaults to 5.</summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// When <c>false</c>, a no-op client is registered and all sidecar calls are skipped.
    /// The backend will always fall back to local scoring rules.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
