using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Seasons;

public static class SeasonRewardsEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/seasons/rewards").WithTags("Seasons/Rewards");

        // Eligibility (UI hook)
        g.MapGet("/eligibility/{playerId:guid}", async (
            Guid playerId,
            [FromQuery] Guid? seasonId,
            SeasonRewardsService svc,
            CancellationToken ct) =>
        {
            var r = await svc.GetEligibilityAsync(playerId, seasonId, ct);
            return Results.Ok(r);
        });

        // Claim daily reward (UI hook)
        g.MapPost("/claim/{playerId:guid}", async (
            Guid playerId,
            ClaimSeasonRewardRequestDto body,
            SeasonRewardsService svc,
            CancellationToken ct) =>
        {
            var r = await svc.ClaimAsync(playerId, body, ct);
            return Results.Ok(r);
        });

        // Preview eligibility (admin hook)
        g.MapGet("/preview/{playerId:guid}", async (
            Guid playerId,
            Guid? seasonId,
            SeasonRewardsService rewards,
            CancellationToken ct) =>
        {
            var dto = await rewards.GetEligibilityAsync(playerId, seasonId, ct);
            return Results.Ok(dto);
        });

    }
}
