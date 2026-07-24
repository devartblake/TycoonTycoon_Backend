namespace Synaptix.Backend.Domain.Entities
{
    public enum PaymentReconciliationCategory
    {
        ProviderCapturedFulfillmentMissing = 1,
        AmountMismatch = 2,
        CurrencyMismatch = 3
    }

    /// <summary>
    /// A persisted, operator-actionable finding from the daily payment reconciliation job.
    /// Kept separate from PlayerTransaction so resolution state (who looked at it, when,
    /// what they did) doesn't get mixed into the immutable purchase ledger.
    /// </summary>
    public sealed class PaymentReconciliationIssue
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public PaymentReconciliationCategory Category { get; private set; }
        public string Provider { get; private set; } = string.Empty;
        public string ProviderRef { get; private set; } = string.Empty;
        public Guid? PaymentCheckoutAttemptId { get; private set; }
        public Guid? PlayerId { get; private set; }
        public decimal? ExpectedAmount { get; private set; }
        public decimal? ActualAmount { get; private set; }
        public string Details { get; private set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ResolvedAtUtc { get; private set; }
        public string? ResolvedBy { get; private set; }
        public string? ResolutionNotes { get; private set; }

        private PaymentReconciliationIssue() { } // EF

        public PaymentReconciliationIssue(
            PaymentReconciliationCategory category,
            string provider,
            string providerRef,
            Guid? paymentCheckoutAttemptId,
            Guid? playerId,
            decimal? expectedAmount,
            decimal? actualAmount,
            string details)
        {
            Category = category;
            Provider = provider;
            ProviderRef = providerRef;
            PaymentCheckoutAttemptId = paymentCheckoutAttemptId;
            PlayerId = playerId;
            ExpectedAmount = expectedAmount;
            ActualAmount = actualAmount;
            Details = details;
        }

        public void Resolve(string resolvedBy, string? notes)
        {
            ResolvedAtUtc = DateTimeOffset.UtcNow;
            ResolvedBy = resolvedBy;
            ResolutionNotes = notes;
        }
    }
}
