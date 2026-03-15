using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class TerritoryTile : Entity
    {
        public Guid SeasonId { get; private set; }
        public int TierNumber { get; private set; }
        public string Category { get; private set; } = string.Empty;
        public Guid? OwnerId { get; set; }
        public DateTimeOffset? CapturedAtUtc { get; set; }
        public int XpMultiplierBps { get; set; }

        private TerritoryTile() { }

        public TerritoryTile(Guid seasonId, int tierNumber, string category)
        {
            SeasonId = seasonId;
            TierNumber = tierNumber;
            Category = category;
            XpMultiplierBps = 0;
        }
    }
}
