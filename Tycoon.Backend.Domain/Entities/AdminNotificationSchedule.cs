namespace Tycoon.Backend.Domain.Entities;

public sealed class AdminNotificationSchedule
{
    public string ScheduleId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string ChannelKey { get; private set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; private set; }
    public string Status { get; private set; } = "scheduled";
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;
    public string? LastError { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
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
        RetryCount = 0;
        MaxRetries = 3;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel() => Status = "cancelled";

    public void Reschedule(DateTimeOffset scheduledAt)
    {
        ScheduledAt = scheduledAt;
    }

    public void MarkSent()
    {
        Status = "sent";
        LastError = null;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
    }


    public bool CanReplay() => Status == "failed";

    public void Replay(DateTimeOffset scheduledAt)
    {
        if (!CanReplay())
        {
            return;
        }

        Status = "scheduled";
        RetryCount = 0;
        LastError = null;
        ProcessedAtUtc = null;
        ScheduledAt = scheduledAt;
    }

    public void MarkRetryOrFail(string reason, DateTimeOffset nextAttemptAt)
    {
        RetryCount++;
        LastError = reason;

        if (RetryCount >= MaxRetries)
        {
            Status = "failed";
            ProcessedAtUtc = DateTimeOffset.UtcNow;
            return;
        }

        Status = "retry_pending";
        ScheduledAt = nextAttemptAt;
    }
}
