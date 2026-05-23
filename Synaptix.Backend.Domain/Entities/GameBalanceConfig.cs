namespace Synaptix.Backend.Domain.Entities;

public sealed class GameBalanceConfig
{
    public string Id { get; private set; } = "default";
    public string ConfigJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private GameBalanceConfig() { } // EF

    public GameBalanceConfig(string configJson)
    {
        ConfigJson = configJson;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Update(string configJson)
    {
        ConfigJson = configJson;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
