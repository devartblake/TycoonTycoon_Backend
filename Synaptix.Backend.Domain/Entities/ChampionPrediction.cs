namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// A spectator's no-loss prediction on a champion_vs_tier match: will the
/// champion defend the crown? Correct predictors share a fixed reward pool at
/// close; nothing is ever staked or lost. Open to the whole player base, not
/// just the ~100 participants — this is the event's audience hook.
/// </summary>
public sealed class ChampionPrediction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GameEventId { get; private set; }
    public Guid PlayerId { get; private set; }

    /// <summary>The player's call: true = champion defends, false = a challenger dethrones.</summary>
    public bool PredictedChampionDefends { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    public bool Resolved { get; private set; }
    public bool? WasCorrect { get; private set; }
    public int RewardCoins { get; private set; }
    public int RewardXp { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }

    private ChampionPrediction() { } // EF

    public ChampionPrediction(Guid gameEventId, Guid playerId, bool predictedChampionDefends)
    {
        GameEventId = gameEventId;
        PlayerId = playerId;
        PredictedChampionDefends = predictedChampionDefends;
    }

    /// <summary>Change the pick (allowed only while predictions are open).</summary>
    public void Update(bool predictedChampionDefends)
    {
        PredictedChampionDefends = predictedChampionDefends;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Resolve(bool championDefended, int rewardCoins, int rewardXp, DateTimeOffset at)
    {
        WasCorrect = PredictedChampionDefends == championDefended;
        RewardCoins = WasCorrect == true ? rewardCoins : 0;
        RewardXp = WasCorrect == true ? rewardXp : 0;
        Resolved = true;
        ResolvedAtUtc = at;
    }
}
