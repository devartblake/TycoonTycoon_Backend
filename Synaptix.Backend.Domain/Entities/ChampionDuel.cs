namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// A champion-initiated head-to-head: the champion targets one alive challenger
/// on a single question. Whoever answers better stays; the loser is eliminated.
/// It only affects the two duelists — a targeted way to cull the mob alongside
/// the simultaneous main rounds.
/// </summary>
public sealed class ChampionDuel
{
    public static class Statuses
    {
        public const string Open = "Open";
        public const string Resolved = "Resolved";
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GameEventId { get; private set; }
    public Guid ChampionPlayerId { get; private set; }
    public Guid ChallengerPlayerId { get; private set; }

    public Guid QuestionId { get; private set; }
    public string CorrectOptionId { get; private set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset DeadlineUtc { get; private set; }
    public string Status { get; private set; } = Statuses.Open;

    public string? ChampionOptionId { get; private set; }
    public DateTimeOffset? ChampionAnsweredAtUtc { get; private set; }
    public string? ChallengerOptionId { get; private set; }
    public DateTimeOffset? ChallengerAnsweredAtUtc { get; private set; }

    public Guid? WinnerPlayerId { get; private set; }
    public Guid? LoserPlayerId { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private ChampionDuel() { } // EF

    public ChampionDuel(
        Guid gameEventId,
        Guid championPlayerId,
        Guid challengerPlayerId,
        Guid questionId,
        string correctOptionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset deadlineUtc)
    {
        GameEventId = gameEventId;
        ChampionPlayerId = championPlayerId;
        ChallengerPlayerId = challengerPlayerId;
        QuestionId = questionId;
        CorrectOptionId = correctOptionId;
        StartedAtUtc = startedAtUtc;
        DeadlineUtc = deadlineUtc;
        Status = Statuses.Open;
    }

    public bool IsOpen => Status == Statuses.Open;
    public bool Involves(Guid playerId) => playerId == ChampionPlayerId || playerId == ChallengerPlayerId;

    public void RecordAnswer(Guid playerId, string optionId, DateTimeOffset at)
    {
        if (playerId == ChampionPlayerId)
        {
            ChampionOptionId = optionId;
            ChampionAnsweredAtUtc = at;
        }
        else if (playerId == ChallengerPlayerId)
        {
            ChallengerOptionId = optionId;
            ChallengerAnsweredAtUtc = at;
        }
    }

    public void Resolve(Guid winnerPlayerId, Guid loserPlayerId, DateTimeOffset at)
    {
        WinnerPlayerId = winnerPlayerId;
        LoserPlayerId = loserPlayerId;
        Status = Statuses.Resolved;
        ResolvedAtUtc = at;
    }

    /// <summary>Retire a duel with no result (the match already ended).</summary>
    public void Void(DateTimeOffset at)
    {
        Status = Statuses.Resolved;
        ResolvedAtUtc = at;
    }
}
