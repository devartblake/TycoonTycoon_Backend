namespace Synaptix.Shared.Contracts.Dtos
{
    /// <summary>
    /// A single entry in the frontend asset manifest returned by GET /v1/assets/manifest.
    /// </summary>
    public sealed record AssetManifestEntry(
        string Id,
        string Type,
        string Name,
        string Key,
        string? ThumbnailKey,
        string Version,
        string? Sha256);

    /// <summary>
    /// Full manifest response from GET /v1/assets/manifest.
    /// </summary>
    public sealed record AssetManifestResponse(
        DateTimeOffset GeneratedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        IReadOnlyList<AssetManifestItemDto> Assets);

    /// <summary>
    /// A single asset in the manifest response, with presigned download URLs populated.
    /// </summary>
    public sealed record AssetManifestItemDto(
        string Id,
        string Type,
        string Name,
        string Version,
        string? Sha256,
        string DownloadUrl,
        string? ThumbnailUrl);
}
