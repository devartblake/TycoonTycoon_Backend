using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Seasons;

public sealed class RankedTierMovementService(
    IAppDb db,
    IOptions<RankedSeasonOptions> opt)
{
    public async Task ApplyMovementsAsync(Guid seasonId, CancellationToken ct)
    {
        var o = opt.Value;
        var now = DateTimeOffset.UtcNow;

        // Profiles with computed TierRank should exist post-recompute.
        // Assume you store TierId/TierRank on PlayerSeasonProfile or in a joined table.
        // If you store ranks elsewhere, adapt selection accordingly.
        var profiles = await db.PlayerSeasonProfiles
            .Where(p => p.SeasonId == seasonId)
            .ToListAsync(ct);

        foreach (var p in profiles)
        {
            // Skip if still in placement
            if (p.PlacementMatchesCompleted < o.PlacementMatchesRequired)
                continue;

            // Promotion eligibility
            if (p.TierRank <= o.PromotionEligibleRank && p.CanPromote(now, o.PromotionCooldownDays))
            {
                p.MarkPromoted(now);
                // Optional: mark a pending movement; actual tier change can happen on next recompute
                // p.PendingMovement = "Promote";
            }

            // Optional demotion logic: bottom N
            // if (p.TierRank >= 90 && p.CanDemote(now, o.DemotionCooldownDays)) { ... }
        }

        await db.SaveChangesAsync(ct);
    }
}
