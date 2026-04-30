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
            .WithOpenApi();

        group.MapGet("/profile/{playerId:guid}", async (
            Guid playerId,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            var profile = await profiles.GetOrCreateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapPost("/profile/{playerId:guid}/event", async (
            Guid playerId,
            [FromBody] PlayerBehaviorEventDto request,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            await profiles.RecordEventAsync(playerId, request, ct);
            return Results.Accepted();
        });

        group.MapPost("/profile/{playerId:guid}/recalculate", async (
            Guid playerId,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            var profile = await profiles.RecalculateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapGet("/home/{playerId:guid}", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var result = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapGet("/recommendations/{playerId:guid}", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var result = await personalization.GetRecommendationsAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapPost("/recommendations/{recommendationId:guid}/accept", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            await personalization.AcceptRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });

        group.MapPost("/recommendations/{recommendationId:guid}/dismiss", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            await personalization.DismissRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });
    }
}
