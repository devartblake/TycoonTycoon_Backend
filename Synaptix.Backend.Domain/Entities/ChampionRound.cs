namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// One live round of a champion_vs_tier match: a question is broadcast, players
/// answer within a window, then the round resolves and eliminates the wrong /
/// absent. Persisted so the resolve step is durable and restart-safe.
/// </summary>
public sealed class ChampionRound
{
    public static class Statuses
    {
        public const string Open = "Open";
        public const string Resolved = "Resolved";
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GameEventId { get; private set; }
    public int RoundNumber { get; private set; }

    public Guid QuestionId { get; private set; }
    public string CorrectOptionId { get; private set; } = string.Empty;

    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset DeadlineUtc { get; private set; }
    public string Status { get; private set; } = Statuses.Open;
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private ChampionRound() { } // EF

    public ChampionRound(
        Guid gameEventId,
        int roundNumber,
        Guid questionId,
        string correctOptionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset deadlineUtc)
    {
        GameEventId = gameEventId;
        RoundNumber = roundNumber;
        QuestionId = questionId;
        CorrectOptionId = correctOptionId;
        StartedAtUtc = startedAtUtc;
        DeadlineUtc = deadlineUtc;
        Status = Statuses.Open;
    }

    public bool IsOpen => Status == Statuses.Open;

    public void MarkResolved(DateTimeOffset at)
    {
        Status = Statuses.Resolved;
        ResolvedAtUtc = at;
    }
}
