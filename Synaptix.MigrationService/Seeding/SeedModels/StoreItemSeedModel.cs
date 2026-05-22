namespace Synaptix.MigrationService.Seeding.SeedModels;

public sealed class StoreItemSeedModel
{
    public string? Sku { get; set; }
    public string? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ItemType { get; set; }
    public string? Category { get; set; }
    public decimal? Price { get; set; }
    public string? Currency { get; set; }
    public int PriceCoins { get; set; }
    public int PriceDiamonds { get; set; }
    public int GrantQuantity { get; set; }
    public int Amount { get; set; }
    public int Quantity { get; set; }
    public int MaxPerPlayer { get; set; }
    public bool IsActive { get; set; } = true;
    public bool? Active { get; set; }
    public int SortOrder { get; set; }
    public string? MediaKey { get; set; }
    public string? IconPath { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; }
    public string? Version { get; set; }
}
