namespace Synaptix.Entitlements.Entities;

public sealed class PlayerEntitlement
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ItemType { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public string Scope { get; private set; } = "permanent"; // permanent | consumable | seasonal
    public Guid SourceTransactionId { get; private set; }
    public DateTimeOffset GrantedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    private PlayerEntitlement() { }

    public static PlayerEntitlement Grant(
        Guid playerId,
        string sku,
        string itemType,
        int quantity,
        Guid sourceTransactionId,
        string scope = "permanent",
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return new PlayerEntitlement
        {
            PlayerId = playerId,
            Sku = sku,
            ItemType = itemType,
            Quantity = quantity,
            SourceTransactionId = sourceTransactionId,
            Scope = scope,
            ExpiresAtUtc = expiresAt
        };
    }

    public void Consume(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (amount > Quantity)
            throw new InvalidOperationException($"Cannot consume {amount} of {Sku}: only {Quantity} available.");
        Quantity -= amount;
    }
}
