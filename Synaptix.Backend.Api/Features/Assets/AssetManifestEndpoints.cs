using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Assets;

public static class AssetManifestEndpoints
{
    private const string ManifestKey = "frontend/assets/manifest.json";
    private static readonly TimeSpan UrlExpiry = TimeSpan.FromMinutes(10);
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/assets").WithTags("Assets");

        // Public — no auth required; presigned URLs give time-limited access.
        g.MapGet("/manifest", GetManifest);
    }

    private static async Task<IResult> GetManifest(
        IObjectStorage storage,
        CancellationToken ct)
    {
        await using var stream = await storage.GetAsync(ManifestKey, ct);
        if (stream is null)
            return Results.NotFound(new { code = "MANIFEST_NOT_FOUND", message = "Asset manifest has not been seeded yet." });

        List<AssetManifestEntry>? entries;
        try
        {
            entries = await JsonSerializer.DeserializeAsync<List<AssetManifestEntry>>(stream, JsonOpts, ct);
        }
        catch
        {
            return Results.Problem("Asset manifest could not be parsed.", statusCode: StatusCodes.Status500InternalServerError);
        }

        if (entries is null or { Count: 0 })
            return Results.Ok(new AssetManifestResponse(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(UrlExpiry), []));

        var expiresAt = DateTimeOffset.UtcNow.Add(UrlExpiry);

        AssetManifestItemDto[] items;
        if (storage is IPresignedStorage presigned)
        {
            items = await Task.WhenAll(entries.Select(async e => new AssetManifestItemDto(
                e.Id, e.Type, e.Name, e.Version, e.Sha256,
                DownloadUrl: await presigned.GetPresignedGetUrlAsync(e.Key, UrlExpiry, ct),
                ThumbnailUrl: e.ThumbnailKey is not null
                    ? await presigned.GetPresignedGetUrlAsync(e.ThumbnailKey, UrlExpiry, ct)
                    : null)));
        }
        else
        {
            // Fallback (local storage): use public URLs.
            items = entries.Select(e => new AssetManifestItemDto(
                e.Id, e.Type, e.Name, e.Version, e.Sha256,
                DownloadUrl: storage.GetPublicUrl(e.Key),
                ThumbnailUrl: e.ThumbnailKey is not null ? storage.GetPublicUrl(e.ThumbnailKey) : null)).ToArray();
        }

        return Results.Ok(new AssetManifestResponse(DateTimeOffset.UtcNow, expiresAt, items));
    }
}
