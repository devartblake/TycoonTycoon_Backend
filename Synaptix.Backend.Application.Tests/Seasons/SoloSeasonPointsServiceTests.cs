using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Seasons;

public sealed class SoloSeasonPointsServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static SoloSeasonPointsService NewService(AppDb db, SeasonSoloPointsOptions? opts = null) =>
        new(db, new SeasonPointsService(db), Options.Create(opts ?? new SeasonSoloPointsOptions()));

    private static async Task<Season> ActiveSeasonAsync(AppDb db)
    {
        var season = new Season(1, "Season One", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30));
        season.Activate();
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        return season;
    }

    [Fact]
    public async Task Awards_PointsPerCorrect_InActiveSeason()
    {
        await using var db = NewDb();
        var season = await ActiveSeasonAsync(db);
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var awarded = await svc.AwardAsync(Guid.NewGuid(), playerId, 7, "quiz-session:test", CancellationToken.None);

        awarded.Should().Be(7);
        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.SeasonId == season.Id && x.PlayerId == playerId);
        profile.RankPoints.Should().Be(7);
        var txn = await db.SeasonPointTransactions.SingleAsync(x => x.PlayerId == playerId);
        txn.Kind.Should().Be(SoloSeasonPointsService.Kind);
        txn.Delta.Should().Be(7);
    }

    [Fact]
    public async Task Awards_Nothing_WhenNoActiveSeason()
    {
        await using var db = NewDb();
        var svc = NewService(db);

        var awarded = await svc.AwardAsync(Guid.NewGuid(), Guid.NewGuid(), 5, null, CancellationToken.None);

        awarded.Should().Be(0);
        (await db.SeasonPointTransactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Awards_Nothing_WhenDisabled_OrNoCorrectAnswers()
    {
        await using var db = NewDb();
        await ActiveSeasonAsync(db);

        var disabled = NewService(db, new SeasonSoloPointsOptions { Enabled = false });
        (await disabled.AwardAsync(Guid.NewGuid(), Guid.NewGuid(), 5, null, CancellationToken.None)).Should().Be(0);

        var svc = NewService(db);
        (await svc.AwardAsync(Guid.NewGuid(), Guid.NewGuid(), 0, null, CancellationToken.None)).Should().Be(0);

        (await db.SeasonPointTransactions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task DailyCap_Truncates_And_Then_Blocks_FurtherAwards()
    {
        await using var db = NewDb();
        await ActiveSeasonAsync(db);
        var svc = NewService(db, new SeasonSoloPointsOptions { DailyCap = 50 });
        var playerId = Guid.NewGuid();

        // 45 of the 50 daily points already earned.
        (await svc.AwardAsync(Guid.NewGuid(), playerId, 45, null, CancellationToken.None)).Should().Be(45);

        // 10 correct answers only yield the 5 remaining points.
        (await svc.AwardAsync(Guid.NewGuid(), playerId, 10, null, CancellationToken.None)).Should().Be(5);

        // Cap reached: nothing further today.
        (await svc.AwardAsync(Guid.NewGuid(), playerId, 10, null, CancellationToken.None)).Should().Be(0);

        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == playerId);
        profile.RankPoints.Should().Be(50);
    }

    [Fact]
    public async Task SameEventId_IsIdempotent()
    {
        await using var db = NewDb();
        await ActiveSeasonAsync(db);
        var svc = NewService(db);
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        (await svc.AwardAsync(eventId, playerId, 8, null, CancellationToken.None)).Should().Be(8);
        (await svc.AwardAsync(eventId, playerId, 8, null, CancellationToken.None)).Should().Be(0);

        var profile = await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == playerId);
        profile.RankPoints.Should().Be(8);
    }

    [Fact]
    public void DeriveEventId_IsDeterministic_AndPlayerBound()
    {
        var player = Guid.NewGuid();
        var other = Guid.NewGuid();

        SoloSeasonPointsService.DeriveEventId(player, "session-1")
            .Should().Be(SoloSeasonPointsService.DeriveEventId(player, "session-1"));

        SoloSeasonPointsService.DeriveEventId(player, "session-1")
            .Should().NotBe(SoloSeasonPointsService.DeriveEventId(other, "session-1"));

        SoloSeasonPointsService.DeriveEventId(player, "session-1")
            .Should().NotBe(SoloSeasonPointsService.DeriveEventId(player, "session-2"));
    }
}
