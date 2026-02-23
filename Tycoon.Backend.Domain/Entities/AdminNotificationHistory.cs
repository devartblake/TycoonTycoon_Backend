namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminNotificationHistory
{
    public string Id { get; private set; } = string.Empty;
    public string ChannelKey { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? MetadataJson { get; private set; }

    private AdminNotificationHistory() { }

    public AdminNotificationHistory(string id, string channelKey, string title, string status, DateTimeOffset createdAt, string? metadataJson)
    {
        Id = id;
        ChannelKey = channelKey;
        Title = title;
        Status = status;
        CreatedAt = createdAt;
        MetadataJson = metadataJson;
    }
}
