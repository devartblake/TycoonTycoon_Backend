using Synaptix.Backend.Domain.Primitives;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class TerritoryDuel : Entity
    {
        public Guid? GameEventId { get; private set; }
        public Guid SeasonId { get; private set; }
        public int TierNumber { get; private set; }
        public string Category { get; private set; } = string.Empty;
        public Guid ChallengerId { get; private set; }
        public Guid? DefenderId { get; private set; }
        public Guid MatchId { get; private set; }
        public TerritoryDuelOutcome? Outcome { get; set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset? ResolvedAtUtc { get; set; }

        private TerritoryDuel() { }

        public TerritoryDuel(
            Guid seasonId,
            int tierNumber,
            string category,
            Guid challengerId,
            Guid? defenderId,
            Guid matchId,
            Guid? gameEventId = null)
        {
            SeasonId = seasonId;
            TierNumber = tierNumber;
            Category = category;
            ChallengerId = challengerId;
            DefenderId = defenderId;
            MatchId = matchId;
            GameEventId = gameEventId;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
