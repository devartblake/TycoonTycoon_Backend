using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Tests.Seasons;

public sealed class SeasonPointsServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static ApplySeasonPointsRequest Req(Guid eventId, Guid seasonId, Guid playerId, int delta) =>
        new(eventId, seasonId, playerId, "match-result", delta, null);

    // ─── ApplyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_Returns_Applied_AndUpdatesRankPoints()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        var result = await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, 50), CancellationToken.None);

        result.Status.Should().Be("Applied");
        result.NewRankPoints.Should().Be(50);
        result.SeasonId.Should().Be(seasonId);
        result.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public async Task Apply_Creates_PlayerSeasonProfile_WhenNoneExists()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, 25), CancellationToken.None);

        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId);
        profile.Should().NotBeNull();
        profile.RankPoints.Should().Be(25);
    }

    [Fact]
    public async Task Apply_Accumulates_RankPoints_AcrossTransactions()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, 30), CancellationToken.None);
        await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, 20), CancellationToken.None);

        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId);
        profile.RankPoints.Should().Be(50);
    }

    [Fact]
    public async Task Apply_Returns_Duplicate_OnSecondCall_WithSameEventId()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await svc.ApplyAsync(Req(eventId, seasonId, playerId, 40), CancellationToken.None);
        var second = await svc.ApplyAsync(Req(eventId, seasonId, playerId, 40), CancellationToken.None);

        second.Status.Should().Be("Duplicate");

        // Points should not be double-counted
        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId);
        profile.RankPoints.Should().Be(40);
    }

    [Fact]
    public async Task Apply_ClampsRankPoints_AtZero_WhenNegativeDelta()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Start with 10 points
        await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, 10), CancellationToken.None);

        // Deduct 50 (more than available)
        var result = await svc.ApplyAsync(Req(Guid.NewGuid(), seasonId, playerId, -50), CancellationToken.None);

        result.NewRankPoints.Should().Be(0, "rank points should be clamped at 0");
    }

    [Fact]
    public async Task Apply_Persists_SeasonPointTransaction()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await svc.ApplyAsync(Req(eventId, seasonId, playerId, 20), CancellationToken.None);

        var txn = await db.SeasonPointTransactions.SingleAsync(x => x.EventId == eventId);
        txn.PlayerId.Should().Be(playerId);
        txn.SeasonId.Should().Be(seasonId);
        txn.Delta.Should().Be(20);
        txn.Kind.Should().Be("match-result");
    }

    // ─── GetActiveSeasonAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetActiveSeason_Returns_Null_WhenNoActiveSeason()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);

        var result = await svc.GetActiveSeasonAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSeason_Returns_ActiveSeason()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);

        var season = new Season(1, "Season One", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));
        season.Activate();
        db.Seasons.Add(season);
        await db.SaveChangesAsync();

        var result = await svc.GetActiveSeasonAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(season.Id);
        result.Status.Should().Be(SeasonStatus.Active);
    }

    [Fact]
    public async Task GetActiveSeason_Returns_Null_WhenSeasonIsScheduled()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);

        var season = new Season(1, "Season One", DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(31));
        // Status = Scheduled (default), not activated
        db.Seasons.Add(season);
        await db.SaveChangesAsync();

        var result = await svc.GetActiveSeasonAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveSeason_Returns_Null_WhenSeasonIsClosed()
    {
        await using var db = NewDb();
        var svc = new SeasonPointsService(db);

        var season = new Season(1, "Season One", DateTimeOffset.UtcNow.AddDays(-10), DateTimeOffset.UtcNow.AddDays(-1));
        season.Activate();
        season.Close(DateTimeOffset.UtcNow);
        db.Seasons.Add(season);
        await db.SaveChangesAsync();

        var result = await svc.GetActiveSeasonAsync(CancellationToken.None);

        result.Should().BeNull();
    }
}