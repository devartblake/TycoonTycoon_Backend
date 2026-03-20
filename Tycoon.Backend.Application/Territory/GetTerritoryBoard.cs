using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Territory
{
    public sealed record GetTerritoryBoard(Guid SeasonId, int TierNumber) : IRequest<TerritoryBoardDto>;

    public sealed class GetTerritoryBoardHandler(IAppDb db) : IRequestHandler<GetTerritoryBoard, TerritoryBoardDto>
    {
        public async Task<TerritoryBoardDto> Handle(GetTerritoryBoard r, CancellationToken ct)
        {
            var tiles = await db.TerritoryTiles.AsNoTracking()
                .Where(x => x.SeasonId == r.SeasonId && x.TierNumber == r.TierNumber)
                .Select(x => new TerritoryTileDto(x.Category, x.OwnerId, x.XpMultiplierBps))
                .ToListAsync(ct);

            return new TerritoryBoardDto(r.SeasonId, r.TierNumber, tiles);
        }
    }
}
