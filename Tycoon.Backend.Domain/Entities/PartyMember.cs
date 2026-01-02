namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PartyMember
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PartyId { get; private set; }
        public Guid PlayerId { get; private set; }

        public DateTimeOffset JoinedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PartyMember() { } // EF

        public PartyMember(Guid partyId, Guid playerId)
        {
            PartyId = partyId;
            PlayerId = playerId;
            JoinedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
