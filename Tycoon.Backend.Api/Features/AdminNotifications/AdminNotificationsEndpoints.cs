using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminNotifications;

public static class AdminNotificationsEndpoints
{
    private static readonly ConcurrentDictionary<string, AdminNotificationChannelDto> Channels = new(
        new[]
        {
            new KeyValuePair<string, AdminNotificationChannelDto>(
                "admin_basic",
                new AdminNotificationChannelDto("admin_basic", "Admin Basic", "General admin notifications", "high", true))
        });

    private static readonly ConcurrentDictionary<string, AdminNotificationScheduledItemDto> Schedules = new();
    private static readonly ConcurrentDictionary<string, AdminNotificationTemplateDto> Templates = new();
    private static readonly ConcurrentQueue<AdminNotificationHistoryItemDto> History = new();

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/notifications").WithTags("Admin/Notifications").WithOpenApi();

        g.MapGet("/channels", () => Results.Ok(Channels.Values.OrderBy(x => x.Key)));

        g.MapPut("/channels/{key}", (string key, [FromBody] UpsertAdminNotificationChannelRequest request) =>
        {
            var dto = new AdminNotificationChannelDto(key, request.Name, request.Description, request.Importance, request.Enabled);
            Channels[key] = dto;
            return Results.Ok(dto);
        });

        g.MapPost("/send", ([FromBody] AdminNotificationSendRequest request) =>
        {
            if (!Channels.ContainsKey(request.ChannelKey))
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Channel not found.");
            }

            var jobId = $"push_job_{Guid.NewGuid():N}";
            History.Enqueue(new AdminNotificationHistoryItemDto(
                Id: jobId,
                ChannelKey: request.ChannelKey,
                Title: request.Title,
                Status: "queued",
                CreatedAt: DateTimeOffset.UtcNow,
                Metadata: request.Payload));

            return Results.Accepted(value: new AdminNotificationSendResponse(jobId, EstimatedRecipients: 0));
        });

        g.MapPost("/schedule", ([FromBody] AdminNotificationScheduleRequest request) =>
        {
            var scheduleId = $"sch_{Guid.NewGuid():N}";
            var dto = new AdminNotificationScheduledItemDto(scheduleId, request.Title, request.ChannelKey, request.ScheduledAt, "scheduled");
            Schedules[scheduleId] = dto;
            return Results.Created($"/admin/notifications/scheduled/{scheduleId}", new AdminNotificationScheduleResponse(scheduleId));
        });

        g.MapGet("/scheduled", ([FromQuery] int page, [FromQuery] int pageSize) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var all = Schedules.Values.OrderBy(x => x.ScheduledAt).ToList();
            var totalItems = all.Count;
            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            return Results.Ok(new AdminNotificationScheduledListResponse(items, page, pageSize, totalItems, totalPages));
        });

        g.MapDelete("/scheduled/{scheduleId}", (string scheduleId) =>
        {
            return Schedules.TryRemove(scheduleId, out _)
                ? Results.NoContent()
                : AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Schedule not found.");
        });

        g.MapGet("/templates", () => Results.Ok(Templates.Values.OrderBy(x => x.Name)));

        g.MapPost("/templates", ([FromBody] AdminNotificationTemplateRequest request) =>
        {
            var id = $"tpl_{Guid.NewGuid():N}";
            var dto = new AdminNotificationTemplateDto(id, request.Name, request.Title, request.Body, request.ChannelKey, request.Variables, DateTimeOffset.UtcNow);
            Templates[id] = dto;
            return Results.Created($"/admin/notifications/templates/{id}", dto);
        });

        g.MapPatch("/templates/{templateId}", (string templateId, [FromBody] AdminNotificationTemplateRequest request) =>
        {
            if (!Templates.ContainsKey(templateId))
            {
                return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Template not found.");
            }

            var dto = new AdminNotificationTemplateDto(templateId, request.Name, request.Title, request.Body, request.ChannelKey, request.Variables, DateTimeOffset.UtcNow);
            Templates[templateId] = dto;
            return Results.Ok(dto);
        });

        g.MapDelete("/templates/{templateId}", (string templateId) =>
        {
            return Templates.TryRemove(templateId, out _)
                ? Results.NoContent()
                : AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Template not found.");
        });

        g.MapGet("/history", ([FromQuery] string? channelKey, [FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200);

            var all = History.ToList().AsEnumerable();
            if (!string.IsNullOrWhiteSpace(channelKey)) all = all.Where(x => string.Equals(x.ChannelKey, channelKey, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(status)) all = all.Where(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));

            var ordered = all.OrderByDescending(x => x.CreatedAt).ToList();
            var totalItems = ordered.Count;
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

            return Results.Ok(new AdminNotificationHistoryResponse(items, page, pageSize, totalItems, totalPages));
        });
    }
}
