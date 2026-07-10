using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Application.Tests.GameEvents;

public sealed class ChampionMatchOrchestratorTests
{
    private sealed class RecordingScheduler : IChampionRoundScheduler
    {
        public List<(Guid EventId, int Round)> Scheduled { get; } = [];
        public List<Guid> DuelsScheduled { get; } = [];
        public void ScheduleResolve(Guid gameEventId, int roundNumber, DateTimeOffset dueUtc)
            => Scheduled.Add((gameEventId, roundNumber));
        public void ScheduleDuelResolve(Guid duelId, DateTimeOffset dueUtc)
            => DuelsScheduled.Add(duelId);
    }

    private sealed class RecordingCloser : IChampionMatchCloser
    {
        public int Calls { get; private set; }
        public Task CloseAsync(Guid gameEventId, CancellationToken ct) { Calls++; return Task.CompletedTask; }
    }

    private sealed class RecordingNotifier : IGameEventNotifier
    {
        public List<ChampionRoundStartedMessage> Started { get; } = [];
        public List<ChampionRoundResolvedMessage> Resolved { get; } = [];
        public List<ChampionMatchEndedMessage> Ended { get; } = [];
        public Task NotifyEliminationAsync(GameEventEliminationMessage m, CancellationToken ct) => Task.CompletedTask;
        public Task NotifyEventClosedAsync(GameEventClosedMessage m, CancellationToken ct) => Task.CompletedTask;
        public List<ChampionDuelStartedMessage> DuelsStarted { get; } = [];
        public List<ChampionDuelResolvedMessage> DuelsResolved { get; } = [];
        public Task NotifyRoundStartedAsync(ChampionRoundStartedMessage m, CancellationToken ct) { Started.Add(m); return Task.CompletedTask; }
        public Task NotifyRoundResolvedAsync(ChampionRoundResolvedMessage m, CancellationToken ct) { Resolved.Add(m); return Task.CompletedTask; }
        public Task NotifyMatchEndedAsync(ChampionMatchEndedMessage m, CancellationToken ct) { Ended.Add(m); return Task.CompletedTask; }
        public Task NotifyDuelStartedAsync(ChampionDuelStartedMessage m, CancellationToken ct) { DuelsStarted.Add(m); return Task.CompletedTask; }
        public Task NotifyDuelResolvedAsync(ChampionDuelResolvedMessage m, CancellationToken ct) { DuelsResolved.Add(m); return Task.CompletedTask; }
    }

    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new AppDb(opts, dispatcher: null);
    }

    private sealed class Harness
    {
        public required AppDb Db;
        public required ChampionMatchOrchestrator Orchestrator;
        public required RecordingScheduler Scheduler;
        public required RecordingCloser Closer;
        public required RecordingNotifier Notifier;
    }

    private static Harness NewHarness(AppDb db, int maxRounds = 15)
    {
        var scheduler = new RecordingScheduler();
        var closer = new RecordingCloser();
        var notifier = new RecordingNotifier();
        var orch = new ChampionMatchOrchestrator(db, notifier, scheduler, closer,
            Options.Create(new ChampionRoundOptions { AnswerWindowSeconds = 30, MaxRounds = maxRounds }));
        return new Harness { Db = db, Orchestrator = orch, Scheduler = scheduler, Closer = closer, Notifier = notifier };
    }

    private static async Task<Guid> AddApprovedQuestionAsync(AppDb db, string correctOptionId = "A")
    {
        var q = new Question("What is 2+2?", "math", QuestionDifficulty.Easy, correctOptionId, null);
        q.SetStatus("Approved");
        q.ReplaceOptions(new[]
        {
            new QuestionOption(q.Id, "A", "4"),
            new QuestionOption(q.Id, "B", "5"),
        });
        db.Questions.Add(q);
        await db.SaveChangesAsync();
        return q.Id;
    }

    private static async Task<GameEvent> LiveEventAsync(AppDb db, Guid championId, IEnumerable<Guid> challengers)
    {
        var ev = new GameEvent(GameEvent.ChampionVsTierKind, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, 0, 100);
        ev.SeedChampion(championId);
        ev.Open(DateTimeOffset.UtcNow);
        ev.Start(DateTimeOffset.UtcNow);
        db.GameEvents.Add(ev);
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, championId, Guid.NewGuid()));
        foreach (var c in challengers)
            db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, c, Guid.NewGuid()));
        await db.SaveChangesAsync();
        return ev;
    }

    private static Guid CurrentRoundQuestion(AppDb db, Guid eventId, int round) =>
        db.ChampionRounds.Single(x => x.GameEventId == eventId && x.RoundNumber == round).Id;

    [Fact]
    public async Task StartMatch_OpensRound1_AndSchedulesResolve()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [Guid.NewGuid(), Guid.NewGuid()]);
        var h = NewHarness(db);

        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        (await db.ChampionRounds.CountAsync(x => x.GameEventId == ev.Id)).Should().Be(1);
        h.Notifier.Started.Should().ContainSingle();
        h.Notifier.Started[0].Options.Should().HaveCount(2);
        h.Scheduler.Scheduled.Should().ContainSingle(x => x.Round == 1);

        // Idempotent.
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);
        (await db.ChampionRounds.CountAsync(x => x.GameEventId == ev.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Resolve_EliminatesWrongAndAbsent_GrowsJackpot_AndAdvances()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        await AddApprovedQuestionAsync(db); // a second question for round 2
        var champ = Guid.NewGuid();
        var right = Guid.NewGuid();
        var wrong = Guid.NewGuid();
        var absent = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [right, wrong, absent]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        // champion + one challenger answer correctly, one wrong, one silent.
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, champ, "A", CancellationToken.None);
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, right, "A", CancellationToken.None);
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, wrong, "B", CancellationToken.None);

        await h.Orchestrator.ResolveRoundAsync(ev.Id, 1, CancellationToken.None);

        var resolved = h.Notifier.Resolved.Single();
        resolved.EliminatedPlayerIds.Should().BeEquivalentTo([wrong, absent]);
        resolved.SurvivorsRemaining.Should().Be(2);
        resolved.ChampionAlive.Should().BeTrue();

        // 2 eliminations × 50 → jackpot 100.
        (await db.GameEvents.SingleAsync(x => x.Id == ev.Id)).JackpotPool.Should().Be(100);

        // Two survivors, not at cap → round 2 opened, match not closed.
        h.Closer.Calls.Should().Be(0);
        (await db.ChampionRounds.CountAsync(x => x.GameEventId == ev.Id)).Should().Be(2);
    }

    [Fact]
    public async Task Resolve_ChampionEliminated_EndsMatch_AndCloses()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var survivor = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [survivor]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        // Champion answers wrong (dethroned); challenger correct.
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, champ, "B", CancellationToken.None);
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, survivor, "A", CancellationToken.None);

        await h.Orchestrator.ResolveRoundAsync(ev.Id, 1, CancellationToken.None);

        h.Closer.Calls.Should().Be(1);
        var ended = h.Notifier.Ended.Single();
        ended.ChampionDefended.Should().BeFalse();
        ended.WinnerPlayerId.Should().Be(survivor);
    }

    [Fact]
    public async Task Resolve_ChampionNoShow_ForfeitsToChallenger()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var survivor = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [survivor]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        // Champion never answers; challenger answers correctly.
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, survivor, "A", CancellationToken.None);
        await h.Orchestrator.ResolveRoundAsync(ev.Id, 1, CancellationToken.None);

        h.Notifier.Ended.Single().ChampionDefended.Should().BeFalse();
        h.Notifier.Ended.Single().WinnerPlayerId.Should().Be(survivor);
        h.Closer.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Resolve_IsIdempotent()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [Guid.NewGuid(), Guid.NewGuid()]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, champ, "A", CancellationToken.None);

        await h.Orchestrator.ResolveRoundAsync(ev.Id, 1, CancellationToken.None);
        var jackpotAfterFirst = (await db.GameEvents.SingleAsync(x => x.Id == ev.Id)).JackpotPool;

        // Re-fire the same resolve — must not double-eliminate or double-count.
        await h.Orchestrator.ResolveRoundAsync(ev.Id, 1, CancellationToken.None);
        (await db.GameEvents.SingleAsync(x => x.Id == ev.Id)).JackpotPool.Should().Be(jackpotAfterFirst);
    }

    [Fact]
    public async Task Submit_Rejects_EliminatedAndNonParticipants()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [Guid.NewGuid()]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        (await h.Orchestrator.SubmitAnswerAsync(ev.Id, Guid.NewGuid(), "A", CancellationToken.None))
            .Should().Be("NotParticipant");
        (await h.Orchestrator.SubmitAnswerAsync(ev.Id, champ, "A", CancellationToken.None))
            .Should().Be("Accepted");
    }

    // ── Watchdog redundancy sweep ─────────────────────────────────────────

    [Fact]
    public async Task Watchdog_ResolvesOverdueRound_Idempotently()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [Guid.NewGuid()]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);
        await h.Orchestrator.SubmitAnswerAsync(ev.Id, champ, "A", CancellationToken.None);

        // Force the round's deadline into the past (Hangfire "dropped" it).
        var round = await db.ChampionRounds.SingleAsync(x => x.GameEventId == ev.Id);
        db.Entry(round).Property("DeadlineUtc").CurrentValue =
            DateTimeOffset.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var swept = await h.Orchestrator.ResolveOverdueRoundsAsync(CancellationToken.None);
        swept.Should().Be(1);
        (await db.ChampionRounds.SingleAsync(x => x.GameEventId == ev.Id)).Status
            .Should().Be("Resolved");

        // Nothing left overdue on a second sweep.
        (await h.Orchestrator.ResolveOverdueRoundsAsync(CancellationToken.None)).Should().Be(0);
    }

    // ── Champion duels ────────────────────────────────────────────────────

    [Fact]
    public async Task Duel_ChampionWins_ChallengerCulled_OthersUnaffected()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db); // round 1
        await AddApprovedQuestionAsync(db); // duel question
        var champ = Guid.NewGuid();
        var target = Guid.NewGuid();
        var bystander = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [target, bystander]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);

        (await h.Orchestrator.StartDuelAsync(ev.Id, champ, target, CancellationToken.None))
            .Should().Be("Started");
        h.Notifier.DuelsStarted.Should().ContainSingle();
        var duel = await db.ChampionDuels.SingleAsync();

        await h.Orchestrator.SubmitDuelAnswerAsync(ev.Id, champ, "A", CancellationToken.None);
        await h.Orchestrator.SubmitDuelAnswerAsync(ev.Id, target, "B", CancellationToken.None);
        await h.Orchestrator.ResolveDuelAsync(duel.Id, CancellationToken.None);

        var resolved = h.Notifier.DuelsResolved.Single();
        resolved.WinnerPlayerId.Should().Be(champ);
        resolved.LoserPlayerId.Should().Be(target);

        (await db.GameEventParticipants.SingleAsync(x => x.PlayerId == target))
            .EliminatedAt.Should().NotBeNull();
        // The bystander is untouched by the duel.
        (await db.GameEventParticipants.SingleAsync(x => x.PlayerId == bystander))
            .EliminatedAt.Should().BeNull();
        // Elimination fed the jackpot.
        (await db.GameEvents.SingleAsync(x => x.Id == ev.Id)).JackpotPool.Should().Be(50);
        h.Closer.Calls.Should().Be(0); // 2 survivors remain
    }

    [Fact]
    public async Task Duel_ChallengerWins_ChampionDethroned_MatchCloses()
    {
        await using var db = NewDb();
        await AddApprovedQuestionAsync(db);
        await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var target = Guid.NewGuid();
        var other = Guid.NewGuid();
        var ev = await LiveEventAsync(db, champ, [target, other]);
        var h = NewHarness(db);
        await h.Orchestrator.StartMatchAsync(ev.Id, CancellationToken.None);
        await h.Orchestrator.StartDuelAsync(ev.Id, champ, target, CancellationToken.None);
        var duel = await db.ChampionDuels.SingleAsync();

        // Champion wrong, challenger correct → champion dethroned.
        await h.Orchestrator.SubmitDuelAnswerAsync(ev.Id, champ, "B", CancellationToken.None);
        await h.Orchestrator.SubmitDuelAnswerAsync(ev.Id, target, "A", CancellationToken.None);
        await h.Orchestrator.ResolveDuelAsync(duel.Id, CancellationToken.None);

        h.Notifier.DuelsResolved.Single().ChampionAlive.Should().BeFalse();
        h.Closer.Calls.Should().Be(1);
        h.Notifier.Ended.Single().ChampionDefended.Should().BeFalse();
    }

    [Fact]
    public async Task Duel_OnlyChampionCanStart_AndLimitEnforced()
    {
        await using var db = NewDb();
        for (var i = 0; i < 6; i++) await AddApprovedQuestionAsync(db);
        var champ = Guid.NewGuid();
        var challengers = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var ev = await LiveEventAsync(db, champ, challengers);
        var h = NewHarness(db); // MaxDuelsPerMatch defaults to 3

        // A non-champion cannot start a duel.
        (await h.Orchestrator.StartDuelAsync(ev.Id, challengers[0], challengers[1], CancellationToken.None))
            .Should().Be("NotChampion");

        // Champion runs the 3 allowed duels (each resolved so the next can start).
        for (var i = 0; i < 3; i++)
        {
            (await h.Orchestrator.StartDuelAsync(ev.Id, champ, challengers[i], CancellationToken.None))
                .Should().Be("Started");
            var duel = await db.ChampionDuels.SingleAsync(x => x.Status == "Open");
            await h.Orchestrator.SubmitDuelAnswerAsync(ev.Id, champ, "A", CancellationToken.None);
            await h.Orchestrator.ResolveDuelAsync(duel.Id, CancellationToken.None);
        }

        // 4th duel exceeds the per-match cap.
        (await h.Orchestrator.StartDuelAsync(ev.Id, champ, challengers[3], CancellationToken.None))
            .Should().Be("DuelLimitReached");
    }
}
