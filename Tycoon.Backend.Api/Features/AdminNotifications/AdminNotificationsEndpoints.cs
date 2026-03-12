using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Api.Observability;
using Tycoon.Backend.Api.Security;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminNotifications;

public static class AdminNotificationsEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/notifications").WithTags("Admin/Notifications").WithOpenApi();

        g.MapGet("/channels", async (IAppDb db, CancellationToken ct) =>
        {
            await EnsureSeedChannel(db, ct);
            var channels = await db.AdminNotificationChannels.AsNoTracking()
                .OrderBy(x => x.Key)
                .Select(x => new AdminNotificationChannelDto(x.Key, x.Name, x.Description, x.Importance, x.Enabled))
                .ToListAsync(ct);
            return Results.Ok(channels);
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);

        g.MapPut("/channels/{key}", async (string key, [FromBody] UpsertAdminNotificationChannelRequest request, IAppDb db, CancellationToken ct) =>
        {
            var channel = await db.AdminNotificationChannels.FirstOrDefaultAsync(x => x.Key == key, ct);
            if (channel is null)
            {
                channel = new AdminNotificationChannel(key, request.Name, request.Description, request.Importance, request.Enabled);
                db.AdminNotificationChannels.Add(channel);
            }
            else
            {
                channel.Update(request.Name, request.Description, request.Importance, request.Enabled);
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new AdminNotificationChannelDto(channel.Key, channel.Name, channel.Description, channel.Importance, channel.Enabled));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapPost("/send", async ([FromBody] AdminNotificationSendRequest request, HttpContext httpContext, IAppDb db, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            if (!await db.AdminNotificationChannels.AnyAsync(x => x.Key == request.ChannelKey, ct))
            {
                await AdminSecurityAudit.WriteAsync(db, "admin_notifications_send", "not_found", new { channelKey = request.ChannelKey }, ct);
                AdminSecurityMetrics.RecordNotification("send", "not_found", sw);
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Channel not found.");
            }

            var jobId = $"push_job_{Guid.NewGuid():N}";
            db.AdminNotificationHistory.Add(new AdminNotificationHistory(
                id: jobId,
                channelKey: request.ChannelKey,
                title: request.Title,
                status: "queued",
                createdAt: DateTimeOffset.UtcNow,
                metadataJson: request.Payload is null ? null : JsonSerializer.Serialize(request.Payload)));

            await db.SaveChangesAsync(ct);
            await AdminSecurityAudit.WriteAsync(db, "admin_notifications_send", "accepted", new
            {
                channelKey = request.ChannelKey,
                actor = httpContext.User.FindFirst("sub")?.Value
            }, ct);
            AdminSecurityMetrics.RecordNotification("send", "accepted", sw);
            return Results.Accepted(value: new AdminNotificationSendResponse(jobId, EstimatedRecipients: 0));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapPost("/schedule", async ([FromBody] AdminNotificationScheduleRequest request, HttpContext httpContext, IAppDb db, CancellationToken ct) =>
        {
            var sw = Stopwatch.StartNew();
            if (!await db.AdminNotificationChannels.AnyAsync(x => x.Key == request.ChannelKey, ct))
            {
                await AdminSecurityAudit.WriteAsync(db, "admin_notifications_schedule", "not_found", new { channelKey = request.ChannelKey }, ct);
                AdminSecurityMetrics.RecordNotification("schedule", "not_found", sw);
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Channel not found.");
            }

            var scheduleId = $"sch_{Guid.NewGuid():N}";
            db.AdminNotificationSchedules.Add(new AdminNotificationSchedule(scheduleId, request.Title, request.Body, request.ChannelKey, request.ScheduledAt));
            await db.SaveChangesAsync(ct);

            await AdminSecurityAudit.WriteAsync(db, "admin_notifications_schedule", "created", new
            {
                scheduleId,
                channelKey = request.ChannelKey,
                actor = httpContext.User.FindFirst("sub")?.Value
            }, ct);
            AdminSecurityMetrics.RecordNotification("schedule", "created", sw);

            return Results.Created($"/admin/notifications/scheduled/{scheduleId}", new AdminNotificationScheduleResponse(scheduleId));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapGet("/scheduled", async ([FromQuery] int page, [FromQuery] int pageSize, IAppDb db, CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var baseQ = db.AdminNotificationSchedules.AsNoTracking()
                .Where(x => x.Status == "scheduled" || x.Status == "retry_pending");
            var totalItems = await baseQ.CountAsync(ct);
            var items = await baseQ.OrderBy(x => x.ScheduledAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminNotificationScheduledItemDto(x.ScheduleId, x.Title, x.ChannelKey, x.ScheduledAt, x.Status))
                .ToListAsync(ct);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            return Results.Ok(new AdminNotificationScheduledListResponse(items, page, pageSize, totalItems, totalPages));
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);


        g.MapGet("/dead-letter", async ([FromQuery] int page, [FromQuery] int pageSize, IAppDb db, CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var baseQ = db.AdminNotificationSchedules.AsNoTracking()
                .Where(x => x.Status == "failed");
            var totalItems = await baseQ.CountAsync(ct);
            var items = await baseQ.OrderByDescending(x => x.ProcessedAtUtc ?? x.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminNotificationScheduledItemDto(x.ScheduleId, x.Title, x.ChannelKey, x.ScheduledAt, x.Status))
                .ToListAsync(ct);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            return Results.Ok(new AdminNotificationScheduledListResponse(items, page, pageSize, totalItems, totalPages));
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);

        g.MapPost("/dead-letter/{scheduleId}/replay", async (string scheduleId, HttpContext httpContext, IAppDb db, CancellationToken ct) =>
        {
            var schedule = await db.AdminNotificationSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, ct);
            if (schedule is null)
            {
                await AdminSecurityAudit.WriteAsync(db, "admin_notifications_dead_letter_replay", "not_found", new { scheduleId }, ct);
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Schedule not found.");
            }

            if (!schedule.CanReplay())
            {
                await AdminSecurityAudit.WriteAsync(db, "admin_notifications_dead_letter_replay", "conflict", new { scheduleId, status = schedule.Status }, ct);
                return AdminApiResponses.Error(StatusCodes.Status409Conflict, "CONFLICT", "Only failed schedules can be replayed.");
            }

            schedule.Replay(DateTimeOffset.UtcNow.AddMinutes(1));
            await db.SaveChangesAsync(ct);
            await AdminSecurityAudit.WriteAsync(db, "admin_notifications_dead_letter_replay", "queued", new
            {
                scheduleId = schedule.ScheduleId,
                actor = httpContext.User.FindFirst("sub")?.Value
            }, ct);

            return Results.Ok(new AdminNotificationScheduleResponse(schedule.ScheduleId));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapDelete("/scheduled/{scheduleId}", async (string scheduleId, IAppDb db, CancellationToken ct) =>
        {
            var schedule = await db.AdminNotificationSchedules.FirstOrDefaultAsync(x => x.ScheduleId == scheduleId, ct);
            if (schedule is null)
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Schedule not found.");
            }

            db.AdminNotificationSchedules.Remove(schedule);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapGet("/templates", async (IAppDb db, CancellationToken ct) =>
        {
            var templates = await db.AdminNotificationTemplates.AsNoTracking()
                .OrderBy(x => x.Name)
                .ToListAsync(ct);
            var dtos = templates.Select(ToTemplateDto).ToList();
            return Results.Ok(dtos);
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);

        g.MapPost("/templates", async ([FromBody] AdminNotificationTemplateRequest request, IAppDb db, CancellationToken ct) =>
        {
            if (!await db.AdminNotificationChannels.AnyAsync(x => x.Key == request.ChannelKey, ct))
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Channel not found.");
            }

            var id = $"tpl_{Guid.NewGuid():N}";
            var entity = new AdminNotificationTemplate(id, request.Name, request.Title, request.Body, request.ChannelKey, JsonSerializer.Serialize(request.Variables));
            db.AdminNotificationTemplates.Add(entity);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/admin/notifications/templates/{id}", ToTemplateDto(entity));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapPatch("/templates/{templateId}", async (string templateId, [FromBody] AdminNotificationTemplateRequest request, IAppDb db, CancellationToken ct) =>
        {
            var entity = await db.AdminNotificationTemplates.FirstOrDefaultAsync(x => x.TemplateId == templateId, ct);
            if (entity is null)
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Template not found.");
            }

            if (!await db.AdminNotificationChannels.AnyAsync(x => x.Key == request.ChannelKey, ct))
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Channel not found.");
            }

            entity.Update(request.Name, request.Title, request.Body, request.ChannelKey, JsonSerializer.Serialize(request.Variables));
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToTemplateDto(entity));
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapDelete("/templates/{templateId}", async (string templateId, IAppDb db, CancellationToken ct) =>
        {
            var entity = await db.AdminNotificationTemplates.FirstOrDefaultAsync(x => x.TemplateId == templateId, ct);
            if (entity is null)
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Template not found.");
            }

            db.AdminNotificationTemplates.Remove(entity);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        })
        .RequireAuthorization(AdminPolicies.AdminNotificationsWritePolicy)
        .RequireRateLimiting("admin-notifications-send");

        g.MapGet("/history", async (
            [FromQuery] DateTimeOffset? from,
            [FromQuery] DateTimeOffset? to,
            [FromQuery] string? channelKey,
            [FromQuery] string? status,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            IAppDb db,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var q = db.AdminNotificationHistory.AsNoTracking().AsQueryable();
            if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
            if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
            if (!string.IsNullOrWhiteSpace(channelKey)) q = q.Where(x => x.ChannelKey == channelKey);
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
        })
        .RequireAuthorization(AdminPolicies.AdminOpsPolicy);
    }

    private static AdminNotificationTemplateDto ToTemplateDto(AdminNotificationTemplate x)
        => new(x.TemplateId, x.Name, x.Title, x.Body, x.ChannelKey, DeserializeVariables(x.VariablesJson), x.UpdatedAtUtc);

    private static IReadOnlyList<string> DeserializeVariables(string json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? []; }
        catch { return []; }
    }

    private static Dictionary<string, object>? DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return parsed;
        }
        catch
        {
            return null;
        }
    }

    private static async Task EnsureSeedChannel(IAppDb db, CancellationToken ct)
    {
        if (await db.AdminNotificationChannels.AnyAsync(x => x.Key == "admin_basic", ct)) return;
        db.AdminNotificationChannels.Add(new AdminNotificationChannel("admin_basic", "Admin Basic", "General admin notifications", "high", true));
        await db.SaveChangesAsync(ct);
    }
}
