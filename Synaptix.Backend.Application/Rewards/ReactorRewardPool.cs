namespace Synaptix.Backend.Application.Rewards;

public static class ReactorRewardPool
{
    public static readonly IReadOnlyList<RewardPoolEntry> Entries =
    [
        new(
            RewardId: "xp_boost_small",
            DisplayName: "Small XP Boost",
            Lines: [new("xp", 100)],
            Animation: new("three_reel_reactor", ["xp_small", "xp_small", "xp_small"], [0, 1, 2], "common", "low"),
            Weight: 35.0
        ),
        new(
            RewardId: "coins_small",
            DisplayName: "Coins",
            Lines: [new("coins", 100)],
            Animation: new("three_reel_reactor", ["syncoins", "syncoins", "syncoins"], [0, 1, 2], "common", "medium"),
            Weight: 30.0
        ),
        new(
            RewardId: "xp_boost_large",
            DisplayName: "XP Surge",
            Lines: [new("xp", 250)],
            Animation: new("three_reel_reactor", ["xp_large", "xp_large", "xp_large"], [0, 1, 2], "rare", "medium"),
            Weight: 15.0
        ),
        new(
            RewardId: "coins_medium",
            DisplayName: "Coin Chest",
            Lines: [new("coins", 250)],
            Animation: new("three_reel_reactor", ["syncoins_gold", "syncoins_gold", "syncoins_gold"], [0, 1, 2], "rare", "high"),
            Weight: 12.0
        ),
        new(
            RewardId: "diamonds_small",
            DisplayName: "Diamonds",
            Lines: [new("diamonds", 2)],
            Animation: new("three_reel_reactor", ["diamond", "diamond", "diamond"], [0, 1, 2], "epic", "high"),
            Weight: 6.0
        ),
        new(
            RewardId: "xp_coins_combo",
            DisplayName: "2x XP Multiplier",
            Lines: [new("xp", 250), new("coins", 50)],
            Animation: new("three_reel_reactor", ["xp_multiplier", "syncoins", "xp_vault"], [0, 1, 2], "rare", "high"),
            Weight: 2.0
        )
    ];
}
