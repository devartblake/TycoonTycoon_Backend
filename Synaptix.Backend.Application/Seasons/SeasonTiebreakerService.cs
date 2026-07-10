using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Seasons;

/// <summary>
/// Season tie-breaker lifecycle: detection at season close, deferral of the
/// tied players' final snapshot rows and rewards, resolution (match result,
/// expiry fallback, admin action), and finalization.
/// </summary>
public sealed class SeasonTiebreakerService(
    IAppDb db,
    SeasonRewardJob rewards,
    IOptions<SeasonTiebreakerOptions> options)
{
    /// <summary>
    /// Detect rank-point ties at the championship spot and at tier reward
    /// boundaries (SeasonRewardRule.MaxTierRank) for a season being closed.
    /// Creates Scheduled tiebreakers, notifies the players, and returns the
    /// set of player ids whose snapshot rows must be deferred.
    /// </summary>
    public async Task<IReadOnlySet<Guid>> DetectAndScheduleAsync(Guid seasonId, CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.Enabled)
            return new HashSet<Guid>();

        // Standings order shared with the public leaderboard.
        var profiles = await db.PlayerSeasonProfiles.AsNoTracking()
            .Where(x => x.SeasonId == seasonId)
            .OrderByDescending(x => x.RankPoints)
            .ThenByDescending(x => x.Wins)
            .ThenBy(x => x.MatchesPlayed)
            .ThenBy(x => x.PlayerId)
            .ToListAsync(ct);

        if (profiles.Count < 2)
            return new HashSet<Guid>();

        var rules = await db.SeasonRewardRules.AsNoTracking().ToListAsync(ct);

        var groups = new List<(string Scope, int Tier, int BoundaryRank, int Points, List<Guid> Players)>();

        // Championship tie: two or more players sharing the top point total.
        var topPoints = profiles[0].RankPoints;
        if (topPoints > 0)
        {
            var top = profiles.Where(x => x.RankPoints == topPoints).Select(x => x.PlayerId).ToList();
            if (top.Count >= 2)
                groups.Add((SeasonTiebreaker.Scopes.Top1, profiles[0].Tier, 1, topPoints, top));
        }

        // Reward-boundary ties inside each tier: the player holding the last
        // rewarded rank shares points with the first unrewarded one.
        foreach (var rule in rules.Where(r => r.MaxTierRank > 0))
        {
            var tierProfiles = profiles.Where(x => x.Tier == rule.Tier).ToList();
            if (tierProfiles.Count <= rule.MaxTierRank)
                continue; // nobody misses out — no contested boundary

            var atBoundary = tierProfiles[rule.MaxTierRank - 1];
            var firstOut = tierProfiles[rule.MaxTierRank];
            if (atBoundary.RankPoints <= 0 || atBoundary.RankPoints != firstOut.RankPoints)
                continue;

            var tied = tierProfiles
                .Where(x => x.RankPoints == atBoundary.RankPoints)
                .Select(x => x.PlayerId)
                .ToList();

            groups.Add((SeasonTiebreaker.Scopes.TierPromotion, rule.Tier, rule.MaxTierRank, atBoundary.RankPoints, tied));
        }

        var deferred = new HashSet<Guid>();
        if (groups.Count == 0)
            return deferred;

        var now = DateTimeOffset.UtcNow;
        var scheduledAt = now + opts.ScheduleDelay;
        var expiresAt = scheduledAt + opts.ExpiryGrace;

        var seen = new List<HashSet<Guid>>();
        foreach (var (scope, tier, boundaryRank, pts, players) in groups)
        {
            // Oversized groups resolve deterministically at close — a mass tie
            // is noise, and scheduling a 20-player match helps nobody.
            if (players.Count > opts.MaxGroupSize)
                continue;

            // The same player set can surface from multiple rules; one match settles it.
            var set = players.ToHashSet();
            if (seen.Any(s => s.SetEquals(set)))
                continue;
            seen.Add(set);

            var tiebreaker = new SeasonTiebreaker(
                seasonId, scope, tier, boundaryRank, pts, players, scheduledAt, expiresAt);
            db.SeasonTiebreakers.Add(tiebreaker);

            foreach (var playerId in players)
            {
                deferred.Add(playerId);
                db.PlayerNotifications.Add(new PlayerNotification(
                    playerId,
                    type: "season_tiebreaker",
                    title: "Tie-breaker match scheduled!",
                    body: scope == SeasonTiebreaker.Scopes.Top1
                        ? "You are tied for the season championship. Win the tie-breaker to claim it."
                        : "You are tied at a reward boundary. Win the tie-breaker to secure your rank.",
                    actionRoute: "/season/tiebreaker",
                    payloadJson: $"{{\"tiebreakerId\":\"{tiebreaker.Id}\",\"seasonId\":\"{seasonId}\",\"scheduledAtUtc\":\"{scheduledAt:O}\",\"expiresAtUtc\":\"{expiresAt:O}\"}}",
                    icon: "emoji_events",
                    avatarUrl: null));
            }
        }

        await db.SaveChangesAsync(ct);
        return deferred;
    }

    /// <summary>Pending (scheduled/in-progress) tiebreakers that include the player.</summary>
    public async Task<List<SeasonTiebreaker>> GetPendingForPlayerAsync(Guid playerId, CancellationToken ct)
    {
        return await db.SeasonTiebreakers.AsNoTracking()
            .Where(x => (x.Status == SeasonTiebreaker.Statuses.Scheduled
                         || x.Status == SeasonTiebreaker.Statuses.InProgress)
                        && x.PlayerIds.Contains(playerId))
            .OrderBy(x => x.ScheduledAtUtc)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Resolve a pending tiebreaker with an explicit winner (match result or
    /// admin decision) and finalize the deferred snapshot rows and rewards.
    /// </summary>
    public async Task<SeasonTiebreaker?> ResolveAsync(
        Guid tiebreakerId, Guid winnerPlayerId, Guid? matchId, string? note, CancellationToken ct)
    {
        var tiebreaker = await db.SeasonTiebreakers
            .FirstOrDefaultAsync(x => x.Id == tiebreakerId, ct);
        if (tiebreaker is null || !tiebreaker.IsPending)
            return null;
        if (!tiebreaker.PlayerIds.Contains(winnerPlayerId))
            return null;

        tiebreaker.Resolve(winnerPlayerId, matchId, note);
        await FinalizeAsync(tiebreaker, winnerPlayerId, ct);
        return tiebreaker;
    }

    /// <summary>
    /// Expiry sweep: any pending tiebreaker past its expiry resolves by the
    /// deterministic standings order (wins, then fewest matches, then id).
    /// </summary>
    public async Task<int> ExpireOverdueAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var overdue = await db.SeasonTiebreakers
            .Where(x => (x.Status == SeasonTiebreaker.Statuses.Scheduled
                         || x.Status == SeasonTiebreaker.Statuses.InProgress)
                        && x.ExpiresAtUtc <= now)
            .ToListAsync(ct);

        foreach (var tiebreaker in overdue)
        {
            var fallback = await DeterministicWinnerAsync(tiebreaker, ct);
            tiebreaker.Expire(fallback, "expired: resolved by standings order");
            await FinalizeAsync(tiebreaker, fallback, ct);
        }

        return overdue.Count;
    }

    /// <summary>
    /// Cancel a pending tiebreaker (ops action). The deferral still has to be
    /// finalized so the tied players get snapshot rows and rewards — the
    /// deterministic order decides.
    /// </summary>
    public async Task<SeasonTiebreaker?> CancelAsync(Guid tiebreakerId, string? note, CancellationToken ct)
    {
        var tiebreaker = await db.SeasonTiebreakers
            .FirstOrDefaultAsync(x => x.Id == tiebreakerId, ct);
        if (tiebreaker is null || !tiebreaker.IsPending)
            return null;

        var fallback = await DeterministicWinnerAsync(tiebreaker, ct);
        tiebreaker.Cancel(fallback, string.IsNullOrWhiteSpace(note) ? "cancelled by operator" : note!.Trim());
        await FinalizeAsync(tiebreaker, fallback, ct);
        return tiebreaker;
    }

    private async Task<Guid> DeterministicWinnerAsync(SeasonTiebreaker tiebreaker, CancellationToken ct)
    {
        var profiles = await db.PlayerSeasonProfiles.AsNoTracking()
            .Where(x => x.SeasonId == tiebreaker.SeasonId && tiebreaker.PlayerIds.Contains(x.PlayerId))
            .ToListAsync(ct);

        var winner = profiles
            .OrderByDescending(x => x.Wins)
            .ThenBy(x => x.MatchesPlayed)
            .ThenBy(x => x.PlayerId)
            .FirstOrDefault();

        return winner?.PlayerId ?? tiebreaker.PlayerIds.Order().First();
    }

    /// <summary>
    /// Write the deferred snapshot rows (winner takes the best contested
    /// position) and re-run the final reward job, which is idempotent per
    /// (season, player) so already-rewarded players are unaffected.
    /// </summary>
    private async Task FinalizeAsync(SeasonTiebreaker tiebreaker, Guid winnerPlayerId, CancellationToken ct)
    {
        var profiles = await db.PlayerSeasonProfiles
            .Where(x => x.SeasonId == tiebreaker.SeasonId && tiebreaker.PlayerIds.Contains(x.PlayerId))
            .ToListAsync(ct);

        var winner = profiles.FirstOrDefault(x => x.PlayerId == winnerPlayerId);
        var bestHolder = profiles
            .OrderBy(x => x.SeasonRank <= 0 ? int.MaxValue : x.SeasonRank)
            .ThenBy(x => x.PlayerId)
            .FirstOrDefault();

        // Winner takes the group's best (tier, tierRank, seasonRank) triple;
        // the previous holder takes the winner's old triple. Everyone else
        // keeps their position.
        if (winner is not null && bestHolder is not null && bestHolder.PlayerId != winner.PlayerId)
        {
            var (bt, btr, bsr) = (bestHolder.Tier, bestHolder.TierRank, bestHolder.SeasonRank);
            bestHolder.SetRanks(winner.Tier, winner.TierRank, winner.SeasonRank);
            winner.SetRanks(bt, btr, bsr);
        }

        var capturedAt = DateTimeOffset.UtcNow;
        var alreadySnapshotted = await db.SeasonRankSnapshots.AsNoTracking()
            .Where(x => x.SeasonId == tiebreaker.SeasonId && tiebreaker.PlayerIds.Contains(x.PlayerId))
            .Select(x => x.PlayerId)
            .ToListAsync(ct);

        foreach (var profile in profiles)
        {
            if (alreadySnapshotted.Contains(profile.PlayerId))
                continue;
            db.SeasonRankSnapshots.Add(new SeasonRankSnapshotRow(profile, capturedAt));
        }

        foreach (var playerId in tiebreaker.PlayerIds)
        {
            var won = playerId == winnerPlayerId;
            db.PlayerNotifications.Add(new PlayerNotification(
                playerId,
                type: "season_tiebreaker_result",
                title: won ? "Tie-breaker won!" : "Tie-breaker decided",
                body: won
                    ? "You won the tie-breaker — your final season rank is locked in."
                    : "The tie-breaker has been decided. Your final season rank is locked in.",
                actionRoute: "/leaderboard",
                payloadJson: $"{{\"tiebreakerId\":\"{tiebreaker.Id}\",\"seasonId\":\"{tiebreaker.SeasonId}\",\"winnerPlayerId\":\"{winnerPlayerId}\",\"status\":\"{tiebreaker.Status}\"}}",
                icon: "emoji_events",
                avatarUrl: null));
        }

        await db.SaveChangesAsync(ct);

        // Deferred final rewards for the now-snapshotted players.
        await rewards.RunAsync(tiebreaker.SeasonId, ct);
    }
}
