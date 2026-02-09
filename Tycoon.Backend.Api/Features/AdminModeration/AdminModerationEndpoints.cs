using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Moderation;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminModeration
{
    public static class AdminModerationEndpoints
    {
        private const string AdminHeader = "X-Admin-User";

        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/moderation").WithTags("Admin/Moderation").WithOpenApi();

            g.MapGet("/profile/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                ModerationService svc,
                IAppDb db,
                CancellationToken ct) =>
            {
                var p = await db.PlayerModerationProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);

                if (p is null)
                    return Results.Ok(new ModerationProfileDto(playerId, (int)ModerationStatus.Normal, null, null, null, DateTimeOffset.UtcNow, null));

                return Results.Ok(new ModerationProfileDto(
                    p.PlayerId,
                    (int)p.Status,
                    p.Reason,
                    p.Notes,
                    p.SetByAdmin,
                    p.SetAtUtc,
                    p.ExpiresAtUtc
                ));
            });

            g.MapPost("/set-status", async (
                HttpContext ctx,
                [FromBody] SetModerationStatusRequest req,
                ModerationService svc,
                CancellationToken ct) =>
            {
                var adminUser = ctx.Request.Headers.TryGetValue(AdminHeader, out var h) ? h.ToString() : null;

                var status = (ModerationStatus)req.Status;

                var profile = await svc.SetStatusAsync(
                    req.PlayerId,
                    status,
                    req.Reason,
                    req.Notes,
                    adminUser,
                    req.ExpiresAtUtc,
                    req.RelatedFlagId,
                    ct);

                return Results.Ok(new ModerationProfileDto(
                    profile.PlayerId,
                    (int)profile.Status,
                    profile.Reason,
                    profile.Notes,
                    profile.SetByAdmin,
                    profile.SetAtUtc,
                    profile.ExpiresAtUtc
                ));
            });

            g.MapGet("/logs", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] Guid? playerId,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.ModerationActionLogs.AsNoTracking();

                if (playerId.HasValue)
                    q = q.Where(x => x.PlayerId == playerId.Value);

                q = q.OrderByDescending(x => x.CreatedAtUtc);

                var total = await q.CountAsync(ct);

                var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new ModerationLogItemDto(
                        x.Id,
                        x.PlayerId,
                        (int)x.NewStatus,
                        x.Reason,
                        x.Notes,
                        x.SetByAdmin,
                        x.CreatedAtUtc,
                        x.ExpiresAtUtc,
                        x.RelatedFlagId
                    ))
                    .ToListAsync(ct);

                return Results.Ok(new ModerationLogListResponseDto(page, pageSize, total, items));
            });
        }
    }
}
