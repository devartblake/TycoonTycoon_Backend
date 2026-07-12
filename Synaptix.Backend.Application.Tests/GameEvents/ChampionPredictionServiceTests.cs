using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.GameEvents;

public sealed class ChampionPredictionServiceTests
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

    private static (ChampionPredictionService Svc, FakeEconomy Economy) NewService(
        AppDb db, ChampionPredictionOptions? opts = null)
    {
        var economy = new FakeEconomy();
        return (new ChampionPredictionService(db, economy, Options.Create(opts ?? new ChampionPredictionOptions())), economy);
    }

    private static async Task<GameEvent> EventAsync(AppDb db, GameEventStatus status)
    {
        var ev = new GameEvent(GameEvent.ChampionVsTierKind, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, 0, 100);
        ev.SeedChampion(Guid.NewGuid());
        if (status != GameEventStatus.Scheduled) ev.Open(DateTimeOffset.UtcNow);
        if (status == GameEventStatus.Live) ev.Start(DateTimeOffset.UtcNow);
        db.GameEvents.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    [Fact]
    public async Task Predict_AcceptsAndUpserts_WhileOpen()
    {
        await using var db = NewDb();
        var ev = await EventAsync(db, GameEventStatus.Open);
        var (svc, _) = NewService(db);
        var player = Guid.NewGuid();

        (await svc.PredictAsync(ev.Id, player, true, CancellationToken.None)).Should().Be("Accepted");
        (await svc.PredictAsync(ev.Id, player, false, CancellationToken.None)).Should().Be("Accepted");

        var row = await db.ChampionPredictions.SingleAsync(x => x.PlayerId == player);
        row.PredictedChampionDefends.Should().BeFalse(); // last pick wins, one row per player
    }

    [Fact]
    public async Task Predict_Rejected_WhenNotOpen()
    {
        await using var db = NewDb();
        var ev = await EventAsync(db, GameEventStatus.Live);
        var (svc, _) = NewService(db);

        (await svc.PredictAsync(ev.Id, Guid.NewGuid(), true, CancellationToken.None)).Should().Be("Closed");
    }

    [Fact]
    public async Task Resolve_SplitsPoolAmongCorrect_AndPaysThemOnly()
    {
        await using var db = NewDb();
        var ev = await EventAsync(db, GameEventStatus.Open);
        var (svc, economy) = NewService(db, new ChampionPredictionOptions { RewardCoinPool = 1000, CorrectXp = 25 });

        var right1 = Guid.NewGuid();
        var right2 = Guid.NewGuid();
        var wrong = Guid.NewGuid();
        await svc.PredictAsync(ev.Id, right1, true, CancellationToken.None);
        await svc.PredictAsync(ev.Id, right2, true, CancellationToken.None);
        await svc.PredictAsync(ev.Id, wrong, false, CancellationToken.None);

        // Champion defended → the two "defend" predictors split 1000.
        await svc.ResolveAsync(ev.Id, championDefended: true, CancellationToken.None);

        economy.Applied.Should().HaveCount(2);
        var payout = economy.Applied.First();
        payout.Lines.Single(l => l.Currency == CurrencyType.Coins).Delta.Should().Be(500);
        payout.Lines.Single(l => l.Currency == CurrencyType.Xp).Delta.Should().Be(25);
        economy.Applied.Should().NotContain(x => x.PlayerId == wrong);

        var wrongRow = await db.ChampionPredictions.SingleAsync(x => x.PlayerId == wrong);
        wrongRow.WasCorrect.Should().BeFalse();
        wrongRow.RewardCoins.Should().Be(0);
    }

    [Fact]
    public async Task Resolve_IsIdempotent()
    {
        await using var db = NewDb();
        var ev = await EventAsync(db, GameEventStatus.Open);
        var (svc, economy) = NewService(db);
        await svc.PredictAsync(ev.Id, Guid.NewGuid(), true, CancellationToken.None);

        await svc.ResolveAsync(ev.Id, true, CancellationToken.None);
        await svc.ResolveAsync(ev.Id, true, CancellationToken.None);

        economy.Applied.Should().HaveCount(1); // second resolve is a no-op
    }

    [Fact]
    public async Task GetState_ReturnsTally_AndCallerPick()
    {
        await using var db = NewDb();
        var ev = await EventAsync(db, GameEventStatus.Open);
        var (svc, _) = NewService(db);
        var me = Guid.NewGuid();
        await svc.PredictAsync(ev.Id, me, true, CancellationToken.None);
        await svc.PredictAsync(ev.Id, Guid.NewGuid(), false, CancellationToken.None);

        var state = await svc.GetStateAsync(ev.Id, me, CancellationToken.None);

        state.Should().NotBeNull();
        state!.Open.Should().BeTrue();
        state.MyPrediction.Should().BeTrue();
        state.DefendCount.Should().Be(1);
        state.DethroneCount.Should().Be(1);
        state.RewardCoinPool.Should().Be(1000);
    }
}
