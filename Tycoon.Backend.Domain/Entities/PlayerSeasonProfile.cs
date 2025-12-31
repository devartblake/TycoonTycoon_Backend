namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Per-season player state used for rank points, W/L/D, and tier assignment.
    /// This is the canonical row we recompute Tier/TierRank/SeasonRank against.
    /// </summary>
    public sealed class PlayerSeasonProfile
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid SeasonId { get; private set; }
        public Guid PlayerId { get; private set; }

        public int RankPoints { get; private set; }

        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }
        public int MatchesPlayed { get; private set; }

        /// <summary>1..N tiers.</summary>
        public int Tier { get; private set; } = 1;

        /// <summary>1..UsersPerTier (typically 100)</summary>
        public int TierRank { get; private set; } = 0;

        /// <summary>1..N (global order within a season)</summary>
        public int SeasonRank { get; private set; } = 0;

        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerSeasonProfile() { } // EF

        public PlayerSeasonProfile(Guid seasonId, Guid playerId, int initialPoints)
        {
            SeasonId = seasonId;
            PlayerId = playerId;
            RankPoints = Math.Max(0, initialPoints);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyPoints(int delta)
        {
            RankPoints = Math.Max(0, RankPoints + delta);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyMatchOutcome(bool win, bool draw)
        {
            MatchesPlayed++;

            if (draw) Draws++;
            else if (win) Wins++;
            else Losses++;

            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void SetRanks(int tier, int tierRank, int seasonRank)
        {
            Tier = Math.Max(1, tier);
            TierRank = Math.Max(0, tierRank);
            SeasonRank = Math.Max(0, seasonRank);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
