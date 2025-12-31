using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Media;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminMedia
{
    public static class AdminMediaEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/admin/media").WithTags("Admin/Media");

            g.MapPost("/intent", ([FromBody] CreateUploadIntentRequest req, MediaService media) =>
            {
                var dto = media.CreateUploadIntent(req);
                return Results.Ok(dto);
            });

            // Local upload endpoint (simple dev default). Replace with object storage later.
            g.MapPost("/upload/{*assetKey}", async ([FromRoute] string assetKey, IFormFile file) =>
            {
                var root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
                var fullPath = Path.Combine(root, assetKey.Replace('/', Path.DirectorySeparatorChar));

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

                await using var fs = File.Create(fullPath);
                await file.CopyToAsync(fs);

                return Results.Ok(new { assetKey, url = $"/{assetKey}" });
            });
        }
    }
}
