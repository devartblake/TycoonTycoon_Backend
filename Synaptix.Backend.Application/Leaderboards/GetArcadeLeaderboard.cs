using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Leaderboards
{
    public sealed record GetArcadeLeaderboard(
        string GameId,
        string Difficulty,
        int Page = 1,
        int PageSize = 50,
        Guid? PlayerId = null
    ) : IRequest<ArcadeLeaderboardResponseDto>;

    public sealed class GetArcadeLeaderboardHandler(IAppDb db)
        : IRequestHandler<GetArcadeLeaderboard, ArcadeLeaderboardResponseDto>
    {
        public async ValueTask<ArcadeLeaderboardResponseDto> Handle(
            GetArcadeLeaderboard r,
            CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            // Base query: entries for this game+difficulty, ordered by score desc then duration asc
            var baseQuery =
                from e in db.ArcadeScores.AsNoTracking()
                where e.GameId == r.GameId && e.Difficulty == r.Difficulty
                orderby e.Score descending, e.DurationMs ascending
                select e;

            var total = await baseQuery.CountAsync(ct);

            // Get paginated entries with rank
            var entries = await baseQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            // Join with players to get usernames
            var playerIds = entries.Select(e => e.PlayerId).ToList();
            var players = await db.Players
                .AsNoTracking()
                .Where(p => playerIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Username, ct);

            var items = new List<ArcadeLeaderboardEntryDto>();
            var rank = (page - 1) * pageSize + 1;

            foreach (var entry in entries)
            {
                if (players.TryGetValue(entry.PlayerId, out var username))
                {
                    items.Add(new ArcadeLeaderboardEntryDto(
                        entry.PlayerId,
                        username,
                        entry.Score,
                        entry.DurationMs,
                        entry.AchievedAtUtc,
                        rank));
                    rank++;
                }
            }

            // If a player ID is provided and not on the current page, compute their rank/score
            int? myRank = null;
            int? myScore = null;

            if (r.PlayerId.HasValue)
            {
                // Check if they're already in the current page
                var onPage = items.Any(i => i.PlayerId == r.PlayerId);
                if (!onPage)
                {
                    var playerEntry = await db.ArcadeScores
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            e => e.PlayerId == r.PlayerId &&
                                 e.GameId == r.GameId &&
                                 e.Difficulty == r.Difficulty,
                            ct);

                    if (playerEntry is not null)
                    {
                        // Rank = 1 + count of entries that beat the player's entry
                        // (higher score wins; equal score ties break on lower duration).
                        var betterCount = await db.ArcadeScores
                            .AsNoTracking()
                            .Where(e => e.GameId == r.GameId &&
                                        e.Difficulty == r.Difficulty &&
                                        (e.Score > playerEntry.Score ||
                                         (e.Score == playerEntry.Score &&
                                          e.DurationMs < playerEntry.DurationMs)))
                            .CountAsync(ct);

                        myRank = betterCount + 1;
                        myScore = playerEntry.Score;
                    }
                }
                else
                {
                    // They're on the page
                    var playerOnPage = items.First(i => i.PlayerId == r.PlayerId);
                    myRank = playerOnPage.Rank;
                    myScore = playerOnPage.Score;
                }
            }

            return new ArcadeLeaderboardResponseDto(
                r.GameId,
                r.Difficulty,
                page,
                pageSize,
                total,
                items.AsReadOnly(),
                myRank,
                myScore);
        }
    }
}
