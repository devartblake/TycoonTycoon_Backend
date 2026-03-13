using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Missions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Application.Tests.Missions;

public sealed class MissionProgressServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static Mission DailyPlay3(bool active = true) =>
        new("Daily", "daily_play_3", "Play 3 Matches", "Play 3 matches today.", 3, 50, active: active);

    private static Mission DailyWin1() =>
        new("Daily", "daily_win_1", "Win a Match", "Win 1 match today.", 1, 30);

    private static Mission WeeklyWin10() =>
        new("Weekly", "weekly_win_10", "Win 10 Matches", "Win 10 matches this week.", 10, 150);

    private static Mission WeeklyPlay25() =>
        new("Weekly", "weekly_play_25", "Play 25 Matches", "Play 25 matches this week.", 25, 200);

    private static Mission WeeklyPerfect5() =>
        new("Weekly", "weekly_perfect_5", "Perfect Rounds", "Complete 5 perfect rounds.", 5, 100);

    // ─── ApplyMatchCompletedAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ApplyMatchCompleted_Increments_DailyPlay3_ForEveryMatch()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        var mission = DailyPlay3();
        db.Missions.Add(mission);
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 5, totalQuestions: 10, durationSeconds: 60, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId);
        claim.MissionId.Should().Be(mission.Id);
        claim.Progress.Should().Be(1);
    }

    [Fact]
    public async Task ApplyMatchCompleted_Increments_WeeklyPlay25_ForEveryMatch()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(WeeklyPlay25());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 0, totalQuestions: 10, durationSeconds: 30, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId);
        claim.Progress.Should().Be(1);
    }

    [Fact]
    public async Task ApplyMatchCompleted_Increments_DailyWin1_OnlyOnWin()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(DailyWin1());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: true, correctAnswers: 8, totalQuestions: 10, durationSeconds: 60, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId);
        claim.Progress.Should().Be(1);
    }

    [Fact]
    public async Task ApplyMatchCompleted_DoesNotIncrement_DailyWin1_OnLoss()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(DailyWin1());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 3, totalQuestions: 10, durationSeconds: 60, ct: default);

        var claimCount = await db.MissionClaims.CountAsync(x => x.PlayerId == playerId);
        claimCount.Should().Be(0, "a loss should not create or increment the win mission claim");
    }

    [Fact]
    public async Task ApplyMatchCompleted_Increments_WeeklyWin10_OnlyOnWin()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(WeeklyWin10());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: true, correctAnswers: 9, totalQuestions: 10, durationSeconds: 45, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId);
        claim.Progress.Should().Be(1);
    }

    [Fact]
    public async Task ApplyMatchCompleted_DoesNotIncrement_WeeklyWin10_OnLoss()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(WeeklyWin10());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 3, totalQuestions: 10, durationSeconds: 60, ct: default);

        (await db.MissionClaims.CountAsync(x => x.PlayerId == playerId)).Should().Be(0);
    }

    [Fact]
    public async Task ApplyMatchCompleted_Marks_Completed_WhenGoalReached()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        var mission = DailyPlay3(); // goal = 3
        db.Missions.Add(mission);
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);

        for (var i = 0; i < 3; i++)
            await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 5, totalQuestions: 10, durationSeconds: 60, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId && x.MissionId == mission.Id);
        claim.Completed.Should().BeTrue();
        claim.Progress.Should().Be(3);
    }

    [Fact]
    public async Task ApplyMatchCompleted_DoesNotProcess_InactiveMission()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        var inactive = DailyPlay3(active: false);
        db.Missions.Add(inactive);
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 5, totalQuestions: 10, durationSeconds: 60, ct: default);

        (await db.MissionClaims.CountAsync(x => x.PlayerId == playerId)).Should().Be(0);
    }

    [Fact]
    public async Task ApplyMatchCompleted_UpdatesMultipleMissions_ForSameMatch()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.AddRange(DailyPlay3(), WeeklyPlay25());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyMatchCompletedAsync(playerId, isWin: false, correctAnswers: 5, totalQuestions: 10, durationSeconds: 60, ct: default);

        var claims = await db.MissionClaims.Where(x => x.PlayerId == playerId).ToListAsync();
        claims.Should().HaveCount(2, "both play-count missions should be incremented");
        claims.Should().AllSatisfy(c => c.Progress.Should().Be(1));
    }

    // ─── ApplyRoundCompletedAsync ─────────────────────────────────────────────

    [Fact]
    public async Task ApplyRoundCompleted_Increments_WeeklyPerfect5_OnPerfectRound()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(WeeklyPerfect5());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyRoundCompletedAsync(playerId, perfectRound: true, avgAnswerTimeMs: 2000, ct: default);

        var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId);
        claim.Progress.Should().Be(1);
    }

    [Fact]
    public async Task ApplyRoundCompleted_DoesNotIncrement_WeeklyPerfect5_OnImperfectRound()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        db.Missions.Add(WeeklyPerfect5());
        await db.SaveChangesAsync();

        var svc = new MissionProgressService(db);
        await svc.ApplyRoundCompletedAsync(playerId, perfectRound: false, avgAnswerTimeMs: 5000, ct: default);

        (await db.MissionClaims.CountAsync(x => x.PlayerId == playerId)).Should().Be(0);
    }
}