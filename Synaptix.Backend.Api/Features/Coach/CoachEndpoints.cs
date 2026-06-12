using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Coach;

public static class CoachEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/coach")
            .RequireAuthorization()
            .WithTags("Coach")
            ;

        group.MapGet("/{playerId:guid}/daily-brief", async (
            Guid playerId,
            HttpContext httpContext,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
            var home = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(home.CoachBrief);
        });

        group.MapPost("/{playerId:guid}/feedback", async (
            Guid playerId,
            [FromBody] CoachFeedbackRequest request,
            HttpContext httpContext,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            if (!IsOwner(httpContext, playerId)) return Results.Forbid();
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

    private static bool IsOwner(HttpContext httpContext, Guid playerId)
    {
        var sub = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? httpContext.User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var userId) && userId == playerId;
    }
}
