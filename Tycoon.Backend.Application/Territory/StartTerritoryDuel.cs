using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.Matches;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Territory
{
    public sealed record StartTerritoryDuel(
        Guid EventId,
        Guid SeasonId,
        int TierNumber,
        string Category,
        Guid ChallengerId,
        Guid? GameEventId = null) : IRequest<StartTerritoryDuelResponse>;

    public sealed class StartTerritoryDuelHandler(IAppDb db, IMediator mediator, FeatureFlagService flags)
        : IRequestHandler<StartTerritoryDuel, StartTerritoryDuelResponse>
    {
        public async Task<StartTerritoryDuelResponse> Handle(StartTerritoryDuel r, CancellationToken ct)
        {
            if (!await flags.IsEnabledAsync(FeatureFlagService.TerritoryEnabled, ct))
                return new StartTerritoryDuelResponse(Guid.Empty, null, "FeatureDisabled");
            // Load or create tile
            var tile = await db.TerritoryTiles
                .FirstOrDefaultAsync(x => x.SeasonId == r.SeasonId
                                       && x.TierNumber == r.TierNumber
                                       && x.Category == r.Category, ct);

            if (tile is null)
            {
                tile = new TerritoryTile(r.SeasonId, r.TierNumber, r.Category);
                db.TerritoryTiles.Add(tile);
                await db.SaveChangesAsync(ct);
            }

            if (tile.OwnerId == r.ChallengerId)
                return new StartTerritoryDuelResponse(Guid.Empty, tile.OwnerId);

            // Start a territory_duel match
            var startResult = await mediator.Send(new StartMatch(r.ChallengerId, "territory_duel"), ct);

            var duel = new TerritoryDuel(
                r.SeasonId, r.TierNumber, r.Category,
                r.ChallengerId, tile.OwnerId, startResult.MatchId, r.GameEventId);

            db.TerritoryDuels.Add(duel);
            await db.SaveChangesAsync(ct);

            return new StartTerritoryDuelResponse(startResult.MatchId, tile.OwnerId);
        }
    }
}
