namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record AvatarCatalogItemDto(
        string Id,
        string Sku,
        string Name,
        string? Description,
        int Price,
        string Currency,
        string Category,
        string Type,
        string? MediaKey,
        string? ThumbnailUrl,
        bool Owned,
        bool IsFeatured,
        string Version
    );

    public sealed record AvatarCatalogDto(IReadOnlyList<AvatarCatalogItemDto> Items);

    public sealed record PurchaseAvatarRequest(Guid PlayerId);

    public sealed record PurchaseAvatarResultDto(
        bool Success,
        string AvatarId,
        int CoinsDeducted,
        int NewBalance
    );

    public sealed record AvatarAssetResponseDto(
        string PresignedUrl,
        string? ThumbnailUrl,
        DateTimeOffset ExpiresAt,
        string? ContentType,
        string ArchiveFormat,
        string? Sha256
    );
}
