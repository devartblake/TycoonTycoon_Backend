using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Application.Territory
{
    public sealed record GetPlayerTileMultiplier(Guid SeasonId, int TierNumber, Guid PlayerId) : IRequest<int>;

    public sealed class GetPlayerTileMultiplierHandler(IAppDb db) : IRequestHandler<GetPlayerTileMultiplier, int>
    {
        public async Task<int> Handle(GetPlayerTileMultiplier r, CancellationToken ct)
        {
            return await db.TerritoryTiles.AsNoTracking()
                .Where(x => x.SeasonId == r.SeasonId
                         && x.TierNumber == r.TierNumber
                         && x.OwnerId == r.PlayerId)
                .SumAsync(x => x.XpMultiplierBps, ct);
        }
    }
}
