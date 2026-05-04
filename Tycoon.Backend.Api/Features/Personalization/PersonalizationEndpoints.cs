using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Personalization;

public static class PersonalizationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/personalization")
            .RequireAuthorization()
            .WithTags("Personalization")
            ;

        group.MapGet("/profile/{playerId:guid}", async (
            Guid playerId,
            HttpContext httpContext,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var profile = await profiles.GetOrCreateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapPost("/profile/{playerId:guid}/event", async (
            Guid playerId,
            [FromBody] PlayerBehaviorEventDto request,
            HttpContext httpContext,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            await profiles.RecordEventAsync(playerId, request, ct);
            return Results.Accepted();
        });

        group.MapPost("/profile/{playerId:guid}/recalculate", async (
            Guid playerId,
            HttpContext httpContext,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var profile = await profiles.RecalculateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapGet("/home/{playerId:guid}", async (
            Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var result = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapGet("/recommendations/{playerId:guid}", async (
            Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var result = await personalization.GetRecommendationsAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapGet("/notifications/{playerId:guid}", async (
            Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var result = await personalization.GetNotificationRecommendationAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapPost("/recommendations/{recommendationId:guid}/accept", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            await personalization.AcceptRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });

        group.MapPost("/recommendations/{recommendationId:guid}/dismiss", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            await personalization.DismissRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });
    }

    private static bool IsOwner(HttpContext httpContext, Guid playerId)
    {
        var sub = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var userId) && userId == playerId;
    }
}
