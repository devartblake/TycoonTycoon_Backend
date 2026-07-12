using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminAnalytics;

public static class AdminAuditEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/audit").WithTags("Admin/Audit");

        // Resolve the distinct client IPs the dashboard collected from audit
        // events to map coordinates (see IGeoIpResolver).
        g.MapPost("/geo-lookup", async (
            [FromBody] GeoLookupRequest req,
            IGeoIpResolver resolver,
            CancellationToken ct) =>
        {
            var results = await resolver.ResolveAsync(req?.Ips ?? Array.Empty<string>(), ct);
            return Results.Ok(results);
        });

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

        g.MapGet("/security/{id}", async (
            [FromRoute] string id,
            IAppDb db,
            CancellationToken ct) =>
        {
            var row = await db.AdminNotificationHistory
                .AsNoTracking()
                .Where(x => x.ChannelKey == "admin_security" && x.Id == id)
                .FirstOrDefaultAsync(ct);

            if (row is null)
            {
                return Results.NotFound(new
                {
                    code = "NOT_FOUND",
                    message = "Resource not found."
                });
            }

            return Results.Ok(new AdminNotificationHistoryItemDto(
                row.Id,
                row.ChannelKey,
                row.Title,
                row.Status,
                row.CreatedAt,
                DeserializeMetadata(row.MetadataJson)));
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
