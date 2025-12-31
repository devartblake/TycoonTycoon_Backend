using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class EconomyTransaction
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }
        public Guid PlayerId { get; private set; }
        public string Kind { get; private set; } = string.Empty;
        public string? Note { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public List<EconomyTransactionLine> Lines { get; private set; } = new();

        private EconomyTransaction() { } // EF

        public EconomyTransaction(Guid eventId, Guid playerId, string kind, string? note)
        {
            EventId = eventId;
            PlayerId = playerId;
            Kind = kind.Trim();
            Note = note;
        }

        public void SetLines(IEnumerable<EconomyLineDto> lines)
        {
            Lines = lines.Select(l => new EconomyTransactionLine(Id, l.Currency, l.Delta)).ToList();
        }
    }

    public sealed class EconomyTransactionLine
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid EconomyTransactionId { get; private set; }

        public CurrencyType Currency { get; private set; }
        public int Delta { get; private set; }

        private EconomyTransactionLine() { } // EF

        public EconomyTransactionLine(Guid txnId, CurrencyType currency, int delta)
        {
            EconomyTransactionId = txnId;
            Currency = currency;
            Delta = delta;
        }
    }
}
