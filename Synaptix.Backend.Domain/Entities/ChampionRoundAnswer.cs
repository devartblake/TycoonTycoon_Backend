namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// A player's answer to a live champion round. One per (round, player); the
/// last write before the deadline wins. Absence of a row = no answer = out.
/// </summary>
public sealed class ChampionRoundAnswer
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid RoundId { get; private set; }
    public Guid GameEventId { get; private set; }
    public Guid PlayerId { get; private set; }

    public string SelectedOptionId { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    private ChampionRoundAnswer() { } // EF

    public ChampionRoundAnswer(Guid roundId, Guid gameEventId, Guid playerId, string selectedOptionId, DateTimeOffset submittedAtUtc)
    {
        RoundId = roundId;
        GameEventId = gameEventId;
        PlayerId = playerId;
        SelectedOptionId = selectedOptionId;
        SubmittedAtUtc = submittedAtUtc;
    }

    public void Update(string selectedOptionId, DateTimeOffset submittedAtUtc)
    {
        SelectedOptionId = selectedOptionId;
        SubmittedAtUtc = submittedAtUtc;
    }

    public void Grade(bool isCorrect) => IsCorrect = isCorrect;
}
