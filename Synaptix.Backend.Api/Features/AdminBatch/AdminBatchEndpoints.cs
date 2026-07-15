using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Admin;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminBatch
{
    /// <summary>
    /// Bulk admin operations (#413). Each returns per-player partial-failure results —
    /// one bad id never aborts the batch.
    /// </summary>
    public static class AdminBatchEndpoints
    {
        private const string AdminHeader = "X-Admin-User";

        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/batch").WithTags("Admin/Batch");

            g.MapPost("/ban", async (
                HttpContext ctx,
                [FromBody] AdminBulkBanRequest req,
                IMediator mediator,
                IAppDb db,
                CancellationToken ct) =>
            {
                if (req.PlayerIds is null || req.PlayerIds.Count == 0)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerIds is required.");
                if (req.PlayerIds.Count > AdminBatchOperations.MaxBatchSize)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", $"At most {AdminBatchOperations.MaxBatchSize} player ids per batch.");
                if (string.IsNullOrWhiteSpace(req.Reason))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Reason is required.");

                var actor = GetActor(ctx);
                var result = await mediator.Send(new AdminBulkBan(req.PlayerIds, req.Reason.Trim(), req.Until, actor), ct);

                await WriteBatchAuditAsync(db, ctx, "batch.ban",
                    new { playerCount = req.PlayerIds.Count, reason = req.Reason.Trim(), until = req.Until },
                    result, ct);

                return Results.Ok(result);
            });

            g.MapPost("/reward", async (
                HttpContext ctx,
                [FromBody] AdminBulkRewardRequest req,
                IMediator mediator,
                IAppDb db,
                CancellationToken ct) =>
            {
                if (req.BatchId == Guid.Empty)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "BatchId is required (used for idempotent retries).");
                if (req.PlayerIds is null || req.PlayerIds.Count == 0)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerIds is required.");
                if (req.PlayerIds.Count > AdminBatchOperations.MaxBatchSize)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", $"At most {AdminBatchOperations.MaxBatchSize} player ids per batch.");
                if (req.Rewards is null || req.Rewards.Count == 0)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Rewards must contain at least one line.");

                var actor = GetActor(ctx);
                var result = await mediator.Send(new AdminBulkReward(req.BatchId, req.PlayerIds, req.Rewards, req.Note, actor), ct);

                await WriteBatchAuditAsync(db, ctx, "batch.reward",
                    new { batchId = req.BatchId, playerCount = req.PlayerIds.Count, rewards = req.Rewards, note = req.Note },
                    result, ct);

                return Results.Ok(result);
            });

            g.MapPost("/reset-progress", async (
                HttpContext ctx,
                [FromBody] AdminBulkResetProgressRequest req,
                IMediator mediator,
                IAppDb db,
                CancellationToken ct) =>
            {
                // Only the skills scope has an existing, audited, reversible single-player
                // reset path (SkillTreeService.RespecAsync). Other scopes are rejected until
                // equivalent per-scope reset paths exist.
                if (!string.Equals(req.Scope, "skills", StringComparison.OrdinalIgnoreCase))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Scope must be 'skills'.");
                if (req.BatchId == Guid.Empty)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "BatchId is required (used for idempotent retries).");
                if (req.PlayerIds is null || req.PlayerIds.Count == 0)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "PlayerIds is required.");
                if (req.PlayerIds.Count > AdminBatchOperations.MaxBatchSize)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", $"At most {AdminBatchOperations.MaxBatchSize} player ids per batch.");

                var refundPercent = Math.Clamp(req.RefundPercent ?? 100, 0, 100);

                var actor = GetActor(ctx);
                var result = await mediator.Send(new AdminBulkResetProgress(req.BatchId, req.PlayerIds, refundPercent, actor), ct);

                await WriteBatchAuditAsync(db, ctx, "batch.reset_progress",
                    new { batchId = req.BatchId, playerCount = req.PlayerIds.Count, scope = "skills", refundPercent },
                    result, ct);

                return Results.Ok(result);
            });
        }

        private static Task WriteBatchAuditAsync(
            IAppDb db, HttpContext ctx, string action, object request,
            BatchOperationResultDto result, CancellationToken ct) =>
            AdminAuditLogger.WriteAsync(
                db, ctx, action,
                resourceType: "batch",
                resourceId: null,
                before: request,
                after: new
                {
                    result.Requested,
                    result.Succeeded,
                    result.Failed,
                    failedIds = result.Items.Where(i => !i.Success).Select(i => i.PlayerId).ToArray()
                },
                ct);

        private static string? GetActor(HttpContext ctx) =>
            ctx.Request.Headers.TryGetValue(AdminHeader, out var h) ? h.ToString() : null;
    }
}
