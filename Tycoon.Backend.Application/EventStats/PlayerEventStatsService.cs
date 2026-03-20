using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.EventStats
{
    /// <summary>
    /// Provides get-or-create access to a player's per-season event stats row.
    /// Call SaveChangesAsync on the IAppDb after mutating the returned object.
    /// </summary>
    public sealed class PlayerEventStatsService(IAppDb db)
    {
        public async Task<PlayerEventStats> GetOrCreateAsync(Guid seasonId, Guid playerId, CancellationToken ct)
        {
            var stats = await db.PlayerEventStats
                .FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId, ct);

            if (stats is null)
            {
                stats = new PlayerEventStats(seasonId, playerId);
                db.PlayerEventStats.Add(stats);
            }

            return stats;
        }
    }
}
