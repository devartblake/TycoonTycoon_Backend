using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Notifications;
using Tycoon.Backend.Domain.Entities;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminNotifications;

public sealed class AdminNotificationDispatchJobTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminNotificationDispatchJobTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DispatchJob_Marks_Sent_When_Channel_Enabled()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
        var job = scope.ServiceProvider.GetRequiredService<AdminNotificationDispatchJob>();

        var channel = new AdminNotificationChannel("admin_basic", "Admin", "Desc", "high", true);
        db.AdminNotificationChannels.Add(channel);

        var schedule = new AdminNotificationSchedule($"sch_{Guid.NewGuid():N}", "Title", "Body", channel.Key, DateTimeOffset.UtcNow.AddMinutes(-1));
        db.AdminNotificationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        await job.Run();

        schedule.Status.Should().Be("sent");
    }

    [Fact]
    public async Task DispatchJob_Retries_Then_Fails_When_Channel_Missing()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
        var job = scope.ServiceProvider.GetRequiredService<AdminNotificationDispatchJob>();

        var schedule = new AdminNotificationSchedule($"sch_{Guid.NewGuid():N}", "Title", "Body", "missing", DateTimeOffset.UtcNow.AddMinutes(-1));
        db.AdminNotificationSchedules.Add(schedule);
        await db.SaveChangesAsync();

        await job.Run();
        schedule.Status.Should().Be("retry_pending");
        schedule.ScheduledAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(1));
        schedule.ScheduledAt.Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(31));

        schedule = db.AdminNotificationSchedules.First(x => x.ScheduleId == schedule.ScheduleId);
        // force due immediately for next attempts
        schedule.Reschedule(DateTimeOffset.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        await job.Run();
        schedule.Reschedule(DateTimeOffset.UtcNow.AddMinutes(-1));
        await db.SaveChangesAsync();

        await job.Run();

        schedule.Status.Should().Be("failed");
        schedule.RetryCount.Should().BeGreaterOrEqualTo(3);

        var latestFailureHistory = db.AdminNotificationHistory
            .Where(x => x.Id.StartsWith("push_job_") && x.Status == "failed")
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        latestFailureHistory.Should().NotBeNull();
        latestFailureHistory!.MetadataJson.Should().Contain("\"deadLetter\":true");
    }
}
