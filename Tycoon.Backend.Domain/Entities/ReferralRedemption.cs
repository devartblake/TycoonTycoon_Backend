namespace Tycoon.Backend.Domain.Entities
{
    public sealed class ReferralRedemption
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }
        public Guid ReferralCodeId { get; private set; }

        public Guid OwnerPlayerId { get; private set; }
        public Guid RedeemerPlayerId { get; private set; }

        public int AwardXpToOwner { get; private set; }
        public int AwardCoinsToOwner { get; private set; }
        public int AwardXpToRedeemer { get; private set; }
        public int AwardCoinsToRedeemer { get; private set; }

        public DateTimeOffset RedeemedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private ReferralRedemption() { } // EF

        public ReferralRedemption(
            Guid eventId,
            Guid referralCodeId,
            Guid ownerPlayerId,
            Guid redeemerPlayerId,
            int awardXpToOwner,
            int awardCoinsToOwner,
            int awardXpToRedeemer,
            int awardCoinsToRedeemer)
        {
            EventId = eventId;
            ReferralCodeId = referralCodeId;
            OwnerPlayerId = ownerPlayerId;
            RedeemerPlayerId = redeemerPlayerId;
            AwardXpToOwner = awardXpToOwner;
            AwardCoinsToOwner = awardCoinsToOwner;
            AwardXpToRedeemer = awardXpToRedeemer;
            AwardCoinsToRedeemer = awardCoinsToRedeemer;
        }
    }
}
