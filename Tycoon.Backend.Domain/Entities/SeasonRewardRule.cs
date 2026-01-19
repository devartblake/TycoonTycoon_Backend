public sealed class SeasonRewardRule
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Tier { get; private set; }
    public int MaxTierRank { get; private set; }

    public int RewardXp { get; private set; }
    public int RewardCoins { get; private set; }

    private SeasonRewardRule() { }

    public SeasonRewardRule(int tier, int maxTierRank, int xp, int coins)
    {
        Tier = tier;
        MaxTierRank = maxTierRank;
        RewardXp = xp;
        RewardCoins = coins;
    }
}
