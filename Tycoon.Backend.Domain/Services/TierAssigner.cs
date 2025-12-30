using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Domain.Services
{
    /// <summary>
    /// Determines which Tier a player belongs to based on score thresholds.
    /// Pure domain: it doesn't load tiers from DB; caller supplies tiers.
    /// </summary>
    public static class TierAssigner
    {
        public static TierAssignmentResult Assign(int score, IReadOnlyList<Tier> tiers)
        {
            if (tiers is null || tiers.Count == 0)
                throw new InvalidOperationException("No tiers configured.");

            // Prefer highest tier that contains the score.
            // If tiers overlap, highest order wins.
            var match = tiers
                .OrderBy(t => t.Order)
                .LastOrDefault(t => t.ContainsScore(score));

            match ??= tiers.OrderBy(t => t.Order).First();

            return new TierAssignmentResult(match.Id, match.Name, match.Order);
        }
    }
}
