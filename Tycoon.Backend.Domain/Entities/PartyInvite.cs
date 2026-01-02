namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PartyInvite
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PartyId { get; private set; }
        public Guid FromPlayerId { get; private set; }
        public Guid ToPlayerId { get; private set; }

        // Pending | Accepted | Declined | Cancelled
        public string Status { get; private set; } = "Pending";

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? RespondedAtUtc { get; private set; }

        private PartyInvite() { } // EF

        public PartyInvite(Guid partyId, Guid fromPlayerId, Guid toPlayerId)
        {
            PartyId = partyId;
            FromPlayerId = fromPlayerId;
            ToPlayerId = toPlayerId;
            Status = "Pending";
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Accept()
        {
            if (Status != "Pending") return;
            Status = "Accepted";
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Decline()
        {
            if (Status != "Pending") return;
            Status = "Declined";
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Cancel()
        {
            if (Status != "Pending") return;
            Status = "Cancelled";
            RespondedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
