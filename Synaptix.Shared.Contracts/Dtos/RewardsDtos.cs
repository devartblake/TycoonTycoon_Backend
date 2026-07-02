namespace Synaptix.Shared.Contracts.Dtos;

public record DailyRewardConfig(
    string RewardType,
    int CoinsAmount,
    string DisplayName,
    string IconName);

public record DailyClaimResponse(
    bool Success,
    int CoinsGranted,
    int NewBalance,
    string Message,
    DateTimeOffset? NextClaimAt);

public record WeeklyRewardDay(
    int Day,
    string RewardType,
    int CoinsAmount,
    int GemsAmount,
    string DisplayLabel);

public record WeeklyStreakData(
    int CurrentDay,
    string CycleStart,
    IReadOnlyList<int> ClaimedDays,
    IReadOnlyList<WeeklyRewardDay> Schedule);

public record WeeklyClaimResponse(
    bool Success,
    int Day,
    int CoinsGranted,
    int GemsGranted,
    int NewBalance,
    string Message,
    WeeklyStreakData UpdatedStreak);

public record RewardStep(
    string RewardType,
    int PointValue,
    int Quantity,
    string Description);

// Progression/Tier DTOs
public record TierReward(
    string? Badge,
    int CoinsBonus,
    int GemsBonus);

public record TierDefinition(
    string Id,
    string Name,
    int Level,
    int MinXp,
    int MaxXp,
    string IconName,
    TierReward Rewards);

public record PlayerTierProgress(
    string CurrentTierId,
    string CurrentTierName,
    int CurrentLevel,
    double CurrentXp,
    double XpInCurrentTier,
    double XpNeededForNextTier,
    double ProgressPercentage);

public record XpAwardResult(
    double XpAwarded,
    double TotalXp,
    int NewLevel,
    bool TierUpgraded,
    string? NewTierId = null);
