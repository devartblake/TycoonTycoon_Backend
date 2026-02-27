using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Notifications;

/// <summary>
/// Best-effort dispatcher for scheduled admin notifications.
/// Runs as a recurring background job and updates schedule status with retry semantics.
/// </summary>
public sealed class AdminNotificationDispatchJob(IAppDb db, ILogger<AdminNotificationDispatchJob> logger)
{
    public async Task Run(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var dueSchedules = await db.AdminNotificationSchedules
            .Where(x => (x.Status == "scheduled" || x.Status == "retry_pending") && x.ScheduledAt <= now)
            .OrderBy(x => x.ScheduledAt)
            .Take(200)
            .ToListAsync(ct);

        if (dueSchedules.Count == 0)
        {
            logger.LogDebug("AdminNotificationDispatchJob: no due schedules.");
            return;
        }

        foreach (var schedule in dueSchedules)
        {
            try
            {
                var channel = await db.AdminNotificationChannels
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Key == schedule.ChannelKey, ct);

                if (channel is null || !channel.Enabled)
                {
                    schedule.MarkRetryOrFail(
                        reason: channel is null ? "Channel not found." : "Channel disabled.",
                        nextAttemptAt: now.AddMinutes(5));

                    db.AdminNotificationHistory.Add(new AdminNotificationHistory(
                        id: $"push_job_{Guid.NewGuid():N}",
                        channelKey: schedule.ChannelKey,
                        title: schedule.Title,
                        status: schedule.Status,
                        createdAt: DateTimeOffset.UtcNow,
                        metadataJson: JsonSerializer.Serialize(new
                        {
                            scheduleId = schedule.ScheduleId,
                            reason = schedule.LastError,
                            retryCount = schedule.RetryCount,
                            maxRetries = schedule.MaxRetries
                        })));

                    continue;
                }

                schedule.MarkSent();

                db.AdminNotificationHistory.Add(new AdminNotificationHistory(
                    id: $"push_job_{Guid.NewGuid():N}",
                    channelKey: schedule.ChannelKey,
                    title: schedule.Title,
                    status: "sent",
                    createdAt: DateTimeOffset.UtcNow,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        scheduleId = schedule.ScheduleId,
                        deliveredBy = "AdminNotificationDispatchJob"
                    })));
            }
            catch (Exception ex)
            {
                schedule.MarkRetryOrFail(ex.Message, now.AddMinutes(5));

                db.AdminNotificationHistory.Add(new AdminNotificationHistory(
                    id: $"push_job_{Guid.NewGuid():N}",
                    channelKey: schedule.ChannelKey,
                    title: schedule.Title,
                    status: schedule.Status,
                    createdAt: DateTimeOffset.UtcNow,
                    metadataJson: JsonSerializer.Serialize(new
                    {
                        scheduleId = schedule.ScheduleId,
                        reason = ex.Message,
                        retryCount = schedule.RetryCount,
                        maxRetries = schedule.MaxRetries
                    })));
            }
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("AdminNotificationDispatchJob processed {Count} schedules.", dueSchedules.Count);
    }
}
