using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminModeration
{
    public static class AdminModerationEndpoints
    {
        private const string AdminHeader = "X-Admin-User";

        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/moderation").WithTags("Admin/Moderation");

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
                IAppDb db,
                CancellationToken ct) =>
            {
                var adminUser = ctx.Request.Headers.TryGetValue(AdminHeader, out var h) ? h.ToString() : null;

                var status = (ModerationStatus)req.Status;

                // Snapshot the pre-change state for the before/after audit trail (#413).
                var before = await db.PlayerModerationProfiles.AsNoTracking()
                    .Where(x => x.PlayerId == req.PlayerId)
                    .Select(x => new { status = (int)x.Status, reason = x.Reason, expiresAtUtc = x.ExpiresAtUtc })
                    .FirstOrDefaultAsync(ct);

                var profile = await svc.SetStatusAsync(
                    req.PlayerId,
                    status,
                    req.Reason,
                    req.Notes,
                    adminUser,
                    req.ExpiresAtUtc,
                    req.RelatedFlagId,
                    ct);

                await AdminAuditLogger.WriteAsync(
                    db, ctx,
                    action: "moderation.set_status",
                    resourceType: "player",
                    resourceId: req.PlayerId.ToString(),
                    before: (object?)before ?? new { status = (int)ModerationStatus.Normal },
                    after: new { status = (int)profile.Status, reason = profile.Reason, expiresAtUtc = profile.ExpiresAtUtc },
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
                [FromQuery] string? status,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.ModerationActionLogs.AsNoTracking();

                if (playerId.HasValue)
                    q = q.Where(x => x.PlayerId == playerId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var parsed = int.TryParse(status, out var statusNum)
                        ? (ModerationStatus?)(ModerationStatus)statusNum
                        : Enum.TryParse<ModerationStatus>(status, true, out var statusEnum) ? statusEnum : null;

                    if (parsed is null || !Enum.IsDefined(typeof(ModerationStatus), parsed.Value))
                        return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Invalid moderation status filter.");

                    q = q.Where(x => x.NewStatus == parsed.Value);
                }

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

            g.MapGet("/logs/{id:guid}", async (
                [FromRoute] Guid id,
                IAppDb db,
                CancellationToken ct) =>
            {
                var item = await db.ModerationActionLogs.AsNoTracking()
                    .Where(x => x.Id == id)
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
                    .FirstOrDefaultAsync(ct);

                return item is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(item);
            });

            g.MapGet("/appeals", async (
                [FromQuery] int page,
                [FromQuery] int pageSize,
                [FromQuery] Guid? playerId,
                [FromQuery] string? status,
                IAppDb db,
                CancellationToken ct) =>
            {
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize, 1, 100);

                var q = db.ModerationAppeals.AsNoTracking();

                if (playerId.HasValue)
                    q = q.Where(x => x.PlayerId == playerId.Value);

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var parsed = int.TryParse(status, out var statusNum)
                        ? (ModerationAppealStatus?)(ModerationAppealStatus)statusNum
                        : Enum.TryParse<ModerationAppealStatus>(status, true, out var statusEnum) ? statusEnum : null;

                    if (parsed is null || !Enum.IsDefined(typeof(ModerationAppealStatus), parsed.Value))
                        return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Invalid appeal status filter.");

                    q = q.Where(x => x.Status == parsed.Value);
                }

                q = q.OrderByDescending(x => x.SubmittedAtUtc);

                var total = await q.CountAsync(ct);

                var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                    .Select(x => new ModerationAppealDto(
                        x.Id,
                        x.PlayerId,
                        x.Reason,
                        (int)x.Status,
                        x.ReviewerNotes,
                        x.ReviewedBy,
                        x.SubmittedAtUtc,
                        x.ReviewedAtUtc
                    ))
                    .ToListAsync(ct);

                return Results.Ok(new ModerationAppealListResponseDto(page, pageSize, total, items));
            });

            g.MapGet("/appeals/{id:guid}", async (
                [FromRoute] Guid id,
                IAppDb db,
                CancellationToken ct) =>
            {
                var item = await db.ModerationAppeals.AsNoTracking()
                    .Where(x => x.Id == id)
                    .Select(x => new ModerationAppealDto(
                        x.Id,
                        x.PlayerId,
                        x.Reason,
                        (int)x.Status,
                        x.ReviewerNotes,
                        x.ReviewedBy,
                        x.SubmittedAtUtc,
                        x.ReviewedAtUtc
                    ))
                    .FirstOrDefaultAsync(ct);

                return item is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(item);
            });

            g.MapPost("/appeals/{id:guid}/review", async (
                HttpContext ctx,
                [FromRoute] Guid id,
                [FromBody] ReviewAppealRequest req,
                ModerationService svc,
                IAppDb db,
                CancellationToken ct) =>
            {
                var verdict = req.Verdict?.Trim().ToLowerInvariant();
                if (verdict is not ("approve" or "reject"))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Verdict must be 'approve' or 'reject'.");

                var adminUser = ctx.Request.Headers.TryGetValue(AdminHeader, out var h) ? h.ToString() : null;

                try
                {
                    var appeal = await svc.ReviewAppealAsync(id, verdict == "approve", req.ReviewerNotes, adminUser, ct);
                    if (appeal is null)
                        return AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.");

                    await AdminAuditLogger.WriteAsync(
                        db, ctx,
                        action: "appeal.review",
                        resourceType: "appeal",
                        resourceId: appeal.Id.ToString(),
                        before: new { status = (int)ModerationAppealStatus.Pending },
                        after: new { status = (int)appeal.Status, reviewerNotes = appeal.ReviewerNotes, playerId = appeal.PlayerId },
                        ct);

                    return Results.Ok(new ModerationAppealDto(
                        appeal.Id,
                        appeal.PlayerId,
                        appeal.Reason,
                        (int)appeal.Status,
                        appeal.ReviewerNotes,
                        appeal.ReviewedBy,
                        appeal.SubmittedAtUtc,
                        appeal.ReviewedAtUtc));
                }
                catch (InvalidOperationException)
                {
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "ALREADY_REVIEWED", "This appeal has already been reviewed.");
                }
            });
        }
    }
}
