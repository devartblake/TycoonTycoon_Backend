using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class QrScanEvent
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }
        public Guid PlayerId { get; private set; }

        public string Value { get; private set; } = string.Empty;
        public DateTimeOffset OccurredAtUtc { get; private set; }

        public QrScanType Type { get; private set; }

        public DateTimeOffset StoredAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private QrScanEvent() { } // EF

        public QrScanEvent(Guid eventId, Guid playerId, string value, DateTimeOffset occurredAtUtc, QrScanType type)
        {
            EventId = eventId;
            PlayerId = playerId;
            Value = value;
            OccurredAtUtc = occurredAtUtc;
            Type = type;
        }
    }
}
