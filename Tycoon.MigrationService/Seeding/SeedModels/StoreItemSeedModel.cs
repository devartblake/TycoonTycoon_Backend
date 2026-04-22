namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed record StoreItemSeedModel(
    string Sku,
    string Name,
    string? Description,
    string ItemType,
    int PriceCoins,
    int PriceDiamonds,
    int GrantQuantity,
    int MaxPerPlayer,
    bool IsActive,
    int SortOrder,
    string? MediaKey,
    string? ThumbnailUrl,
    bool IsFeatured,
    string? Version
);
