using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class GameEventPrizeClaim : Entity
    {
        public Guid GameEventId { get; private set; }
        public Guid PlayerId { get; private set; }
        public Guid EventId { get; private set; }
        public int AwardedXp { get; private set; }
        public int AwardedCoins { get; private set; }
        public int Rank { get; private set; }
        public DateTimeOffset ClaimedAtUtc { get; private set; }

        private GameEventPrizeClaim() { }

        public GameEventPrizeClaim(
            Guid gameEventId,
            Guid playerId,
            Guid eventId,
            int awardedXp,
            int awardedCoins,
            int rank)
        {
            GameEventId = gameEventId;
            PlayerId = playerId;
            EventId = eventId;
            AwardedXp = awardedXp;
            AwardedCoins = awardedCoins;
            Rank = rank;
            ClaimedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
