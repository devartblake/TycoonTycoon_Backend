using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Progression;

/// <summary>
/// Shared tier-progression definitions and helpers.
/// Used by ProgressionEndpoints (direct XP awards) and QuestionsEndpoints
/// (server-authoritative quiz XP awarding) so both paths advance players
/// through the same tier ladder.
/// </summary>
public static class TierProgression
{
    /// <summary>
    /// Tier definitions based on XP thresholds.
    /// </summary>
    public static readonly TierDefinition[] TierDefinitions =
    [
        new("bronze-rookie", "Bronze Rookie", 1, 0, 500,
            "bronze_rookie",
            new("welcome_badge", 100, 0)),

        new("silver-scholar", "Silver Scholar", 2, 500, 1200,
            "silver_scholar",
            new("scholar_badge", 250, 5)),

        new("gold-master", "Gold Master", 3, 1200, 2500,
            "gold_master",
            new("master_badge", 500, 15)),

        new("platinum-elite", "Platinum Elite", 4, 2500, 5000,
            "platinum_elite",
            new("elite_badge", 1000, 30)),

        new("diamond-legend", "Diamond Legend", 5, 5000, 10000,
            "diamond_legend",
            new("legend_badge", 2000, 50)),

        new("master-sage", "Master Sage", 6, 10000, 20000,
            "master_sage",
            new("sage_badge", 3000, 75)),

        new("celestial-ascendant", "Celestial Ascendant", 7, 20000, int.MaxValue,
            "celestial_ascendant",
            new("ascendant_badge", 5000, 100)),
    ];

    /// <summary>
    /// Find the tier that contains this XP value.
    /// </summary>
    public static TierDefinition GetTierForXp(double xp)
    {
        foreach (var tier in TierDefinitions)
        {
            if (xp >= tier.MinXp && xp < tier.MaxXp)
                return tier;
        }
        // Return highest tier if XP exceeds all ranges
        return TierDefinitions[^1];
    }

    /// <summary>
    /// Get next tier after current.
    /// </summary>
    public static TierDefinition? GetNextTier(TierDefinition current)
    {
        var currentIndex = System.Array.FindIndex(TierDefinitions, t => t.Id == current.Id);
        return currentIndex >= 0 && currentIndex < TierDefinitions.Length - 1
            ? TierDefinitions[currentIndex + 1]
            : null;
    }

    /// <summary>
    /// Base XP for a correct answer at the given difficulty.
    /// Mirrors the mobile client's display formula (difficulty × 10:
    /// Easy 10, Medium 20, Hard 30, Expert 40) so the server-authoritative
    /// award matches what gameplay shows.
    /// </summary>
    public static double XpForCorrectAnswer(QuestionDifficulty difficulty)
        => (int)difficulty * 10.0;
}
