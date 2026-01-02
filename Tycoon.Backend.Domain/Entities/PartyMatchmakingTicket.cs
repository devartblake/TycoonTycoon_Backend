namespace Tycoon.Backend.Domain.Entities
{
    public sealed class PartyMatchmakingTicket
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid PartyId { get; private set; }
        public Guid LeaderPlayerId { get; private set; }

        public string Mode { get; private set; } = "ranked";
        public int Tier { get; private set; } = 1;

        // Global | TierOnly | Practice | Shadow
        public string Scope { get; private set; } = "Global";

        public int PartySize { get; private set; } = 1;

        // Queued | Matched | Cancelled
        public string Status { get; private set; } = "Queued";

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAtUtc { get; private set; }

        // Optimistic concurrency (mirrors how you treat other concurrency hardening)
        public long RowVersion { get; private set; }

        private PartyMatchmakingTicket() { } // EF

        public PartyMatchmakingTicket(
            Guid partyId,
            Guid leaderPlayerId,
            string mode,
            int tier,
            string scope,
            int partySize,
            TimeSpan ttl)
        {
            PartyId = partyId;
            LeaderPlayerId = leaderPlayerId;
            Mode = string.IsNullOrWhiteSpace(mode) ? "ranked" : mode.Trim();
            Tier = tier <= 0 ? 1 : tier;
            Scope = string.IsNullOrWhiteSpace(scope) ? "Global" : scope.Trim();
            PartySize = partySize <= 0 ? 1 : partySize;
            Status = "Queued";
            CreatedAtUtc = DateTimeOffset.UtcNow;
            ExpiresAtUtc = CreatedAtUtc.Add(ttl);
        }

        public void MarkMatched()
        {
            if (Status != "Queued") return;
            Status = "Matched";
        }

        public void Cancel()
        {
            if (Status != "Queued") return;
            Status = "Cancelled";
        }
    }
}
