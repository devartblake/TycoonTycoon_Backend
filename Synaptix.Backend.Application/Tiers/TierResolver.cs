using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Application.Tiers
{
    /// <summary>
    /// Resolves a Tier based on a player's score using the seeded Tier ranges.
    /// </summary>
    public sealed class TierResolver
    {
        private readonly IAppDb _db;

        public TierResolver(IAppDb db)
        {
            _db = db;
        }

        public async Task<Tier?> ResolveForScoreAsync(int score, CancellationToken ct)
        {
            // Tier ranges are seeded and non-overlapping.
            return await _db.Tiers.AsNoTracking()
                .OrderBy(t => t.Order)
                .FirstOrDefaultAsync(t => score >= t.MinScore && score <= t.MaxScore, ct);
        }
    }
}
