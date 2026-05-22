using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.Messaging;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Messages
{
    public static class MessagesEndpoints
    {
        public static void Map(WebApplication app)
        {
            var group = app.MapGroup("/messages")
                .WithTags("Messages")
                .RequireAuthorization()
                .AddEndpointFilter(async (ctx, next) =>
                {
                    var flags = ctx.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
                    if (!await flags.IsEnabledAsync("social_enabled", ctx.HttpContext.RequestAborted))
                        return Results.Json(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } }, statusCode: StatusCodes.Status403Forbidden);
                    return await next(ctx);
                });

            group.MapGet("/conversations", GetConversations);
            group.MapPost("/conversations/direct", CreateDirectConversation);
            group.MapGet("/conversations/{conversationId:guid}/messages", GetMessages);
            group.MapPost("/conversations/{conversationId:guid}/messages", SendMessage);
            group.MapPost("/conversations/{conversationId:guid}/read", MarkConversationRead);
            group.MapGet("/unread-count", GetUnreadCount);
        }

        private static async Task<IResult> GetConversations(
            HttpContext httpContext,
            DirectMessagingService messagingService,
            int? page,
            int? pageSize,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var response = await messagingService.GetConversationsAsync(playerId, page ?? 1, pageSize ?? 50, ct);
            return Results.Ok(response);
        }

        private static async Task<IResult> CreateDirectConversation(
            CreateDirectConversationRequestDto request,
            HttpContext httpContext,
            DirectMessagingService messagingService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var result = await messagingService.GetOrCreateDirectConversationAsync(playerId, request.TargetPlayerId, ct);
            return result.Error switch
            {
                "validation_error" => ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "targetPlayerId is required."),
                "self_dm_not_allowed" => ApiResponses.Error(StatusCodes.Status400BadRequest, "SELF_DM_NOT_ALLOWED", "You cannot create a direct conversation with yourself."),
                "not_found" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Target player not found."),
                _ => Results.Ok(result.Conversation)
            };
        }

        private static async Task<IResult> GetMessages(
            Guid conversationId,
            HttpContext httpContext,
            DirectMessagingService messagingService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var result = await messagingService.GetMessagesAsync(playerId, conversationId, ct);
            return result.Error switch
            {
                "forbidden" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Conversation does not belong to the authenticated user."),
                "not_found" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Conversation not found."),
                _ => Results.Ok(result.Messages)
            };
        }

        private static async Task<IResult> SendMessage(
            Guid conversationId,
            SendDirectMessageRequestDto request,
            HttpContext httpContext,
            DirectMessagingService messagingService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var result = await messagingService.SendMessageAsync(playerId, conversationId, request.Content, request.ClientMessageId, ct);
            return result.Error switch
            {
                "validation_error" => ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "content is required."),
                "forbidden" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Conversation does not belong to the authenticated user."),
                "not_found" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Conversation not found."),
                _ => Results.Ok(result.Message)
            };
        }

        private static async Task<IResult> MarkConversationRead(
            Guid conversationId,
            HttpContext httpContext,
            DirectMessagingService messagingService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var error = await messagingService.MarkConversationReadAsync(playerId, conversationId, ct);
            return error switch
            {
                "forbidden" => ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Conversation does not belong to the authenticated user."),
                "not_found" => ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Conversation not found."),
                _ => Results.Ok(new { success = true })
            };
        }

        private static async Task<IResult> GetUnreadCount(
            HttpContext httpContext,
            DirectMessagingService messagingService,
            CancellationToken ct)
        {
            if (!TryGetUserId(httpContext, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");

            var unreadCount = await messagingService.GetUnreadCountAsync(playerId, ct);
            return Results.Ok(new UnreadCountResponseDto(unreadCount));
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
