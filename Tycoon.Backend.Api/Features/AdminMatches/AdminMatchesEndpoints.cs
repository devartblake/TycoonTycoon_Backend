using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminMatches
{
    public static class AdminMatchesEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/matches").WithTags("Admin/Matches").WithOpenApi();

            g.MapGet("", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.MatchResults.AsNoTracking()
                    .OrderByDescending(x => x.EndedAtUtc);

                var total = await q.CountAsync(ct);

                var items = await q.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Join(db.Matches.AsNoTracking(),
                        r => r.MatchId,
                        m => m.Id,
                        (r, m) => new MatchListItemDto(
                            m.Id,
                            r.Mode,
                            r.Category,
                            r.QuestionCount,
                            r.EndedAtUtc,
                            r.Status
                        ))
                    .ToListAsync(ct);

                return Results.Ok(new MatchListResponseDto(page, pageSize, total, items));
            });
        }
    }
}
