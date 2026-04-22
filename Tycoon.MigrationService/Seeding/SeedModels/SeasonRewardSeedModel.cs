namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed record SeasonRewardSeedModel(
    int Tier,
    int MaxTierRank,
    int RewardXp,
    int RewardCoins
);
