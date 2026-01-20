using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Seasons;

public sealed class SeasonRolloverJob(IAppDb db, SeasonService seasons)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var active = await seasons.GetActiveAsync(ct);
        if (active is null) 
            return;

        // If your Season uses non-nullable EndsAtUtc, treat default as "no end configured"
        if (active.EndsAtUtc == default)
            return;

        if (active.EndsAtUtc <= DateTimeOffset.UtcNow)
        {
            // mark inactive / closed
            var season = await db.Seasons.FindAsync(new object?[] { active.SeasonId }, ct);
            if (season is null)
                return;

            season.Close(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);

            // Optionally create next season here (or admin-created)
        }
    }
}
