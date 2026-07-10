using Microsoft.Extensions.Logging;

namespace Synaptix.Backend.Application.Seasons;

/// <summary>
/// Hangfire-recurring sweep that resolves overdue tiebreakers by the
/// deterministic standings order so no-shows can't hold up final snapshots
/// and reward payouts indefinitely.
/// </summary>
public sealed class SeasonTiebreakerExpiryJob(
    SeasonTiebreakerService tiebreakers,
    ILogger<SeasonTiebreakerExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var expired = await tiebreakers.ExpireOverdueAsync(ct);
        if (expired > 0)
            logger.LogInformation("SeasonTiebreakerExpiryJob resolved {Count} overdue tiebreaker(s).", expired);
    }
}
