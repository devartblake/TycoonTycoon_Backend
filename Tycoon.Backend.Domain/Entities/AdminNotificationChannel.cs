namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminNotificationChannel
{
    public string Key { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Importance { get; private set; } = "high";
    public bool Enabled { get; private set; } = true;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private AdminNotificationChannel() { }

    public AdminNotificationChannel(string key, string name, string description, string importance, bool enabled)
    {
        Key = key;
        Update(name, description, importance, enabled);
    }

    public void Update(string name, string description, string importance, bool enabled)
    {
        Name = name;
        Description = description;
        Importance = importance;
        Enabled = enabled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
