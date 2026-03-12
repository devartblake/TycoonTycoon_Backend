using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies that AppDb.SaveChangesAsync correctly collects and clears domain events
/// from aggregate roots via DomainEventCollector.
/// </summary>
public sealed class AppDbDomainEventCollectionTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public async Task SaveChangesAsync_Clears_DomainEvents_AfterCommit()
    {
        await using var db = NewDb();

        // Match constructor raises MatchStartedEvent
        var match = new Match(Guid.NewGuid(), "solo");
        match.DomainEvents.Should().ContainSingle(e => e is MatchStartedEvent, "constructor should raise MatchStartedEvent");

        db.Matches.Add(match);
        await db.SaveChangesAsync();

        // Events must be cleared after SaveChangesAsync (DomainEventCollector.CollectAndClear)
        match.DomainEvents.Should().BeEmpty("domain events should be cleared after SaveChangesAsync");
    }

    [Fact]
    public async Task SaveChangesAsync_Returns_AffectedRowCount()
    {
        await using var db = NewDb();
        var m1 = new Mission("Daily", "daily_play_3", "Play 3", "Desc", 3, 50);
        var m2 = new Mission("Daily", "daily_win_1", "Win 1", "Desc", 1, 30);

        db.Missions.AddRange(m1, m2);
        var affected = await db.SaveChangesAsync();

        affected.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_DoesNotThrow_WhenNoEventsPresent()
    {
        await using var db = NewDb();
        var mission = new Mission("Daily", "daily_play_3", "Play 3", "Desc", 3, 50);

        // Mission is not an AggregateRoot, so no domain events
        db.Missions.Add(mission);

        // Should complete without errors even with no events to dispatch
        var act = async () => await db.SaveChangesAsync();
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_Clears_Events_FromMultipleAggregates()
    {
        await using var db = NewDb();

        var host1 = Guid.NewGuid();
        var host2 = Guid.NewGuid();

        var match1 = new Match(host1, "solo");
        var match2 = new Match(host2, "ranked");

        match1.DomainEvents.Should().HaveCount(1);
        match2.DomainEvents.Should().HaveCount(1);

        db.Matches.AddRange(match1, match2);
        await db.SaveChangesAsync();

        match1.DomainEvents.Should().BeEmpty();
        match2.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task Match_Finish_RaisesMatchCompletedEvent_BeforeSave()
    {
        await using var db = NewDb();
        var match = new Match(Guid.NewGuid(), "solo");

        db.Matches.Add(match);
        await db.SaveChangesAsync(); // clears MatchStartedEvent

        match.Finish(isWin: true, scoreDelta: 10, xpEarned: 50, correctAnswers: 7, totalQuestions: 10, durationSeconds: 90);

        match.DomainEvents.Should().ContainSingle(e => e is MatchCompletedEvent,
            "Finish() should raise MatchCompletedEvent");
    }
}
