using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Avatars;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Avatars
{
    public static class AvatarEndpoints
    {
        public static void Map(WebApplication app)
        {
            var purchaseGroup = app.MapGroup("/store/avatars")
                .WithTags("Avatars")
                
                .RequireAuthorization();

            purchaseGroup.MapPost("/{avatarId}/purchase", PurchaseAvatar);

            var assetsGroup = app.MapGroup("/v1/assets/avatars")
                .WithTags("AvatarAssets")
                
                .RequireAuthorization();

            assetsGroup.MapGet("/{avatarId}", GetAvatarAsset);
        }

        private static async Task<IResult> PurchaseAvatar(
            [FromRoute] string avatarId,
            [FromBody] PurchaseAvatarRequest body,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var jwtPlayerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication required.");

            if (jwtPlayerId != body.PlayerId)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "forbidden",
                    "Cannot purchase on behalf of another player.");

            var result = await mediator.Send(new PurchaseAvatar(body.PlayerId, avatarId), ct);

            return result.ErrorCode switch
            {
                "avatar_not_found" => ApiResponses.Error(StatusCodes.Status404NotFound,
                    "avatar_not_found", result.ErrorMessage!),
                "already_owned" => ApiResponses.Error(StatusCodes.Status409Conflict,
                    "already_owned", result.ErrorMessage!),
                "insufficient_funds" => ApiResponses.Error(StatusCodes.Status409Conflict,
                    "insufficient_funds", result.ErrorMessage!, result.ErrorDetails),
                null => Results.Ok(result.Dto),
                _ => ApiResponses.Error(StatusCodes.Status500InternalServerError,
                    "purchase_failed", result.ErrorMessage!)
            };
        }

        private static async Task<IResult> GetAvatarAsset(
            [FromRoute] string avatarId,
            HttpContext httpContext,
            IMediator mediator,
            CancellationToken ct)
        {
            if (!TryGetAuthenticatedPlayerId(httpContext.User, out var playerId))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "unauthorized", "Authentication required.");

            var result = await mediator.Send(new GetAvatarAsset(playerId, avatarId), ct);

            if (!result.Found)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "not_found",
                    $"Avatar {avatarId} not found.");

            if (!result.Owned)
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "not_owned",
                    "Player does not own this avatar.");

            return Results.Ok(result.Dto);
        }

        private static bool TryGetAuthenticatedPlayerId(ClaimsPrincipal user, out Guid playerId)
        {
            playerId = Guid.Empty;
            var raw = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? user.FindFirst("sub")?.Value;
            return raw is not null && Guid.TryParse(raw, out playerId);
        }
    }
}
