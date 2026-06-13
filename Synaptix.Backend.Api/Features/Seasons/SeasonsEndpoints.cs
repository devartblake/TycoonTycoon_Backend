using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Seasons
{
    public static class SeasonsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/seasons").WithTags("Seasons");

            g.MapGet("/active", async (SeasonService svc, CancellationToken ct) =>
            {
                var s = await svc.GetActiveAsync(ct);
                return s is null ? Results.NotFound() : Results.Ok(s);
            });

            g.MapGet("/state/{playerId:guid}", async (
                [FromRoute] Guid playerId,
                SeasonService seasons,
                IAppDb db,
                CancellationToken ct) =>
            {
                var active = await seasons.GetActiveAsync(ct);
                if (active is null) return Results.NotFound();

                var profile = await db.PlayerSeasonProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.SeasonId == active.SeasonId && x.PlayerId == playerId, ct);

                if (profile is null)
                {
                    return Results.Ok(new PlayerSeasonStateDto(playerId, active.SeasonId, 0, 0, 0, 0, 0, 1, 0, 0));
                }

                return Results.Ok(new PlayerSeasonStateDto(
                    profile.PlayerId,
                    profile.SeasonId,
                    profile.RankPoints,
                    profile.Wins,
                    profile.Losses,
                    profile.Draws,
                    profile.MatchesPlayed,
                    profile.Tier,
                    profile.TierRank,
                    profile.SeasonRank
                ));
            });
        }
    }
}
