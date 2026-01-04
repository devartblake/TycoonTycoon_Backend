namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PartyMatchMember
    {
        public Guid PartyId { get; private set; }
        public Guid MatchId { get; private set; }
        public Guid PlayerId { get; private set; }

        // store role as string for wire compatibility + no shared enum coupling
        public string Role { get; private set; } = "Member";

        public DateTimeOffset CapturedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PartyMatchMember() { } // EF

        public PartyMatchMember(Guid partyId, Guid matchId, Guid playerId, string role)
        {
            PartyId = partyId;
            MatchId = matchId;
            PlayerId = playerId;
            Role = string.IsNullOrWhiteSpace(role) ? "Member" : role;
            CapturedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
