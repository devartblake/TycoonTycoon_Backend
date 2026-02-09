using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminAntiCheat
{
    public static class AdminAntiCheatAnalyticsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/anti-cheat/analytics").WithTags("Admin/AntiCheat").WithOpenApi();

            g.MapGet("/summary", async (
                [FromQuery] int windowHours,
                IAppDb db,
                CancellationToken ct) =>
            {
                windowHours = Math.Clamp(windowHours, 1, 168);
                var end = DateTimeOffset.UtcNow;
                var start = end.AddHours(-windowHours);

                var q = db.AntiCheatFlags.AsNoTracking()
                    .Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc <= end);

                var total = await q.CountAsync(ct);

                var severe = await q.CountAsync(x => x.Severity == AntiCheatSeverity.Severe, ct);
                var warning = await q.CountAsync(x => x.Severity == AntiCheatSeverity.Warning, ct);
                var info = await q.CountAsync(x => x.Severity == AntiCheatSeverity.Info, ct);

                var byRule = await q.GroupBy(x => new { x.RuleKey, x.Severity })
                    .Select(g => new AntiCheatRuleCountDto(g.Key.RuleKey, (int)g.Key.Severity, g.Count()))
                    .OrderByDescending(x => x.Count)
                    .Take(50)
                    .ToListAsync(ct);

                return Results.Ok(new AntiCheatSummaryDto(start, end, total, severe, warning, info, byRule));
            });

            g.MapGet("/players", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] int windowHours,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);
                windowHours = Math.Clamp(windowHours, 1, 168);

                var end = DateTimeOffset.UtcNow;
                var start = end.AddHours(-windowHours);

                // Aggregate flags per player within window
                var baseQ = db.AntiCheatFlags.AsNoTracking()
                    .Where(x => x.PlayerId != null && x.CreatedAtUtc >= start && x.CreatedAtUtc <= end);

                var grouped = baseQ.GroupBy(x => x.PlayerId!.Value)
                    .Select(g => new
                    {
                        PlayerId = g.Key,
                        Severe = g.Count(x => x.Severity == AntiCheatSeverity.Severe),
                        Warning = g.Count(x => x.Severity == AntiCheatSeverity.Warning),
                        Last = g.Max(x => x.CreatedAtUtc)
                    });

                var total = await grouped.CountAsync(ct);

                var pageItems = await grouped
                    .OrderByDescending(x => x.Severe)
                    .ThenByDescending(x => x.Warning)
                    .ThenByDescending(x => x.Last)
                    .Skip((page - 1) * pageSize).Take(pageSize)
                    .ToListAsync(ct);

                // Pull moderation statuses in batch
                var ids = pageItems.Select(x => x.PlayerId).ToList();

                var statusMap = await db.PlayerModerationProfiles.AsNoTracking()
                    .Where(x => ids.Contains(x.PlayerId))
                    .ToDictionaryAsync(x => x.PlayerId, x => (int)x.Status, ct);

                var items = pageItems.Select(x => new PlayerRiskRowDto(
                    x.PlayerId,
                    x.Severe,
                    x.Warning,
                    statusMap.TryGetValue(x.PlayerId, out var s) ? s : (int)ModerationStatus.Normal,
                    x.Last)).ToList();

                return Results.Ok(new PlayerRiskListResponseDto(page, pageSize, total, items));
            });
        }
    }
}
