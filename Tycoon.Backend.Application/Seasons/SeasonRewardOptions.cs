namespace Tycoon.Backend.Application.Seasons;

public sealed record SeasonRewardTierRule(
    int Tier,
    int MaxTierRank,
    int RewardXp,
    int RewardCoins
);

public sealed class SeasonRewardOptions
{
    public List<SeasonRewardTierRule> Rules { get; init; } = new();
}
