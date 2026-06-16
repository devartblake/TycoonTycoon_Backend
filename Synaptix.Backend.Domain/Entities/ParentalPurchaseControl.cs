namespace Synaptix.Backend.Domain.Entities;

public sealed class ParentalPurchaseControl
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ChildUserId { get; private set; }

    public bool PurchasesEnabled { get; private set; }

    // Monthly spend ceiling in cents (0 = unlimited)
    public int MonthlySpendLimitCents { get; private set; }

    public bool AdsEnabled { get; private set; } = true;

    public bool LootBoxesEnabled { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private ParentalPurchaseControl() { }

    public ParentalPurchaseControl(
        Guid childUserId,
        bool purchasesEnabled,
        int monthlySpendLimitCents,
        bool adsEnabled,
        bool lootBoxesEnabled)
    {
        ChildUserId = childUserId;
        PurchasesEnabled = purchasesEnabled;
        MonthlySpendLimitCents = Math.Max(0, monthlySpendLimitCents);
        AdsEnabled = adsEnabled;
        LootBoxesEnabled = lootBoxesEnabled;
    }

    public void Update(bool purchasesEnabled, int monthlySpendLimitCents, bool adsEnabled, bool lootBoxesEnabled)
    {
        PurchasesEnabled = purchasesEnabled;
        MonthlySpendLimitCents = Math.Max(0, monthlySpendLimitCents);
        AdsEnabled = adsEnabled;
        LootBoxesEnabled = lootBoxesEnabled;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
