namespace Synaptix.Backend.Application.Rewards;

public sealed class RewardOutcomeService
{
    private readonly IRewardRng _rng;

    public RewardOutcomeService(IRewardRng rng)
    {
        _rng = rng;
    }

    public RewardPoolEntry SelectFromPool(IReadOnlyList<RewardPoolEntry> pool)
    {
        var enabled = pool.Where(e => e.IsEnabled).ToList();
        if (enabled.Count == 0)
            throw new InvalidOperationException("No enabled reward pool entries available.");

        var totalWeight = enabled.Sum(e => e.Weight);
        var roll = _rng.NextDouble() * totalWeight;

        var cumulative = 0.0;
        foreach (var entry in enabled)
        {
            cumulative += entry.Weight;
            if (roll < cumulative)
                return entry;
        }

        return enabled[^1];
    }
}
