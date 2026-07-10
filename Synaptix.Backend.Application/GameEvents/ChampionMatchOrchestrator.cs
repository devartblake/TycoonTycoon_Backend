using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Application.GameEvents;

/// <summary>
/// Drives a live champion_vs_tier match through synchronized rounds:
/// start round → broadcast question → collect answers → resolve (eliminate the
/// wrong/absent, grow the jackpot) → next round or close. A champion who
/// doesn't answer is eliminated like anyone else, so a no-show forfeits and
/// the asymmetric close crowns the last surviving challenger.
/// </summary>
public sealed class ChampionMatchOrchestrator(
    IAppDb db,
    IGameEventNotifier notifier,
    IChampionRoundScheduler scheduler,
    IChampionMatchCloser closer,
    IOptions<ChampionRoundOptions> options)
{
    private const int EliminationJackpotIncrement = 50;

    /// <summary>Start the match at round 1. Idempotent: no-op if a round already exists.</summary>
    public async Task StartMatchAsync(Guid gameEventId, CancellationToken ct)
    {
        var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null || ev.Kind != GameEvent.ChampionVsTierKind || ev.Status != GameEventStatus.Live)
            return;

        var hasRounds = await db.ChampionRounds.AnyAsync(x => x.GameEventId == gameEventId, ct);
        if (hasRounds)
            return;

        await StartRoundAsync(ev, 1, ct);
    }

    /// <summary>Record a player's answer to the current open round.</summary>
    public async Task<string> SubmitAnswerAsync(Guid gameEventId, Guid playerId, string optionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(optionId))
            return "InvalidAnswer";

        var round = await db.ChampionRounds
            .Where(x => x.GameEventId == gameEventId && x.Status == ChampionRound.Statuses.Open)
            .OrderByDescending(x => x.RoundNumber)
            .FirstOrDefaultAsync(ct);
        if (round is null)
            return "NoOpenRound";

        var now = DateTimeOffset.UtcNow;
        if (now > round.DeadlineUtc)
            return "RoundClosed";

        var participant = await db.GameEventParticipants
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.PlayerId == playerId, ct);
        if (participant is null)
            return "NotParticipant";
        if (participant.EliminatedAt.HasValue)
            return "Eliminated";

        var existing = await db.ChampionRoundAnswers
            .FirstOrDefaultAsync(x => x.RoundId == round.Id && x.PlayerId == playerId, ct);
        if (existing is null)
            db.ChampionRoundAnswers.Add(new ChampionRoundAnswer(round.Id, gameEventId, playerId, optionId.Trim(), now));
        else
            existing.Update(optionId.Trim(), now);

        await db.SaveChangesAsync(ct);
        return "Accepted";
    }

    /// <summary>
    /// Resolve a round at its deadline: grade, eliminate the wrong/absent, then
    /// advance or end the match. Idempotent — a re-fired job is a no-op.
    /// </summary>
    public async Task ResolveRoundAsync(Guid gameEventId, int roundNumber, CancellationToken ct)
    {
        var round = await db.ChampionRounds
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.RoundNumber == roundNumber, ct);
        if (round is null || !round.IsOpen)
            return;

        var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null)
            return;

        // If the match already ended (e.g. the champion was dethroned in a
        // duel), retire the round without eliminating or broadcasting.
        if (ev.Status == GameEventStatus.Closed)
        {
            round.MarkResolved(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var answers = await db.ChampionRoundAnswers
            .Where(x => x.RoundId == round.Id)
            .ToListAsync(ct);
        var correctByPlayer = new HashSet<Guid>();
        foreach (var a in answers)
        {
            var isCorrect = string.Equals(a.SelectedOptionId, round.CorrectOptionId, StringComparison.OrdinalIgnoreCase);
            a.Grade(isCorrect);
            if (isCorrect)
                correctByPlayer.Add(a.PlayerId);
        }

        var alive = await db.GameEventParticipants
            .Where(x => x.GameEventId == gameEventId && x.EliminatedAt == null)
            .ToListAsync(ct);

        var championId = ev.ChampionPlayerId;
        var championWasAlive = championId is Guid cid && alive.Any(x => x.PlayerId == cid);

        var eliminated = new List<Guid>();
        foreach (var p in alive)
        {
            if (correctByPlayer.Contains(p.PlayerId))
                continue; // survived
            p.EliminatedAt = now;
            if (ev.FeedsJackpot)
                ev.AddToJackpot(EliminationJackpotIncrement);
            eliminated.Add(p.PlayerId);
        }

        round.MarkResolved(now);
        await db.SaveChangesAsync(ct);

        var survivors = alive.Count - eliminated.Count;
        var championAlive = championWasAlive && championId is Guid cid2 && !eliminated.Contains(cid2);

        await notifier.NotifyRoundResolvedAsync(new ChampionRoundResolvedMessage(
            gameEventId, roundNumber, round.CorrectOptionId, eliminated, survivors, championAlive, ev.JackpotPool), ct);

        var matchOver = !championAlive || survivors <= 1 || roundNumber >= options.Value.MaxRounds;
        if (!matchOver)
        {
            await StartRoundAsync(ev, roundNumber + 1, ct);
            return;
        }

        await EndMatchAsync(ev, championAlive, ct);
    }

    /// <summary>
    /// End the match: close (which authoritatively assigns rank 1 + pays the
    /// jackpot) and broadcast the result. Winner for the broadcast is the
    /// surviving champion, else the first remaining survivor by entry order.
    /// </summary>
    private async Task EndMatchAsync(GameEvent ev, bool championAlive, CancellationToken ct)
    {
        var remaining = await db.GameEventParticipants
            .Where(x => x.GameEventId == ev.Id && x.EliminatedAt == null)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        Guid? winner = championAlive ? ev.ChampionPlayerId : remaining.FirstOrDefault()?.PlayerId;
        var roundsPlayed = await db.ChampionRounds.CountAsync(x => x.GameEventId == ev.Id, ct);
        var jackpotAwarded = ev.EffectiveJackpot;

        await closer.CloseAsync(ev.Id, ct);

        await notifier.NotifyMatchEndedAsync(new ChampionMatchEndedMessage(
            ev.Id, winner, championAlive, jackpotAwarded, roundsPlayed), ct);
    }

    /// <summary>
    /// Redundancy sweep for the hosted watchdog: resolve any round whose
    /// deadline passed (by the grace margin) but that the primary Hangfire job
    /// didn't close — a dropped job, a restart, a clock skew. ResolveRoundAsync
    /// is idempotent, so this never conflicts with a job that did fire.
    /// Returns the number of rounds it resolved.
    /// </summary>
    public async Task<int> ResolveOverdueRoundsAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-options.Value.WatchdogGraceSeconds);
        var overdue = await db.ChampionRounds.AsNoTracking()
            .Where(x => x.Status == ChampionRound.Statuses.Open && x.DeadlineUtc <= cutoff)
            .Select(x => new { x.GameEventId, x.RoundNumber })
            .ToListAsync(ct);

        foreach (var r in overdue)
            await ResolveRoundAsync(r.GameEventId, r.RoundNumber, ct);

        return overdue.Count;
    }

    // ── Champion duels ────────────────────────────────────────────────────

    /// <summary>
    /// The champion calls out one alive challenger for a head-to-head duel on a
    /// single question. Only the champion can initiate; capped per match; only
    /// the two duelists are affected.
    /// </summary>
    public async Task<string> StartDuelAsync(Guid gameEventId, Guid championId, Guid challengerId, CancellationToken ct)
    {
        if (challengerId == Guid.Empty || challengerId == championId)
            return "InvalidChallenger";

        var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null || ev.Kind != GameEvent.ChampionVsTierKind)
            return "NotFound";
        if (ev.Status != GameEventStatus.Live)
            return "InvalidStatus";
        if (ev.ChampionPlayerId != championId)
            return "NotChampion";

        var champion = await db.GameEventParticipants
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.PlayerId == championId, ct);
        if (champion is null || champion.EliminatedAt.HasValue)
            return "ChampionEliminated";

        var challenger = await db.GameEventParticipants
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.PlayerId == challengerId, ct);
        if (challenger is null || challenger.EliminatedAt.HasValue)
            return "InvalidChallenger";

        var hasOpenDuel = await db.ChampionDuels
            .AnyAsync(x => x.GameEventId == gameEventId && x.Status == ChampionDuel.Statuses.Open, ct);
        if (hasOpenDuel)
            return "DuelInProgress";

        var duelsSoFar = await db.ChampionDuels.CountAsync(x => x.GameEventId == gameEventId, ct);
        if (duelsSoFar >= options.Value.MaxDuelsPerMatch)
            return "DuelLimitReached";

        var question = await PickQuestionAsync(gameEventId, ct);
        if (question is null)
            return "NoQuestions";

        var now = DateTimeOffset.UtcNow;
        var deadline = now.AddSeconds(options.Value.DuelWindowSeconds);
        var duel = new ChampionDuel(gameEventId, championId, challengerId, question.Id, question.CorrectOptionId, now, deadline);
        db.ChampionDuels.Add(duel);
        await db.SaveChangesAsync(ct);

        var optionDtos = question.Options
            .Select(o => new ChampionRoundOptionDto(o.OptionId, o.Text))
            .ToList();

        await notifier.NotifyDuelStartedAsync(new ChampionDuelStartedMessage(
            gameEventId, duel.Id, championId, challengerId, question.Id, question.Text, optionDtos, deadline), ct);

        scheduler.ScheduleDuelResolve(duel.Id, deadline);
        return "Started";
    }

    /// <summary>Record a duelist's answer to the current open duel.</summary>
    public async Task<string> SubmitDuelAnswerAsync(Guid gameEventId, Guid playerId, string optionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(optionId))
            return "InvalidAnswer";

        var duel = await db.ChampionDuels
            .Where(x => x.GameEventId == gameEventId && x.Status == ChampionDuel.Statuses.Open)
            .OrderByDescending(x => x.StartedAtUtc)
            .FirstOrDefaultAsync(ct);
        if (duel is null)
            return "NoOpenDuel";
        if (!duel.Involves(playerId))
            return "NotDuelist";

        var now = DateTimeOffset.UtcNow;
        if (now > duel.DeadlineUtc)
            return "DuelClosed";

        duel.RecordAnswer(playerId, optionId.Trim(), now);
        await db.SaveChangesAsync(ct);
        return "Accepted";
    }

    /// <summary>
    /// Resolve a duel: correct beats wrong, then speed; the champion wins exact
    /// ties (bounded by the per-match duel cap). The loser is eliminated and
    /// feeds the jackpot; a dethroned champion ends the match.
    /// </summary>
    public async Task ResolveDuelAsync(Guid duelId, CancellationToken ct)
    {
        var duel = await db.ChampionDuels.FirstOrDefaultAsync(x => x.Id == duelId, ct);
        if (duel is null || !duel.IsOpen)
            return;

        var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == duel.GameEventId, ct);
        if (ev is null)
            return;

        var now = DateTimeOffset.UtcNow;

        // Already-ended match: retire the duel with no contest.
        if (ev.Status == GameEventStatus.Closed)
        {
            duel.Void(now);
            await db.SaveChangesAsync(ct);
            return;
        }

        bool championCorrect = duel.ChampionOptionId is { } co
            && string.Equals(co, duel.CorrectOptionId, StringComparison.OrdinalIgnoreCase);
        bool challengerCorrect = duel.ChallengerOptionId is { } ho
            && string.Equals(ho, duel.CorrectOptionId, StringComparison.OrdinalIgnoreCase);

        Guid winner, loser;
        if (championCorrect && !challengerCorrect)
        {
            (winner, loser) = (duel.ChampionPlayerId, duel.ChallengerPlayerId);
        }
        else if (challengerCorrect && !championCorrect)
        {
            (winner, loser) = (duel.ChallengerPlayerId, duel.ChampionPlayerId);
        }
        else if (championCorrect && challengerCorrect)
        {
            // Both right — faster wins; champion takes an exact tie.
            var champTime = duel.ChampionAnsweredAtUtc ?? DateTimeOffset.MaxValue;
            var challTime = duel.ChallengerAnsweredAtUtc ?? DateTimeOffset.MaxValue;
            (winner, loser) = champTime <= challTime
                ? (duel.ChampionPlayerId, duel.ChallengerPlayerId)
                : (duel.ChallengerPlayerId, duel.ChampionPlayerId);
        }
        else
        {
            // Both wrong — the champion keeps the crown, the challenger is culled.
            (winner, loser) = (duel.ChampionPlayerId, duel.ChallengerPlayerId);
        }

        duel.Resolve(winner, loser, now);

        var loserParticipant = await db.GameEventParticipants
            .FirstOrDefaultAsync(x => x.GameEventId == ev.Id && x.PlayerId == loser, ct);
        if (loserParticipant is not null && !loserParticipant.EliminatedAt.HasValue)
        {
            loserParticipant.EliminatedAt = now;
            if (ev.FeedsJackpot)
                ev.AddToJackpot(EliminationJackpotIncrement);
        }

        await db.SaveChangesAsync(ct);

        var championAlive = loser != ev.ChampionPlayerId;
        var survivors = await db.GameEventParticipants
            .CountAsync(x => x.GameEventId == ev.Id && x.EliminatedAt == null, ct);

        await notifier.NotifyDuelResolvedAsync(new ChampionDuelResolvedMessage(
            ev.Id, duel.Id, winner, loser, duel.CorrectOptionId, championAlive, survivors, ev.JackpotPool), ct);

        // A dethroned champion, or a match down to one survivor, ends it.
        if (!championAlive)
            await EndMatchAsync(ev, championAlive: false, ct);
        else if (survivors <= 1)
            await EndMatchAsync(ev, championAlive: true, ct);
    }

    /// <summary>Watchdog redundancy sweep for overdue duels (see rounds sweep).</summary>
    public async Task<int> ResolveOverdueDuelsAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-options.Value.WatchdogGraceSeconds);
        var overdue = await db.ChampionDuels.AsNoTracking()
            .Where(x => x.Status == ChampionDuel.Statuses.Open && x.DeadlineUtc <= cutoff)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var id in overdue)
            await ResolveDuelAsync(id, ct);

        return overdue.Count;
    }

    private async Task StartRoundAsync(GameEvent ev, int roundNumber, CancellationToken ct)
    {
        var question = await PickQuestionAsync(ev.Id, ct);
        if (question is null)
        {
            // Out of fresh questions — end the match on the current standings.
            await closer.CloseAsync(ev.Id, ct);
            await notifier.NotifyMatchEndedAsync(new ChampionMatchEndedMessage(
                ev.Id, ev.ChampionPlayerId, ChampionDefended: true, ev.EffectiveJackpot, roundNumber - 1), ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var deadline = now.AddSeconds(options.Value.AnswerWindowSeconds);
        var round = new ChampionRound(ev.Id, roundNumber, question.Id, question.CorrectOptionId, now, deadline);
        db.ChampionRounds.Add(round);
        await db.SaveChangesAsync(ct);

        var aliveCount = await db.GameEventParticipants
            .CountAsync(x => x.GameEventId == ev.Id && x.EliminatedAt == null, ct);

        var optionDtos = question.Options
            .Select(o => new ChampionRoundOptionDto(o.OptionId, o.Text))
            .ToList();

        await notifier.NotifyRoundStartedAsync(new ChampionRoundStartedMessage(
            ev.Id, roundNumber, question.Id, question.Text, optionDtos, deadline, aliveCount, ev.JackpotPool), ct);

        scheduler.ScheduleResolve(ev.Id, roundNumber, deadline);
    }

    private async Task<Question?> PickQuestionAsync(Guid gameEventId, CancellationToken ct)
    {
        var usedIds = await db.ChampionRounds
            .Where(x => x.GameEventId == gameEventId)
            .Select(x => x.QuestionId)
            .ToListAsync(ct);

        var candidateIds = await db.Questions
            .AsNoTracking()
            .Where(q => q.Status == "Approved" && !usedIds.Contains(q.Id))
            .OrderBy(q => q.CreatedAtUtc)
            .Select(q => q.Id)
            .Take(options.Value.QuestionSampleSize)
            .ToListAsync(ct);

        if (candidateIds.Count == 0)
            return null;

        var pickedId = candidateIds[Random.Shared.Next(candidateIds.Count)];
        return await db.Questions
            .Include(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == pickedId, ct);
    }
}
