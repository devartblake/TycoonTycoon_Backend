using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Seasons;

public sealed class SeasonTiebreakerServiceTests
{
    private sealed class FakeEconomy : IEconomyService
    {
        public List<CreateEconomyTxnRequest> Applied { get; } = [];

        public Task<EconomyTxnResultDto> ApplyAsync(CreateEconomyTxnRequest req, CancellationToken ct)
        {
            Applied.Add(req);
            return Task.FromResult(new EconomyTxnResultDto(
                req.EventId, req.PlayerId, EconomyTxnStatus.Applied,
                req.Lines.ToList(), 0, 0, 0, DateTimeOffset.UtcNow));
        }

        public Task<EconomyHistoryDto> GetHistoryAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<EconomyTxnResultDto> RollbackByEventIdAsync(Guid eventId, string reason, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<AdminPlayerEconomyDto?> GetPlayerSummaryAsync(Guid playerId, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<AdminEconomyStatsDto> GetEconomyStatsAsync(CancellationToken ct)
            => throw new NotSupportedException();
    }

    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static (SeasonTiebreakerService Service, FakeEconomy Economy) NewService(
        AppDb db, SeasonTiebreakerOptions? opts = null)
    {
        var economy = new FakeEconomy();
        var rewards = new SeasonRewardJob(db, economy);
        var svc = new SeasonTiebreakerService(db, rewards, Options.Create(opts ?? new SeasonTiebreakerOptions()));
        return (svc, economy);
    }

    private static async Task<Season> SeasonAsync(AppDb db)
    {
        var season = new Season(1, "S1", DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);
        season.Activate();
        db.Seasons.Add(season);
        await db.SaveChangesAsync();
        return season;
    }

    private static async Task<PlayerSeasonProfile> ProfileAsync(
        AppDb db, Guid seasonId, int points, int wins, int matches,
        int tier = 1, int tierRank = 0, int seasonRank = 0)
    {
        var p = new PlayerSeasonProfile(seasonId, Guid.NewGuid(), points);
        for (var i = 0; i < wins; i++) p.ApplyMatchOutcome(win: true, draw: false);
        for (var i = 0; i < matches - wins; i++) p.ApplyMatchOutcome(win: false, draw: false);
        p.SetRanks(tier, tierRank, seasonRank);
        db.PlayerSeasonProfiles.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    // ─── Detection ────────────────────────────────────────────────────────

    [Fact]
    public async Task Detects_Top1_Tie_And_Defers_TiedPlayers()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        var a = await ProfileAsync(db, season.Id, 100, wins: 5, matches: 10, seasonRank: 1);
        var b = await ProfileAsync(db, season.Id, 100, wins: 4, matches: 10, seasonRank: 2);
        var c = await ProfileAsync(db, season.Id, 60, wins: 3, matches: 10, seasonRank: 3);
        var (svc, _) = NewService(db);

        var deferred = await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);

        deferred.Should().BeEquivalentTo([a.PlayerId, b.PlayerId]);
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();
        tiebreaker.Scope.Should().Be(SeasonTiebreaker.Scopes.Top1);
        tiebreaker.Status.Should().Be(SeasonTiebreaker.Statuses.Scheduled);
        tiebreaker.PlayerIds.Should().BeEquivalentTo([a.PlayerId, b.PlayerId]);
        tiebreaker.RankPoints.Should().Be(100);

        // Both tied players were notified; the third player was not.
        (await db.PlayerNotifications.CountAsync(n => n.Type == "season_tiebreaker")).Should().Be(2);
        (await db.PlayerNotifications.AnyAsync(n => n.PlayerId == c.PlayerId)).Should().BeFalse();
    }

    [Fact]
    public async Task Detects_RewardBoundary_Tie_FromRewardRules()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        // Reward rule: tier 1, top-2 rewarded. Three players, 2nd and 3rd tied.
        db.SeasonRewardRules.Add(new SeasonRewardRule(tier: 1, maxTierRank: 2, xp: 100, coins: 50));
        var first = await ProfileAsync(db, season.Id, 200, wins: 9, matches: 10, tier: 1, tierRank: 1, seasonRank: 1);
        var second = await ProfileAsync(db, season.Id, 80, wins: 5, matches: 10, tier: 1, tierRank: 2, seasonRank: 2);
        var third = await ProfileAsync(db, season.Id, 80, wins: 4, matches: 10, tier: 1, tierRank: 3, seasonRank: 3);
        var (svc, _) = NewService(db);

        var deferred = await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);

