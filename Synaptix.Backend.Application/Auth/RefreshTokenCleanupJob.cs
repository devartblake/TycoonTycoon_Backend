using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Application.Auth
{
    /// <summary>
    /// Hangfire-scheduled reaper for the RefreshTokens table. Rotation is a
    /// soft-revoke (revoked rows are kept so reuse detection can spot a replayed
    /// token), and expired tokens are never deleted inline, so without a sweep
    /// the table grows unbounded — especially once the access-token TTL is
    /// shortened and refreshes happen more often.
    ///
    /// Deletes tokens that are past a retention window and can no longer be used:
    /// fully expired tokens, and revoked tokens whose revocation is older than the
    /// window. The window keeps a forensic tail (and the reuse-detection grace
    /// window) intact. Runs in batches so a large backlog doesn't hold one long
    /// transaction.
    ///
    /// Register as a recurring job, e.g.:
    ///   RecurringJob.AddOrUpdate&lt;RefreshTokenCleanupJob&gt;(
    ///       "refresh-token-cleanup",
    ///       job => job.RunAsync(CancellationToken.None),
    ///       "30 3 * * *"); // daily, off-peak
    /// </summary>
    public sealed class RefreshTokenCleanupJob
    {
        private const int BatchSize = 500;

        private readonly IAppDb _db;
        private readonly ILogger<RefreshTokenCleanupJob> _logger;
        private readonly TimeSpan _retention;

        public RefreshTokenCleanupJob(
            IAppDb db,
            ILogger<RefreshTokenCleanupJob> logger,
            IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            var days = configuration.GetValue("Auth:RefreshTokenRetentionDays", 7);
            if (days < 1) days = 1;
            _retention = TimeSpan.FromDays(days);
        }

        // Test-friendly constructor with an explicit retention window.
        public RefreshTokenCleanupJob(
            IAppDb db,
            ILogger<RefreshTokenCleanupJob> logger,
            TimeSpan retention)
        {
            _db = db;
            _logger = logger;
            _retention = retention < TimeSpan.FromDays(1) ? TimeSpan.FromDays(1) : retention;
        }

        public async Task<int> RunAsync(CancellationToken ct)
        {
            var cutoff = DateTimeOffset.UtcNow - _retention;
            var totalDeleted = 0;

            while (!ct.IsCancellationRequested)
            {
                var batch = await _db.RefreshTokens
                    .Where(rt => rt.ExpiresAt < cutoff
                        || (rt.IsRevoked && rt.RevokedAt != null && rt.RevokedAt < cutoff))
                    .OrderBy(rt => rt.CreatedAt)
                    .Take(BatchSize)
                    .ToListAsync(ct);

                if (batch.Count == 0)
                    break;

                _db.RefreshTokens.RemoveRange(batch);
                await _db.SaveChangesAsync(ct);
                totalDeleted += batch.Count;

                if (batch.Count < BatchSize)
                    break;
            }

            if (totalDeleted > 0)
                _logger.LogInformation(
                    "RefreshTokenCleanupJob: deleted {Count} expired/revoked refresh token(s) older than {RetentionDays}d.",
                    totalDeleted, _retention.TotalDays);
            else
                _logger.LogDebug("RefreshTokenCleanupJob: nothing to delete.");

            return totalDeleted;
        }
    }
}
