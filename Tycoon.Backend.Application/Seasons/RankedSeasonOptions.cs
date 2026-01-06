namespace Tycoon.Backend.Application.Seasons;

public sealed record RankedSeasonOptions
{
    public int UsersPerTier { get; init; } = 100;

    // Placement
    public int PlacementMatchesRequired { get; init; } = 5;
    public int PlacementWinPoints { get; init; } = 25;
    public int PlacementLossPoints { get; init; } = 10;
    public int PlacementDrawPoints { get; init; } = 18;

    // Normal ranked
    public int RankedWinBase { get; init; } = 30;
    public int RankedLossBase { get; init; } = 5;
    public int RankedDrawBase { get; init; } = 15;

    // Performance add-on
    public int CorrectDivisor { get; init; } = 2; // + correct/2

    // Promotion thresholds (within a tier)
    public int PromotionEligibleRank { get; init; } = 25;
    public int DailyRewardRank { get; init; } = 20;

    // Integrity / cooldowns
    public int PromotionCooldownDays { get; init; } = 2;
    public int DemotionCooldownDays { get; init; } = 2;
}
