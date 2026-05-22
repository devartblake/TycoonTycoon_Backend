namespace Tycoon.Backend.Domain.Entities
{
    public sealed class StoreStockPolicy
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>Links to StoreItem.Sku — one policy per SKU.</summary>
        public string Sku { get; private set; } = string.Empty;

        /// <summary>Max units a player may buy per reset interval. 0 = unlimited.</summary>
        public int MaxQuantityPerUser { get; private set; }

        /// <summary>"daily", "weekly", "monthly", or "none" (no automatic reset).</summary>
        public string ResetInterval { get; private set; } = "daily";

        public bool IsActive { get; private set; } = true;
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private StoreStockPolicy() { }

        public StoreStockPolicy(string sku, int maxQuantityPerUser, string resetInterval = "daily")
        {
            Sku = sku;
            MaxQuantityPerUser = maxQuantityPerUser;
            ResetInterval = resetInterval;
        }

        public void Update(int maxQuantityPerUser, string resetInterval, bool? isActive = null)
        {
            MaxQuantityPerUser = maxQuantityPerUser;
            ResetInterval = resetInterval;
            if (isActive.HasValue) IsActive = isActive.Value;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset CalculateNextReset(DateTimeOffset from)
        {
            var todayUtc = from.UtcDateTime.Date;
            return ResetInterval switch
            {
                "weekly"  => new DateTimeOffset(todayUtc.AddDays(7), TimeSpan.Zero),
                "monthly" => new DateTimeOffset(todayUtc.AddMonths(1), TimeSpan.Zero),
                _         => new DateTimeOffset(todayUtc.AddDays(1), TimeSpan.Zero),
            };
        }
    }
}
