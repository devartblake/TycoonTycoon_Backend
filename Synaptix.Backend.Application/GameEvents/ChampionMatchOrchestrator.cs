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

        // End the match: winner = surviving champion, else the first remaining
        // survivor by entry order (the authoritative rank-1 is assigned by the
        // close handler; this is for the ended broadcast).
        var remaining = await db.GameEventParticipants
            .Where(x => x.GameEventId == gameEventId && x.EliminatedAt == null)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct);

        Guid? winner = championAlive
            ? championId
            : remaining.FirstOrDefault()?.PlayerId;

        var jackpotAwarded = ev.EffectiveJackpot;
        await closer.CloseAsync(gameEventId, ct);

        await notifier.NotifyMatchEndedAsync(new ChampionMatchEndedMessage(
            gameEventId, winner, championAlive, jackpotAwarded, roundNumber), ct);
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
