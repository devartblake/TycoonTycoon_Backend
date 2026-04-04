namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record StoreItemDto(
        Guid Id,
        string Sku,
        string Name,
        string Description,
        string ItemType,
        int PriceCoins,
        int PriceDiamonds,
        int GrantQuantity,
        int MaxPerPlayer,
        string? MediaKey,
        int SortOrder);

    public sealed record StoreCatalogDto(
        IReadOnlyList<StoreItemDto> Items,
        int Count);

    public sealed record StorePurchaseRequest(
        Guid PlayerId,
        string Sku,
        int Quantity,
        string Currency);

    public sealed record StorePurchaseResultDto(
        string Status,
        Guid? TransactionId,
        int BalanceXp,
        int BalanceCoins,
        int BalanceDiamonds,
        string? ErrorMessage);

    public sealed record PlayerInventoryItemDto(
        string ItemType,
        int Quantity);

    public sealed record PlayerInventoryDto(
        Guid PlayerId,
        IReadOnlyList<PlayerInventoryItemDto> Items,
        int Count);
}
