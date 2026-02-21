using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Mobile.Seasons
{
    public static class MobileSeasonsEndpoints
    {
        public static void Map(RouteGroupBuilder mobile)
        {
            var g = mobile.MapGroup("/seasons")
                .WithTags("Mobile/Seasons")
                .WithOpenApi();

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
