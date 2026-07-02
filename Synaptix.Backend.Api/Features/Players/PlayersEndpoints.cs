using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Players;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Players
{
    public static class PlayersEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var g = app.MapGroup("/players").WithTags("Players");

            g.MapPost("/", async ([FromBody] CreatePlayerRequest req, AppDb db, CancellationToken ct) =>
            {
                var p = new Player(req.Username, string.IsNullOrWhiteSpace(req.CountryCode) ? "US" : req.CountryCode);
                db.Players.Add(p);
                await db.SaveChangesAsync(ct);

                return Results.Created($"/players/{p.Id}",
                    new PlayerDto(p.Id, p.Username, p.CountryCode, p.Level, p.Xp));
            });

            // Must be registered before /{id:guid} to avoid route ambiguity
            g.MapGet("/me", GetOrCreateMyPlayer)
                .WithName("GetMyPlayer")
                .RequireAuthorization();

            g.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetPlayerById(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            g.MapGet("/{id:guid}/stats", GetCareerStats);
        }

        private static async Task<IResult> GetCareerStats(
            Guid id,
            IAppDb db,
            CancellationToken ct)
        {
            // Verify player exists
            var playerExists = await db.Players
                .AsNoTracking()
                .AnyAsync(p => p.Id == id, ct);

            if (!playerExists)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Player not found.");

            var participations = db.MatchParticipantResults
                .AsNoTracking()
                .Where(p => p.PlayerId == id);

            var totalMatches = await participations.CountAsync(ct);

            if (totalMatches == 0)
            {
                return Results.Ok(new PlayerCareerStatsDto(
                    PlayerId: id,
                    TotalMatches: 0,
                    Wins: 0,
                    Losses: 0,
                    WinRate: 0,
                    TotalCorrect: 0,
                    TotalWrong: 0,
                    AvgScore: 0,
                    AvgAnswerTimeMs: 0
                ));
            }

            // Aggregate stats from all match participations
            var stats = await participations
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalCorrect = g.Sum(p => p.Correct),
                    TotalWrong = g.Sum(p => p.Wrong),
                    AvgScore = g.Average(p => (double)p.Score),
                    AvgAnswerTimeMs = g.Average(p => p.AvgAnswerTimeMs)
                })
                .FirstAsync(ct);

            // Count wins: player had the highest score in their match result
            var wins = await db.MatchParticipantResults
                .AsNoTracking()
                .Where(p => p.PlayerId == id)
                .Where(p => !db.MatchParticipantResults
                    .Any(o => o.MatchResultId == p.MatchResultId && o.Score > p.Score))
                .CountAsync(ct);

            var losses = totalMatches - wins;
            var winRate = Math.Round((double)wins / totalMatches * 100, 1);

            return Results.Ok(new PlayerCareerStatsDto(
                PlayerId: id,
                TotalMatches: totalMatches,
                Wins: wins,
                Losses: losses,
                WinRate: winRate,
                TotalCorrect: stats.TotalCorrect,
                TotalWrong: stats.TotalWrong,
                AvgScore: Math.Round(stats.AvgScore, 1),
                AvgAnswerTimeMs: Math.Round(stats.AvgAnswerTimeMs, 1)
            ));
        }

        private static async Task<IResult> GetOrCreateMyPlayer(
            HttpContext httpContext,
            AppDb db,
            CancellationToken ct)
        {
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");

            if (claim is null || !Guid.TryParse(claim.Value, out var userId) || userId == Guid.Empty)
                return Results.Unauthorized();

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
                return Results.Unauthorized();

            var player = await db.Players
                .FirstOrDefaultAsync(p => p.Username == user.Handle, ct);

            if (player is null)
            {
                player = new Player(user.Handle, user.Country ?? "US");
                db.Players.Add(player);
                await db.SaveChangesAsync(ct);
            }

            // Get player wallet for economy data
            var wallet = await db.PlayerWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.PlayerId == player.Id, ct);

            // Get user roles
            var userRoles = await db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleName)
                .ToListAsync(ct);

            // Extract preferences from Flags dictionary
            user.Flags.TryGetValue("age_group", out var ageGroupObj);
            user.Flags.TryGetValue("synaptix_mode", out var synaptixModeObj);
            user.Flags.TryGetValue("pref_home_surface", out var prefSurfaceObj);
            user.Flags.TryGetValue("reduced_motion", out var reducedMotionObj);
            user.Flags.TryGetValue("tone_preference", out var toneObj);
            user.Flags.TryGetValue("is_premium", out var premiumObj);

            var profile = new CurrentUserProfileDto(
                UserId: user.Id,
                Username: user.Handle,
                Email: user.Email,
                DisplayName: user.Handle,
                Country: user.Country,
                AvatarUrl: user.AvatarUrl,
                UserRole: user.SystemRole,
                UserRoles: userRoles.Count > 0 ? userRoles.AsReadOnly() : null,
                PlayerId: player.Id,
                PlayerLevel: player.Level,
                PlayerXp: player.Xp,
                PlayerScore: player.Score,
                CurrentTierId: player.TierId?.ToString(),
                Coins: wallet?.Coins ?? 0,
                Diamonds: wallet?.Diamonds ?? 0,
                CumulativeXp: wallet?.Xp ?? 0,
                IsPremium: premiumObj is bool premium && premium,
                AgeGroup: ageGroupObj?.ToString(),
                SynaptixMode: synaptixModeObj?.ToString(),
                PreferredHomeSurface: prefSurfaceObj?.ToString(),
                ReducedMotion: reducedMotionObj is bool reduced && reduced,
                TonePreference: toneObj?.ToString(),
                IsAnonymous: user.IsAnonymous,
                CreatedAt: user.CreatedAt,
                LastLoginAt: user.LastLoginAt
            );

            return Results.Ok(profile);
        }
    }
}
