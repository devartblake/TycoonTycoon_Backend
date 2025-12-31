using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminAntiCheat
{
    public static class AdminAntiCheatEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/anti-cheat").WithTags("Admin/AntiCheat");

            g.MapGet("/flags", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] int? severity,
                [FromQuery] Guid? playerId,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.AntiCheatFlags.AsNoTracking();

                if (severity.HasValue) q = q.Where(x => (int)x.Severity == severity.Value);
                if (playerId.HasValue) q = q.Where(x => x.PlayerId == playerId.Value);

                q = q.OrderByDescending(x => x.CreatedAtUtc);

                var total = await q.CountAsync(ct);

                var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new AntiCheatFlagDto(
                        x.Id,
                        x.MatchId,
                        x.PlayerId,
                        x.RuleKey,
                        (int)x.Severity,
                        (int)x.Action,
                        x.Message,
                        x.CreatedAtUtc
                    ))
                    .ToListAsync(ct);

                return Results.Ok(new AntiCheatFlagListResponseDto(page, pageSize, total, items));
            });
        }
    }
}
