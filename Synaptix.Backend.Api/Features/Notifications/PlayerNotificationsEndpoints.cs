using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Notifications;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Notifications
{
    public static class PlayerNotificationsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/notifications")
                .WithTags("Notifications")
                .RequireAuthorization()
                .AddEndpointFilter(async (ctx, next) =>
                {
                    var flags = ctx.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
                    if (!await flags.IsEnabledAsync("notifications_enabled", ctx.HttpContext.RequestAborted))
                        return Results.Json(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } }, statusCode: StatusCodes.Status403Forbidden);
                    return await next(ctx);
                });

            group.MapGet("/inbox", GetInbox);
            group.MapGet("/unread-count", GetUnreadCount);
            group.MapPost("/{notificationId:guid}/read", MarkRead);
            group.MapPost("/read-all", MarkAllRead);
            group.MapDelete("/{notificationId:guid}", Delete);
        }

        private static async Task<IResult> GetInbox(
            HttpContext httpContext,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            PlayerInboxService inboxService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var response = await inboxService.GetInboxAsync(playerId, page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(response);
        }

        private static async Task<IResult> GetUnreadCount(
            HttpContext httpContext,
            PlayerInboxService inboxService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var unreadCount = await inboxService.GetUnreadCountAsync(playerId, ct);
            return Results.Ok(new UnreadCountResponseDto(unreadCount));
        }

        private static async Task<IResult> MarkRead(
            Guid notificationId,
            HttpContext httpContext,
            PlayerInboxService inboxService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var error = await inboxService.MarkReadAsync(playerId, notificationId, ct);
            return error switch
            {
                "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Notification not found."),
                "Forbidden" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Notification does not belong to the authenticated user."),
                _ => Results.Ok(new { success = true })
            };
        }

        private static async Task<IResult> MarkAllRead(
            HttpContext httpContext,
            PlayerInboxService inboxService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var updatedCount = await inboxService.MarkAllReadAsync(playerId, ct);
            return Results.Ok(new { updatedCount });
        }

        private static async Task<IResult> Delete(
            Guid notificationId,
            HttpContext httpContext,
            PlayerInboxService inboxService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var error = await inboxService.DeleteAsync(playerId, notificationId, ct);
            return error switch
            {
                "NotFound" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Notification not found."),
                "Forbidden" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Notification does not belong to the authenticated user."),
                _ => Results.Ok(new { success = true })
            };
        }

        private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
        }
    }
}
