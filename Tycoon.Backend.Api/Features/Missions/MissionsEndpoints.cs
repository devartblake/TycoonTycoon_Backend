using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Api.Realtime;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Backend.Application.Missions;
using Tycoon.Shared.Contracts.Dtos;
using Tycoon.Shared.Contracts.Realtime.Missions;

namespace Tycoon.Backend.Api.Features.Missions
{
    public static class MissionsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/missions").WithTags("Missions").WithOpenApi();

            g.MapGet("/", async ([FromQuery] string? type, IMediator mediator, CancellationToken ct) =>
            {
                var list = await mediator.Send(new ListMissions(type ?? ""), ct);
                return Results.Ok(list);
            });

            // Apply mission progress from a completed match (idempotent)
            g.MapPost("/progress/match-completed", async (
                [FromBody] MatchCompletedProgressDto dto,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new ApplyMatchCompletedProgress(dto), ct);
                return Results.Ok(result);
            });

            // Apply mission progress from a completed round (idempotent)
            g.MapPost("/progress/round-completed", async (
                [FromBody] RoundCompletedProgressDto dto,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new ApplyRoundCompletedProgress(dto), ct);
                return Results.Ok(result);
            });

            // Claim a completed mission reward (idempotent) and return updated mission list
            g.MapPost("/{missionId:guid}/claim", async (
                [FromRoute] Guid missionId,
                [FromQuery] Guid playerId,
                [FromQuery] string? type,
                IMediator mediator,
                IHubContext<NotificationHub, INotificationClient> hub,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new ClaimMission(playerId, missionId, type ?? ""), ct);

                // Broadcast only when a new claim occurs (not on AlreadyClaimed/NotCompleted/NotFound)
                if (result.Status == ClaimMissionStatus.Claimed)
                {
                    var msg = new MissionClaimedMessage(
                        PlayerId: result.PlayerId,
                        MissionId: result.MissionId,
                        MissionType: result.MissionType,
                        MissionKey: result.MissionKey,
                        RewardXp: result.RewardXp,
                        RewardCoins: result.RewardCoins,
                        RewardDiamonds: result.RewardDiamonds,
                        ClaimedAtUtc: result.ClaimedAtUtcUtc
                    );

                    // If you have real auth later, switch to Clients.User(userId)
                    await hub.Clients.Group($"player:{result.PlayerId}")
                        .MissionClaimed(msg);
                }

                return result.Status switch
                {
                    ClaimMissionStatus.NotFound => Results.NotFound(new { message = "Mission claim not found." }),
                    ClaimMissionStatus.NotCompleted => Results.BadRequest(new { message = "Mission is not completed yet." }),
                    ClaimMissionStatus.AlreadyClaimed => Results.Ok(result),
                    ClaimMissionStatus.Claimed => Results.Ok(result),
                    _ => Results.Ok(result)
                };
            });
        }
    }
}
