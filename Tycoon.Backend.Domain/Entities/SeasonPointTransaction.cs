namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Idempotent ledger for season rank points (separate from Economy coins/xp).
    /// EventId must be unique to prevent double-apply on retries.
    /// </summary>
    public sealed class SeasonPointTransaction
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }     // idempotency key
        public Guid SeasonId { get; private set; }
        public Guid PlayerId { get; private set; }

        public string Kind { get; private set; } = string.Empty; // "match-result" | "admin-adjust" | etc.
        public int Delta { get; private set; }
        public string? Note { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private SeasonPointTransaction() { } // EF

        public SeasonPointTransaction(Guid eventId, Guid seasonId, Guid playerId, string kind, int delta, string? note)
        {
            EventId = eventId;
            SeasonId = seasonId;
            PlayerId = playerId;
            Kind = (kind ?? "").Trim();
            Delta = delta;
            Note = note;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
