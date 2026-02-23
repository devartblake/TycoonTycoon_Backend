namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminNotificationSchedule
{
    public string ScheduleId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string ChannelKey { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public string Status { get; private set; } = "scheduled";
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private AdminNotificationSchedule() { }

    public AdminNotificationSchedule(string scheduleId, string title, string body, string channelKey, DateTimeOffset scheduledAt)
    {
        ScheduleId = scheduleId;
        Title = title;
        Body = body;
        ChannelKey = channelKey;
        ScheduledAt = scheduledAt;
        Status = "scheduled";
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel() => Status = "cancelled";
}
