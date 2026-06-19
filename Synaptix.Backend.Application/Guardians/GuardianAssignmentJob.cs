using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Config;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Application.EventStats;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Guardians
{
    public sealed class GuardianAssignmentJob(
        IAppDb db,
        IEconomyService econ,
        IOptions<GuardianOptions> opts,
        ILogger<GuardianAssignmentJob> logger,
        PlayerEventStatsService eventStats,
        FeatureFlagService featureFlags)
    {
        public async Task RunAsync(CancellationToken ct)
        {
            if (!await featureFlags.IsEnabledAsync(FeatureFlagService.GuardianEnabled, ct))
            {
                logger.LogInformation("GuardianAssignmentJob: guardian_enabled=false, skipping.");
                return;
            }

            var activeSeason = await db.Seasons
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Status == SeasonStatus.Active, ct);

            if (activeSeason is null)
            {
                logger.LogInformation("GuardianAssignmentJob: no active season, skipping.");
                return;
            }

            var tiers = await db.Tiers.AsNoTracking().OrderBy(x => x.Order).ToListAsync(ct);
            var tomorrow = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(2);
            var expiresAt = new DateTimeOffset(tomorrow, TimeSpan.Zero);
            var now = DateTimeOffset.UtcNow;

            foreach (var tier in tiers)
            {
                var tierNum = tier.Order;

                // Find top N players in this tier by rank points
                var topProfiles = await db.PlayerSeasonProfiles
                    .AsNoTracking()
                    .Where(x => x.SeasonId == activeSeason.Id && x.Tier == tierNum)
                    .OrderByDescending(x => x.RankPoints)
                    .Take(opts.Value.MaxGuardiansPerTier)
                    .ToListAsync(ct);

                // Remove expired guardians for this tier
                var expiredGuardians = await db.TierGuardians
                    .Where(x => x.SeasonId == activeSeason.Id && x.TierNumber == tierNum && x.ExpiresAtUtc <= now)
                    .ToListAsync(ct);

                foreach (var eg in expiredGuardians)
                    db.TierGuardians.Remove(eg);

                // Upsert guardians for top players
                var existingGuardians = await db.TierGuardians
                    .Where(x => x.SeasonId == activeSeason.Id && x.TierNumber == tierNum)
                    .ToListAsync(ct);

                var existingPlayerIds = existingGuardians.Select(g => g.PlayerId).ToHashSet();

                foreach (var profile in topProfiles)
                {
                    if (!existingPlayerIds.Contains(profile.PlayerId))
                    {
                        var newGuardian = new TierGuardian(activeSeason.Id, tierNum, profile.PlayerId, expiresAt);
                        db.TierGuardians.Add(newGuardian);
                        logger.LogInformation("Assigned guardian for player {PlayerId} in tier {Tier}", profile.PlayerId, tier.Order);
                    }
                    else
                    {
                        // Extend existing guardian expiry
                        var existing = existingGuardians.First(g => g.PlayerId == profile.PlayerId);
                        existing.ExpiresAtUtc = expiresAt;
                    }
                }
            }

            await db.SaveChangesAsync(ct);

            // Award daily passive to all active guardians (idempotent via DeterministicGuid)
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var allActiveGuardians = await db.TierGuardians
                .Where(x => x.SeasonId == activeSeason.Id && x.ExpiresAtUtc > now)
                .ToListAsync(ct);

            foreach (var guardian in allActiveGuardians)
            {
                // Build a deterministic but unique Guid for guardian + day
                var dayBytes = new byte[16];
                var dayNumber = today.DayNumber;
                dayBytes[0] = (byte)(dayNumber >> 24);
                dayBytes[1] = (byte)(dayNumber >> 16);
                dayBytes[2] = (byte)(dayNumber >> 8);
                dayBytes[3] = (byte)dayNumber;
                var dayGuid = new Guid(dayBytes);
                var dailyEventId = DeterministicGuid(guardian.Id, dayGuid);

                var result = await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: dailyEventId,
                    PlayerId: guardian.PlayerId,
                    Kind: "guardian-passive",
                    Lines: new[]
                    {
                        new EconomyLineDto(CurrencyType.Coins, opts.Value.PassiveCoins),
                        new EconomyLineDto(CurrencyType.Xp, opts.Value.PassiveXp)
                    },
                    Note: $"guardian:{guardian.Id}:day:{today}"
                ), ct);

                if (result.Status == EconomyTxnStatus.Applied)
                {
                    guardian.PassiveCoins += opts.Value.PassiveCoins;
                    guardian.PassiveXp += opts.Value.PassiveXp;
                }

                // Increment guardian day counter (idempotent: only runs when economy txn is newly applied)
                if (result.Status == EconomyTxnStatus.Applied)
                {
                    var stats = await eventStats.GetOrCreateAsync(activeSeason.Id, guardian.PlayerId, ct);
                    stats.GuardianDaysTotal++;
                    stats.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            await db.SaveChangesAsync(ct);
            logger.LogInformation("GuardianAssignmentJob completed for season {SeasonId}", activeSeason.Id);
        }

        private static Guid DeterministicGuid(Guid a, Guid b)
        {
            Span<byte> bytes = stackalloc byte[32];
            a.TryWriteBytes(bytes[..16]);
            b.TryWriteBytes(bytes[16..]);
            Span<byte> result = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
                result[i] = (byte)(bytes[i] ^ bytes[i + 16]);
            return new Guid(result);
        }
    }
}
