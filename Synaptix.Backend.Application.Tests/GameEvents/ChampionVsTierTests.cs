using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Application.EventStats;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.GameEvents;

public sealed class ChampionVsTierTests
{
    private sealed class FakeEconomy : IEconomyService
    {
        public List<CreateEconomyTxnRequest> Applied { get; } = [];
        public Task<EconomyTxnResultDto> ApplyAsync(CreateEconomyTxnRequest req, CancellationToken ct)
        {
            Applied.Add(req);
            return Task.FromResult(new EconomyTxnResultDto(
                req.EventId, req.PlayerId, EconomyTxnStatus.Applied, req.Lines.ToList(), 0, 0, 0, DateTimeOffset.UtcNow));
        }
        public Task<EconomyHistoryDto> GetHistoryAsync(Guid p, int a, int b, CancellationToken ct) => throw new NotSupportedException();
        public Task<EconomyTxnResultDto> RollbackByEventIdAsync(Guid e, string r, CancellationToken ct) => throw new NotSupportedException();
        public Task<AdminPlayerEconomyDto?> GetPlayerSummaryAsync(Guid p, CancellationToken ct) => throw new NotSupportedException();
        public Task<AdminEconomyStatsDto> GetEconomyStatsAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static async Task<Guid> ActiveSeasonAsync(AppDb db)
    {
        var s = new Season(1, "S1", DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(29));
        s.Activate();
        db.Seasons.Add(s);
        await db.SaveChangesAsync();
        return s.Id;
    }

    private static async Task<Guid> TierChampionAsync(AppDb db, Guid seasonId, int tier, int points)
    {
        var p = new PlayerSeasonProfile(seasonId, Guid.NewGuid(), points);
        p.SetRanks(tier, 1, 1);
        db.PlayerSeasonProfiles.Add(p);
        await db.SaveChangesAsync();
        return p.PlayerId;
    }

    private static GameEvent NewChampionVsTier(int tier = 1) =>
        new(GameEvent.ChampionVsTierKind, tier, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, entryFeeCoins: 100, reviveCostGems: 0, maxParticipants: 100);

    private static CloseGameEventAndDistributePrizesHandler CloseHandler(AppDb db, FakeEconomy econ) =>
        new(db, econ, new NullGameEventNotifier(), new SeasonService(db), new PlayerEventStatsService(db));

    // ─── Domain ───────────────────────────────────────────────────────────

    [Fact]
    public void EffectiveJackpot_AppliesClampedMultiplier()
    {
        var ev = NewChampionVsTier();
        ev.AddToJackpot(1000);

        ev.EffectiveJackpot.Should().Be(1000); // default 1.0x

        ev.SetJackpotMultiplier(2.5m);
        ev.EffectiveJackpot.Should().Be(2500);

        ev.SetJackpotMultiplier(99m); // clamped to 10x
        ev.EffectiveJackpot.Should().Be(10000);

        ev.SetJackpotMultiplier(0.1m); // clamped up to 1x
        ev.EffectiveJackpot.Should().Be(1000);
    }

    [Fact]
    public void SeedChampion_IsIdempotent_AndFeedsJackpotIsKindScoped()
    {
        var ev = NewChampionVsTier();
        ev.FeedsJackpot.Should().BeTrue();

        var first = Guid.NewGuid();
        ev.SeedChampion(first);
        ev.SeedChampion(Guid.NewGuid()); // ignored
        ev.ChampionPlayerId.Should().Be(first);
    }

    // ─── Seeder ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Seeder_EnrollsTierNumberOne_AsChampion()
    {
        await using var db = NewDb();
        var seasonId = await ActiveSeasonAsync(db);
        var champ = await TierChampionAsync(db, seasonId, tier: 1, points: 500);
        await TierChampionAsync(db, seasonId, tier: 1, points: 100); // lower — not champion
        var ev = NewChampionVsTier(tier: 1);
        db.GameEvents.Add(ev);
        await db.SaveChangesAsync();

        await new TierChampionSeeder(db, new SeasonService(db)).SeedAsync(ev, CancellationToken.None);
        await db.SaveChangesAsync();

        ev.ChampionPlayerId.Should().Be(champ);
        (await db.GameEventParticipants.CountAsync(x => x.GameEventId == ev.Id && x.PlayerId == champ)).Should().Be(1);

        // Idempotent second run.
        await new TierChampionSeeder(db, new SeasonService(db)).SeedAsync(ev, CancellationToken.None);
        (await db.GameEventParticipants.CountAsync(x => x.GameEventId == ev.Id)).Should().Be(1);
    }

