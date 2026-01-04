namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Durable link between a Party and the Match it queued into.
    /// Enables lifecycle management (auto-close, auditing, analytics).
    /// </summary>
    public sealed class PartyMatchLink
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PartyId { get; private set; }
        public Guid MatchId { get; private set; }

        // Queued | Matched | Closed
        public string Status { get; private set; } = "Matched";

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ClosedAtUtc { get; private set; }

        private PartyMatchLink() { } // EF

        public PartyMatchLink(Guid partyId, Guid matchId)
        {
            PartyId = partyId;
            MatchId = matchId;
            Status = "Matched";
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkClosed()
        {
            if (Status == "Closed") return;
            Status = "Closed";
            ClosedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
