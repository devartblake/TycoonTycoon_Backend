using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Media;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminMedia
{
    public static class AdminMediaEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/admin/media").WithTags("Admin/Media").WithOpenApi();

            g.MapPost("/intent", ([FromBody] CreateUploadIntentRequest req, MediaService media) =>
            {
                var dto = media.CreateUploadIntent(req);
                return Results.Ok(dto);
            });

            g.MapPost("/upload/{*assetKey}", async ([FromRoute] string assetKey, IFormFile file, IObjectStorage storage, CancellationToken ct) =>
            {
                await using var stream = file.OpenReadStream();
                await storage.PutAsync(assetKey, stream, file.ContentType, file.Length, ct);
                return Results.Ok(new { assetKey, url = storage.GetPublicUrl(assetKey) });
            });
        }
    }
}