    // ─── Asymmetric close ─────────────────────────────────────────────────

    [Fact]
    public async Task Close_ChampionDefends_ChampionTakesRank1AndJackpot()
    {
        await using var db = NewDb();
        await ActiveSeasonAsync(db);
        var econ = new FakeEconomy();

        var ev = NewChampionVsTier();
        var champion = Guid.NewGuid();
        ev.SeedChampion(champion);
        ev.AddToJackpot(500);
        ev.SetJackpotMultiplier(2.0m); // sponsor doubles it
        ev.Open(DateTimeOffset.UtcNow);
        ev.Start(DateTimeOffset.UtcNow);
        db.GameEvents.Add(ev);

        // Champion survives; two challengers eliminated.
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, champion, Guid.NewGuid()));
        var c1 = new GameEventParticipant(ev.Id, Guid.NewGuid(), Guid.NewGuid()) { EliminatedAt = DateTimeOffset.UtcNow.AddMinutes(-5) };
        var c2 = new GameEventParticipant(ev.Id, Guid.NewGuid(), Guid.NewGuid()) { EliminatedAt = DateTimeOffset.UtcNow.AddMinutes(-1) };
        db.GameEventParticipants.AddRange(c1, c2);
        await db.SaveChangesAsync();

        var res = await CloseHandler(db, econ).Handle(new CloseGameEventAndDistributePrizes(ev.Id), CancellationToken.None);

        // 500 jackpot × 2.0 sponsor multiplier.
        res.JackpotDistributed.Should().Be(1000);
        var championParticipant = await db.GameEventParticipants.SingleAsync(x => x.PlayerId == champion);
        championParticipant.FinalRank.Should().Be(1);

        var championPrize = econ.Applied.Single(x => x.PlayerId == champion);
        var coins = championPrize.Lines.Single(l => l.Currency == CurrencyType.Coins).Delta;
        coins.Should().Be(250 + 1000); // winner base coins + multiplied jackpot
    }

    [Fact]
    public async Task Close_ChampionDethroned_LastChallengerWins()
    {
        await using var db = NewDb();
        await ActiveSeasonAsync(db);
        var econ = new FakeEconomy();

        var ev = NewChampionVsTier();
        var champion = Guid.NewGuid();
        ev.SeedChampion(champion);
        ev.AddToJackpot(300);
        ev.Open(DateTimeOffset.UtcNow);
        ev.Start(DateTimeOffset.UtcNow);
        db.GameEvents.Add(ev);

        // Champion eliminated; one challenger survives.
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, champion, Guid.NewGuid())
        { EliminatedAt = DateTimeOffset.UtcNow.AddMinutes(-2) });
        var survivor = new GameEventParticipant(ev.Id, Guid.NewGuid(), Guid.NewGuid());
        db.GameEventParticipants.Add(survivor);
        await db.SaveChangesAsync();

        var res = await CloseHandler(db, econ).Handle(new CloseGameEventAndDistributePrizes(ev.Id), CancellationToken.None);

        res.JackpotDistributed.Should().Be(300);
        var championParticipant = await db.GameEventParticipants.SingleAsync(x => x.PlayerId == champion);
        var survivorParticipant = await db.GameEventParticipants.SingleAsync(x => x.PlayerId == survivor.PlayerId);
        survivorParticipant.FinalRank.Should().Be(1);
        championParticipant.FinalRank.Should().NotBe(1);

        var winnerPrize = econ.Applied.Single(x => x.PlayerId == survivor.PlayerId);
        winnerPrize.Lines.Single(l => l.Currency == CurrencyType.Coins).Delta.Should().Be(250 + 300);
    }
}
