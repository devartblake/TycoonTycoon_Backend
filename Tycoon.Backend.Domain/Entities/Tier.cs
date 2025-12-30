using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Represents a leaderboard tier (Bronze, Silver, Gold, etc.).
    /// Tiers define ranking brackets based on score thresholds.
    /// </summary>
    public sealed class Tier : Entity
    {
        private Tier() { } // EF Core

        public Tier(
            string name,
            int order,
            int minScore,
            int maxScore)
        {
            Name = name;
            Order = order;
            MinScore = minScore;
            MaxScore = maxScore;
        }

        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Visual / progression order (1 = lowest).
        /// </summary>
        public int Order { get; private set; }

        /// <summary>
        /// Inclusive minimum score for this tier.
        /// </summary>
        public int MinScore { get; private set; }

        /// <summary>
        /// Inclusive maximum score for this tier.
        /// </summary>
        public int MaxScore { get; private set; }

        public bool ContainsScore(int score)
            => score >= MinScore && score <= MaxScore;
    }
}
