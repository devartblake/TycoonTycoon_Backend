namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminAppConfig
{
    public string Id { get; private set; } = "default";
    public string ApiBaseUrl { get; private set; } = "https://api.example.com";
    public bool EnableLogging { get; private set; }
    public string FeatureFlagsJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private AdminAppConfig() { }

    public AdminAppConfig(string apiBaseUrl, bool enableLogging, string featureFlagsJson)
    {
        ApiBaseUrl = apiBaseUrl;
        EnableLogging = enableLogging;
        FeatureFlagsJson = featureFlagsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(bool? enableLogging, string? featureFlagsJson)
    {
        if (enableLogging.HasValue) EnableLogging = enableLogging.Value;
        if (!string.IsNullOrWhiteSpace(featureFlagsJson)) FeatureFlagsJson = featureFlagsJson;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
