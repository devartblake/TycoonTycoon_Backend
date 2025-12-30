namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Records gameplay events that have already been applied to mission progress.
    /// Prevents double-counting when clients retry (offline sync, network retries, etc.).
    /// </summary>
    public sealed class ProcessedGameplayEvent
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }
        public Guid PlayerId { get; private set; }
        public string Kind { get; private set; } = "";

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private ProcessedGameplayEvent() { } // EF

        public ProcessedGameplayEvent(Guid eventId, Guid playerId, string kind)
        {
            EventId = eventId;
            PlayerId = playerId;
            Kind = kind;
        }
    }
}
