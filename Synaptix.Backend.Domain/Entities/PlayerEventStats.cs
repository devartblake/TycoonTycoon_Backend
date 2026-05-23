using Synaptix.Backend.Domain.Primitives;

namespace Synaptix.Backend.Domain.Entities
{
    /// <summary>
    /// Per-player, per-season materialized stats across all event game modes
    /// (GameEvent, Guardian, Territory). Updated incrementally by handlers.
    /// Never used for ranked-ladder tier assignment — that is PlayerSeasonProfile's job.
    /// </summary>
    public sealed class PlayerEventStats : Entity
    {
        public Guid SeasonId { get; private set; }
        public Guid PlayerId { get; private set; }

        // --- GameEvent aggregate ---
        public int EventsEntered { get; set; }
        public int EventsTop20 { get; set; }
        public int EventsWon { get; set; }
        public int TotalEventXpEarned { get; set; }
        public int TotalEventCoinsEarned { get; set; }
        public int ChampionBattleEliminations { get; set; }

        // --- Guardian aggregate (cross-tier) ---
        public int GuardianPromotions { get; set; }
        public int GuardianDefencesWon { get; set; }
        public int GuardianDefencesLost { get; set; }
        public int GuardianDaysTotal { get; set; }

        // --- Territory aggregate ---
        public int TilesEverCaptured { get; set; }
        public int CurrentTilesOwned { get; set; }
        public int PeakXpMultiplierBps { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        private PlayerEventStats() { } // EF

        public PlayerEventStats(Guid seasonId, Guid playerId)
        {
            SeasonId = seasonId;
            PlayerId = playerId;
        }
    }
}
