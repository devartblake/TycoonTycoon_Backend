using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminAnalytics;

public static class AdminAuditEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/audit").WithTags("Admin/Audit").WithOpenApi();

        g.MapGet("/security", async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var q = db.AdminNotificationHistory
                .AsNoTracking()
                .Where(x => x.ChannelKey == "admin_security");

            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);

            var totalItems = await q.CountAsync(ct);
            var rows = await q.OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = rows.Select(x => new AdminNotificationHistoryItemDto(
                x.Id,
                x.ChannelKey,
                x.Title,
                x.Status,
                x.CreatedAt,
                DeserializeMetadata(x.MetadataJson))).ToList();

            return Results.Ok(new AdminNotificationHistoryResponse(items, page, pageSize, totalItems, totalPages));
        });
    }

    private static Dictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch
        {
            return null;
        }
    }
}
