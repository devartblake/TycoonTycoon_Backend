namespace Synaptix.MigrationService.Seeding.SeedModels;

public sealed class AssetCatalogSeedModel
{
    public string Id { get; set; } = "";
    // "avatar" | "gear" | "environment" | "effect" | "ui"
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    // MinIO object key, e.g. "models/environments/arena_default.glb"
    public string Key { get; set; } = "";
    // Optional MinIO key for a thumbnail image
    public string? ThumbnailKey { get; set; }
    public string Version { get; set; } = "1.0.0";
    // Optional SHA-256 hex digest — lets the frontend detect stale cached copies
    public string? Sha256 { get; set; }
}
