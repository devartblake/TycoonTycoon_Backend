using Mediator;
using Microsoft.AspNetCore.Builder;
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
        public static void Map(WebApplication app)
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

            return Results.Ok(new PlayerDto(player.Id, player.Username, player.CountryCode, player.Level, player.Xp));
        }
    }
}
