namespace Tycoon.Backend.Domain.Entities
{
    public sealed class FlashSale
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Sku { get; private set; } = string.Empty;
        public int DiscountPercent { get; private set; }
        public DateTimeOffset StartsAtUtc { get; private set; }
        public DateTimeOffset EndsAtUtc { get; private set; }
        public bool IsActive { get; private set; } = true;
        public string? Reason { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private FlashSale() { }

        public FlashSale(string sku, int discountPercent, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc, string? reason = null)
        {
            Sku = sku;
            DiscountPercent = discountPercent;
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Reason = reason;
        }

        public bool IsCurrentlyActive =>
            IsActive && StartsAtUtc <= DateTimeOffset.UtcNow && EndsAtUtc >= DateTimeOffset.UtcNow;

        public void Cancel() => IsActive = false;
    }
}
