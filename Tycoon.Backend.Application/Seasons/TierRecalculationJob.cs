using Microsoft.Extensions.Logging;
using Tycoon.Backend.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons
{
    /// <summary>
    /// Hangfire-scheduled job that recomputes tier rankings for all active seasons.
    /// Register as a recurring job (e.g. every 5 minutes) so tier ranks stay fresh
    /// without blocking the match submission hot path.
    ///
    /// Example Hangfire registration in Program.cs / startup:
    ///   RecurringJob.AddOrUpdate&lt;TierRecalculationJob&gt;(
    ///       "tier-recompute",
    ///       job => job.RunAsync(CancellationToken.None),
    ///       "*/5 * * * *");
    /// </summary>
    public sealed class TierRecalculationJob
    {
        private readonly TierAssignmentService _tiers;
        private readonly IAppDb _db;
        private readonly ILogger<TierRecalculationJob> _logger;

        public TierRecalculationJob(
            TierAssignmentService tiers,
            IAppDb db,
            ILogger<TierRecalculationJob> logger)
        {
            _tiers = tiers;
            _db = db;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            var activeSeason = await _db.Seasons
                .AsNoTracking()
                .Where(s => s.Status == SeasonStatus.Active)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (activeSeason == default)
            {
                _logger.LogDebug("TierRecalculationJob: no active season found, skipping.");
                return;
            }

            _logger.LogInformation("TierRecalculationJob: recomputing tiers for season {SeasonId}.", activeSeason);

            await _tiers.RecomputeAsync(activeSeason, usersPerTier: 100, ct: ct);

            _logger.LogInformation("TierRecalculationJob: completed for season {SeasonId}.", activeSeason);
        }
    }
}