using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Seasons
{
    public sealed class TierAssignmentService(IAppDb db)
    {
        public async Task RecomputeAsync(Guid seasonId, int usersPerTier = 100, CancellationToken ct = default)
        {
            // Order by rank points desc, then updatedAt (stable-ish)
            var profiles = await db.PlayerSeasonProfiles
                .Where(x => x.SeasonId == seasonId)
                .OrderByDescending(x => x.RankPoints)
                .ThenBy(x => x.UpdatedAtUtc)
                .ToListAsync(ct);

            for (var i = 0; i < profiles.Count; i++)
            {
                var seasonRank = i + 1;
                var tier = ((seasonRank - 1) / usersPerTier) + 1;            // 1..N
                var tierRank = ((seasonRank - 1) % usersPerTier) + 1;       // 1..100

                profiles[i].SetRanks(tier, tierRank, seasonRank);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
