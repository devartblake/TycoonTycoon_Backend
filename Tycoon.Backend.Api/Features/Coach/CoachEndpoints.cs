using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Coach;

public static class CoachEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/coach")
            .RequireAuthorization()
            .WithTags("Coach")
            .WithOpenApi();

        group.MapGet("/{playerId:guid}/daily-brief", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var home = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(home.CoachBrief);
        });

        group.MapPost("/{playerId:guid}/feedback", async (
            Guid playerId,
            [FromBody] CoachFeedbackRequest request,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            await profiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                EventType: "coach_feedback",
                EventSource: "coach",
                Category: null,
                Difficulty: null,
                Mode: null,
                Metadata: new Dictionary<string, object>
                {
                    ["feedback"] = request.Feedback,
                    ["briefId"] = request.BriefId
                },
                OccurredAt: DateTimeOffset.UtcNow), ct);

            return Results.Accepted();
        });
    }
}
