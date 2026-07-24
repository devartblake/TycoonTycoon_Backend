namespace Synaptix.Backend.Domain.Entities
{
    public enum PaymentCheckoutStatus
    {
        Created = 1,
        Captured = 2,
        Failed = 3,
        Expired = 4,
        Refunded = 5
    }

    /// <summary>
    /// Tracks a checkout session/order from creation through resolution so reconciliation
    /// has a local record to compare against the provider — without this, a checkout that
    /// the provider captures but that never produces a PlayerTransaction (crashed request,
    /// dropped webhook) is invisible until a player complains.
    /// </summary>
    public sealed class PaymentCheckoutAttempt
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string Provider { get; private set; } = string.Empty; // "paypal" | "stripe"
        public string Sku { get; private set; } = string.Empty;
        public int Quantity { get; private set; }
        public decimal ExpectedAmount { get; private set; }
        public string Currency { get; private set; } = string.Empty;
        public string ProviderRef { get; private set; } = string.Empty; // PayPal order id / Stripe session id
        public string? ProviderCaptureRef { get; private set; } // PayPal capture id / Stripe payment intent id
        public PaymentCheckoutStatus Status { get; private set; } = PaymentCheckoutStatus.Created;
        public Guid? PlayerTransactionId { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ResolvedAtUtc { get; private set; }

        private PaymentCheckoutAttempt() { } // EF

        public PaymentCheckoutAttempt(
            Guid playerId,
            string provider,
            string sku,
            int quantity,
            decimal expectedAmount,
            string currency,
            string providerRef)
        {
            PlayerId = playerId;
            Provider = provider;
            Sku = sku;
            Quantity = quantity;
            ExpectedAmount = expectedAmount;
            Currency = currency;
            ProviderRef = providerRef;
        }

        public void MarkCaptured(Guid playerTransactionId, string? providerCaptureRef)
        {
            Status = PaymentCheckoutStatus.Captured;
            PlayerTransactionId = playerTransactionId;
            ProviderCaptureRef = providerCaptureRef ?? ProviderCaptureRef;
            ResolvedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkExpired()
        {
            Status = PaymentCheckoutStatus.Expired;
            ResolvedAtUtc = DateTimeOffset.UtcNow;
        }

        public void MarkRefunded()
        {
            Status = PaymentCheckoutStatus.Refunded;
            ResolvedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
