using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Tiers
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
