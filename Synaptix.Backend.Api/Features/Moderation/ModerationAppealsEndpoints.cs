using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Moderation
{
    /// <summary>
    /// Player-facing moderation appeals: a sanctioned player submits an appeal and
    /// tracks its outcome. Review happens on the admin side (/admin/moderation/appeals).
    /// </summary>
    public static class ModerationAppealsEndpoints
    {
        public static void Map(RouteGroupBuilder v1)
        {
            var g = v1.MapGroup("/moderation/appeals")
                .WithTags("Moderation")
                .RequireAuthorization();

            g.MapPost("", async (
                HttpContext ctx,
                [FromBody] SubmitAppealRequest req,
                ModerationService svc,
                CancellationToken ct) =>
            {
                if (!TryGetPlayerId(ctx.User, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing player identity.");

                if (string.IsNullOrWhiteSpace(req.Reason))
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Appeal reason is required.");

                if (req.Reason.Length > 2000)
                    return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Appeal reason must be 2000 characters or fewer.");

                var appeal = await svc.SubmitAppealAsync(playerId, req.Reason.Trim(), ct);
                if (appeal is null)
                    return ApiResponses.Error(StatusCodes.Status409Conflict, "APPEAL_PENDING", "A pending appeal already exists for this player.");

                return Results.Ok(ToDto(appeal));
            });

            g.MapGet("/mine", async (
                HttpContext ctx,
                IAppDb db,
                CancellationToken ct) =>
            {
                if (!TryGetPlayerId(ctx.User, out var playerId))
                    return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing player identity.");

                var items = await db.ModerationAppeals.AsNoTracking()
                    .Where(x => x.PlayerId == playerId)
                    .OrderByDescending(x => x.SubmittedAtUtc)
                    .Select(x => new ModerationAppealDto(
                        x.Id, x.PlayerId, x.Reason, (int)x.Status,
                        x.ReviewerNotes, x.ReviewedBy, x.SubmittedAtUtc, x.ReviewedAtUtc))
                    .ToListAsync(ct);

                return Results.Ok(items);
            });
        }

        private static bool TryGetPlayerId(ClaimsPrincipal user, out Guid playerId)
        {
            var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;
            return Guid.TryParse(raw, out playerId);
        }

        internal static ModerationAppealDto ToDto(Synaptix.Backend.Domain.Entities.ModerationAppeal x) =>
            new(x.Id, x.PlayerId, x.Reason, (int)x.Status,
                x.ReviewerNotes, x.ReviewedBy, x.SubmittedAtUtc, x.ReviewedAtUtc);
    }
}
