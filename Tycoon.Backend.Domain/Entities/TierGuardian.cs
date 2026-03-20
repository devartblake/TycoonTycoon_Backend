using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class TierGuardian : Entity
    {
        public Guid SeasonId { get; private set; }
        public int TierNumber { get; private set; }
        public Guid PlayerId { get; private set; }
        public DateTimeOffset AssignedAtUtc { get; private set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public int PassiveCoins { get; set; }
        public int PassiveXp { get; set; }
        public int DefencesWon { get; set; }
        public int DefencesLost { get; set; }

        private TierGuardian() { }

        public TierGuardian(Guid seasonId, int tierNumber, Guid playerId, DateTimeOffset expiresAtUtc)
        {
            SeasonId = seasonId;
            TierNumber = tierNumber;
            PlayerId = playerId;
            AssignedAtUtc = DateTimeOffset.UtcNow;
            ExpiresAtUtc = expiresAtUtc;
        }
    }
}
