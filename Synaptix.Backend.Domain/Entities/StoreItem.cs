namespace Synaptix.Backend.Domain.Entities
{
    /// <summary>
    /// A purchasable item in the Synaptix store catalog.
    /// Prices are defined in Credits (Coins) and/or Synapse Shards (Diamonds).
    /// </summary>
    public sealed class StoreItem
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        /// <summary>Stable SKU identifier (e.g., "powerup:skip", "cosmetic:neon-border").</summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>Display name shown in the store UI.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Short description of the item.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Item type for inventory routing (e.g., "powerup", "cosmetic", "bundle").</summary>
        public string ItemType { get; set; } = string.Empty;

        /// <summary>Price in Credits (Coins). 0 means not purchasable with Credits.</summary>
        public int PriceCoins { get; set; }

        /// <summary>Price in Synapse Shards (Diamonds). 0 means not purchasable with Shards.</summary>
        public int PriceDiamonds { get; set; }

        /// <summary>Quantity granted per purchase (e.g., 1 for a single powerup, 5 for a bundle).</summary>
        public int GrantQuantity { get; set; } = 1;

        /// <summary>Max times a single player can purchase this item (0 = unlimited).</summary>
        public int MaxPerPlayer { get; set; }

        /// <summary>Whether this item is currently visible and purchasable.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Sort order for catalog display.</summary>
        public int SortOrder { get; set; }

        /// <summary>Optional media/image key for the item.</summary>
        public string? MediaKey { get; set; }

        public string? ThumbnailUrl { get; set; }
        public bool IsFeatured { get; set; }
        public string? Version { get; set; }

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        // Compliance fields (COPPA / consumer protection)
        public bool IsRandomized { get; set; }
        public int AgeMin { get; set; }
        public bool RequiresParentApproval { get; set; }
        public bool IsRefundable { get; set; } = true;
        public ItemKind ItemKind { get; set; } = ItemKind.Consumable;
    }
}