        deferred.Should().BeEquivalentTo([second.PlayerId, third.PlayerId]);
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();
        tiebreaker.Scope.Should().Be(SeasonTiebreaker.Scopes.TierPromotion);
        tiebreaker.Tier.Should().Be(1);
        tiebreaker.BoundaryRank.Should().Be(2);
        deferred.Should().NotContain(first.PlayerId);
    }

    [Fact]
    public async Task Ignores_ZeroPoint_Ties_And_OversizedGroups()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        // Everyone at zero: no championship tiebreaker.
        await ProfileAsync(db, season.Id, 0, wins: 0, matches: 0);
        await ProfileAsync(db, season.Id, 0, wins: 0, matches: 0);
        var (svc, _) = NewService(db, new SeasonTiebreakerOptions { MaxGroupSize = 8 });

        var deferred = await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);

        deferred.Should().BeEmpty();
        (await db.SeasonTiebreakers.CountAsync()).Should().Be(0);
    }

    // ─── Resolution / expiry ──────────────────────────────────────────────

    [Fact]
    public async Task Resolve_SwapsBestPosition_WritesSnapshots_AndRunsRewards()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        db.SeasonRewardRules.Add(new SeasonRewardRule(tier: 1, maxTierRank: 10, xp: 100, coins: 50));
        var a = await ProfileAsync(db, season.Id, 100, wins: 5, matches: 10, tier: 1, tierRank: 1, seasonRank: 1);
        var b = await ProfileAsync(db, season.Id, 100, wins: 4, matches: 10, tier: 1, tierRank: 2, seasonRank: 2);
        var (svc, economy) = NewService(db);
        await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();

        // The underdog (b) wins the tiebreaker match.
        var matchId = Guid.NewGuid();
        var resolved = await svc.ResolveAsync(tiebreaker.Id, b.PlayerId, matchId, "match:test", CancellationToken.None);

        resolved.Should().NotBeNull();
        resolved!.Status.Should().Be(SeasonTiebreaker.Statuses.Completed);
        resolved.WinnerPlayerId.Should().Be(b.PlayerId);
        resolved.MatchId.Should().Be(matchId);

        // b took a's rank-1 position; a dropped to b's old position.
        var pa = await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == a.PlayerId);
        var pb = await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == b.PlayerId);
        pb.SeasonRank.Should().Be(1);
        pa.SeasonRank.Should().Be(2);

        // Deferred snapshot rows now exist for both.
        (await db.SeasonRankSnapshots.CountAsync(x => x.SeasonId == season.Id)).Should().Be(2);
        var winnerRow = await db.SeasonRankSnapshots.SingleAsync(x => x.PlayerId == b.PlayerId);
        winnerRow.SeasonRank.Should().Be(1);

        // Final rewards ran for the snapshotted players.
        economy.Applied.Should().HaveCount(2);
    }

    [Fact]
    public async Task Resolve_Rejects_NonParticipant_And_NonPending()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        var a = await ProfileAsync(db, season.Id, 100, wins: 5, matches: 10, seasonRank: 1);
        var b = await ProfileAsync(db, season.Id, 100, wins: 4, matches: 10, seasonRank: 2);
        var (svc, _) = NewService(db);
        await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();

        (await svc.ResolveAsync(tiebreaker.Id, Guid.NewGuid(), null, null, CancellationToken.None)).Should().BeNull();

        (await svc.ResolveAsync(tiebreaker.Id, a.PlayerId, null, null, CancellationToken.None)).Should().NotBeNull();
        // Already completed: a second resolution is refused.
        (await svc.ResolveAsync(tiebreaker.Id, b.PlayerId, null, null, CancellationToken.None)).Should().BeNull();
    }

    [Fact]
    public async Task Expiry_ResolvesDeterministically_MostWins_First()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        var a = await ProfileAsync(db, season.Id, 100, wins: 3, matches: 10, seasonRank: 1);
        var b = await ProfileAsync(db, season.Id, 100, wins: 7, matches: 10, seasonRank: 2);
        var (svc, _) = NewService(db, new SeasonTiebreakerOptions
        {
            ScheduleDelay = TimeSpan.FromHours(-48), // already past
            ExpiryGrace = TimeSpan.FromHours(24),
        });
        await svc.DetectAndScheduleAsync(season.Id, CancellationToken.None);

        var expired = await svc.ExpireOverdueAsync(CancellationToken.None);

        expired.Should().Be(1);
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();
        tiebreaker.Status.Should().Be(SeasonTiebreaker.Statuses.Expired);
        // b has more wins → deterministic fallback winner, takes rank 1.
        tiebreaker.WinnerPlayerId.Should().Be(b.PlayerId);
        (await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == b.PlayerId)).SeasonRank.Should().Be(1);
        (await db.PlayerSeasonProfiles.SingleAsync(x => x.PlayerId == a.PlayerId)).SeasonRank.Should().Be(2);
    }

    // ─── Close orchestration ──────────────────────────────────────────────

    [Fact]
    public async Task Close_DefersSnapshotRows_ForTiedPlayers_Only()
    {
        await using var db = NewDb();
        var season = await SeasonAsync(db);
        var a = await ProfileAsync(db, season.Id, 100, wins: 5, matches: 10, seasonRank: 1);
        var b = await ProfileAsync(db, season.Id, 100, wins: 4, matches: 10, seasonRank: 2);
        var c = await ProfileAsync(db, season.Id, 60, wins: 3, matches: 10, seasonRank: 3);
        var (svc, economy) = NewService(db);
        var orchestrator = new SeasonCloseOrchestrator(db, new SeasonRewardJob(db, economy), svc);

        var result = await orchestrator.CloseAsync(season.Id, CancellationToken.None);

        result.Should().Be("Closed");
        // Only the untied player was snapshotted at close.
        var rows = await db.SeasonRankSnapshots.Where(x => x.SeasonId == season.Id).ToListAsync();
        rows.Should().ContainSingle(x => x.PlayerId == c.PlayerId);
        rows.Should().NotContain(x => x.PlayerId == a.PlayerId);
        rows.Should().NotContain(x => x.PlayerId == b.PlayerId);

        // Resolving the tiebreaker completes the standings.
        var tiebreaker = await db.SeasonTiebreakers.SingleAsync();
        await svc.ResolveAsync(tiebreaker.Id, a.PlayerId, null, null, CancellationToken.None);
        (await db.SeasonRankSnapshots.CountAsync(x => x.SeasonId == season.Id)).Should().Be(3);
    }
}
