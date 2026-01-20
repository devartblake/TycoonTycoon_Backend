using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons;

/// <summary>
/// Step 8.1: Freeze ranked standings, close season, then distribute rewards.
/// Idempotent by season state: if already closed, no-op.
/// </summary>
public sealed class SeasonCloseOrchestrator(
    IAppDb db,
    SeasonRewardJob rewards)
{
    public async Task<string> CloseAsync(Guid seasonId, CancellationToken ct)
    {
        var season = await db.Seasons.FirstOrDefaultAsync(s => s.Id == seasonId, ct);
        if (season is null) return "NotFound";

        // Idempotent: if already closed, do not resnapshot/reward.
        if (season.Status == SeasonStatus.Closed)
            return "AlreadyClosed";

        if (season.Status != SeasonStatus.Active)
            return "NotActive";

        // Capture timestamp for snapshot consistency
        var capturedAt = DateTimeOffset.UtcNow;

        // Remove any prior snapshot rows for this season (defensive; should not exist for Active)
        var existing = await db.SeasonRankSnapshots
            .Where(x => x.SeasonId == seasonId)
            .ToListAsync(ct);

        if (existing.Count > 0)
        {
            db.SeasonRankSnapshots.RemoveRange(existing);
            await db.SaveChangesAsync(ct);
        }

        // Freeze from current PlayerSeasonProfiles
        var profiles = await db.PlayerSeasonProfiles
            .AsNoTracking()
            .Where(p => p.SeasonId == seasonId)
            .ToListAsync(ct);

        foreach (var p in profiles)
        {
            db.SeasonRankSnapshots.Add(new SeasonRankSnapshotRow(p, capturedAt));
        }

        await db.SaveChangesAsync(ct);

        // Close season
        season.Close(capturedAt); // implement if you don't have it; see helper below
        await db.SaveChangesAsync(ct);

        // Distribute rewards (should use snapshot at close time)
        await rewards.RunAsync(seasonId, ct);

        return "Closed";
    }
}
