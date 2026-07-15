using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Security;

/// <summary>
/// Rich admin audit writer (#413): records who did what to which resource, with optional
/// before/after change snapshots and the caller's IP. Complements AdminSecurityAudit
/// (auth/session events on the notification-history channel) — existing call sites of
/// that writer are intentionally untouched.
/// </summary>
public static class AdminAuditLogger
{
    private const string AdminHeader = "X-Admin-User";

    public static async Task WriteAsync(
        IAppDb db,
        HttpContext http,
        string action,
        string resourceType,
        string? resourceId,
        object? before,
        object? after,
        CancellationToken ct = default)
    {
        db.AdminAuditLogs.Add(new AdminAuditLog(
            actor: ResolveActor(http),
            action: action,
            resourceType: resourceType,
            resourceId: resourceId,
            changesBeforeJson: before is null ? null : JsonSerializer.Serialize(before),
            changesAfterJson: after is null ? null : JsonSerializer.Serialize(after),
            ipAddress: GetClientIp(http)));

        await db.SaveChangesAsync(ct);
    }

    private static string ResolveActor(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue(AdminHeader, out var header) &&
            !string.IsNullOrWhiteSpace(header))
            return header.ToString();

        var sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? http.User.FindFirst("sub")?.Value;
        return string.IsNullOrWhiteSpace(sub) ? "unknown" : sub;
    }

    private static string GetClientIp(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
