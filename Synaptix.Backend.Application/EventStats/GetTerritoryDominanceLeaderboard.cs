using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.EventStats
{
    public sealed record GetTerritoryDominanceLeaderboard(Guid SeasonId, int TierNumber, int Top = 20)
        : IRequest<List<TerritoryDominanceDto>>;

    public sealed class GetTerritoryDominanceLeaderboardHandler(IAppDb db)
        : IRequestHandler<GetTerritoryDominanceLeaderboard, List<TerritoryDominanceDto>>
    {
        public async ValueTask<List<TerritoryDominanceDto>> Handle(GetTerritoryDominanceLeaderboard r, CancellationToken ct)
        {
            var top = Math.Clamp(r.Top, 1, 100);

            var rows = await db.TerritoryTiles
                .AsNoTracking()
                .Where(t => t.SeasonId == r.SeasonId && t.TierNumber == r.TierNumber && t.OwnerId.HasValue)
                .GroupBy(t => t.OwnerId!.Value)
                .Select(g => new TerritoryDominanceDto(
                    g.Key,
                    g.Count(),
                    g.Sum(t => t.XpMultiplierBps)))
                .OrderByDescending(x => x.TilesOwned)
                .ThenByDescending(x => x.TotalXpMultiplierBps)
                .Take(top)
                .ToListAsync(ct);

            return rows;
        }
    }
}
