namespace Synaptix.Backend.Api.Features.Arcade;

public static class SpinRewardCatalog
{
    private static readonly IReadOnlyList<SpinSegmentDto> Segments =
    [
        new(
            Id: "gold_chest",
            Label: "Gold Chest",
            Color: "#FFD700",
            ImagePath: "assets/images/rewards/gold.png",
            RewardType: "coins",
            Reward: 250,
            IsExclusive: false,
            RequiredStreak: 0,
            RequiredCurrency: 0,
            Description: "Win 250 coins.",
            Probability: 1.0,
            Metadata: [],
            IsEnabled: true,
            EnabledUntil: null
        ),
        new(
            Id: "silver_chest",
            Label: "Silver Chest",
            Color: "#C0C0C0",
            ImagePath: "assets/images/rewards/silver.png",
            RewardType: "coins",
            Reward: 100,
            IsExclusive: false,
            RequiredStreak: 0,
            RequiredCurrency: 0,
            Description: "Win 100 coins.",
            Probability: 1.0,
            Metadata: [],
            IsEnabled: true,
            EnabledUntil: null
        ),
        new(
            Id: "bronze_chest",
            Label: "Bronze Chest",
            Color: "#CD7F32",
            ImagePath: "assets/images/rewards/bronze.png",
            RewardType: "coins",
            Reward: 50,
            IsExclusive: false,
            RequiredStreak: 0,
            RequiredCurrency: 0,
            Description: "Win 50 coins.",
            Probability: 1.0,
            Metadata: [],
            IsEnabled: true,
            EnabledUntil: null
        ),
        new(
            Id: "exclusive_diamond",
            Label: "Exclusive Diamond",
            Color: "#B9F2FF",
            ImagePath: "assets/images/rewards/diamond.png",
            RewardType: "coins",
            Reward: 500,
            IsExclusive: true,
            RequiredStreak: 3,
            RequiredCurrency: 100,
            Description: "Exclusive high-value reward.",
            Probability: 0.25,
            Metadata: new Dictionary<string, object>
            {
                ["requiredStreak"] = 3,
                ["requiredCurrency"] = 100
            },
            IsEnabled: true,
            EnabledUntil: null
        )
    ];

    public static IReadOnlyList<SpinSegmentDto> GetEnabledSegments()
    {
        var now = DateTimeOffset.UtcNow;
        return Segments
            .Where(s => s.IsEnabled && (s.EnabledUntil is null || s.EnabledUntil > now))
            .ToList();
    }

    public static SpinSegmentDto? Find(string segmentId) =>
        Segments.FirstOrDefault(s => string.Equals(s.Id, segmentId, StringComparison.OrdinalIgnoreCase));
}
