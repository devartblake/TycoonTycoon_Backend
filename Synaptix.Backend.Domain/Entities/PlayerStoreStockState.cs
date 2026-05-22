namespace Synaptix.Backend.Domain.Entities
{
    public sealed class PlayerStoreStockState
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string Sku { get; private set; } = string.Empty;
        public int QuantityUsed { get; private set; }
        public int? EffectiveMaxQuantity { get; private set; }
        public DateTimeOffset? LastResetAtUtc { get; private set; }
        public DateTimeOffset? NextResetAtUtc { get; private set; }
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerStoreStockState() { }

        public static PlayerStoreStockState Create(Guid playerId, string sku, StoreStockPolicy policy)
        {
            var now = DateTimeOffset.UtcNow;
            return new PlayerStoreStockState
            {
                PlayerId = playerId,
                Sku = sku,
                QuantityUsed = 0,
                LastResetAtUtc = now,
                NextResetAtUtc = policy.CalculateNextReset(now),
                UpdatedAtUtc = now
            };
        }

        public void ResetIfExpired(StoreStockPolicy policy)
        {
            if (NextResetAtUtc is null || NextResetAtUtc > DateTimeOffset.UtcNow) return;
            var now = DateTimeOffset.UtcNow;
            QuantityUsed = 0;
            LastResetAtUtc = now;
            NextResetAtUtc = policy.CalculateNextReset(now);
            UpdatedAtUtc = now;
        }

        public void Consume(int quantity)
        {
            QuantityUsed += quantity;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void BulkReset(StoreStockPolicy policy, DateTimeOffset now)
        {
            QuantityUsed = 0;
            LastResetAtUtc = now;
            NextResetAtUtc = policy.ResetInterval == "none" ? null : policy.CalculateNextReset(now);
            UpdatedAtUtc = now;
        }

        public void SetOverride(int? effectiveMaxQuantity)
        {
            EffectiveMaxQuantity = effectiveMaxQuantity;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public int GetRemaining(StoreStockPolicy policy)
        {
            var cap = EffectiveMaxQuantity ?? policy.MaxQuantityPerUser;
            return cap == 0 ? int.MaxValue : Math.Max(0, cap - QuantityUsed);
        }
    }
}
