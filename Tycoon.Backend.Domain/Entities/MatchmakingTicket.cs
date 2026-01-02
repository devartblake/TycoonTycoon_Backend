namespace Tycoon.Backend.Domain.Entities
{
    public sealed class MatchmakingTicket
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
        public Guid PlayerId { get; private set; }
        public string Mode { get; private set; } = "duel";
        public int Tier { get; private set; }
        public string Scope { get; private set; } = "Global"; // Global | TierOnly | Practice | Shadow
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset ExpiresAtUtc { get; private set; }
        public string Status { get; private set; } = "Queued"; // Queued | Matched | Cancelled

        private MatchmakingTicket() { }
        public MatchmakingTicket(Guid playerId, string mode, int tier, string scope, TimeSpan ttl)
        {
            PlayerId = playerId;
            Mode = mode;
            Tier = tier;
            Scope = scope;
            ExpiresAtUtc = DateTimeOffset.UtcNow.Add(ttl);
        }

        public void MarkMatched() => Status = "Matched";
        public void Cancel() => Status = "Cancelled";
    }
}
