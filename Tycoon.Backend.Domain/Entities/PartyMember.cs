namespace Tycoon.Backend.Domain.Entities
{
    public enum PartyRole
    {
        Leader = 1,
        Member = 2
    }

    public sealed class PartyMember
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PartyId { get; private set; }
        public Guid PlayerId { get; private set; }

        public PartyRole Role { get; private set; }

        public DateTimeOffset JoinedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PartyMember() { } // EF

        public PartyMember(Guid partyId, Guid playerId)
        {
            PartyId = partyId;
            PlayerId = playerId;
            Role = PartyRole.Member;
            JoinedAtUtc = DateTimeOffset.UtcNow;
        }

        public PartyMember(Guid partyId, Guid playerId, PartyRole role)
        {
            PartyId = partyId;
            PlayerId = playerId;
            Role = role;
            JoinedAtUtc = DateTimeOffset.UtcNow;
        }

        public void PromoteToLeader()
        {
            Role = PartyRole.Leader;
        }

        public void DemoteToMember()
        {
            Role = PartyRole.Member;
        }
    }
}
