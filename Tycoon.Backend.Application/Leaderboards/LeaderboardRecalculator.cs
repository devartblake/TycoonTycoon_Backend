using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Leaderboards
{
    /// <summary>
    /// Recomputes global rank and tier rank using population-based tiering:
    /// - Exactly 100 players per tier (except the last tier if not full).
    /// - TierRank is 1..100 within a tier.
    /// - GlobalRank is 1..N overall.
    ///
    /// Writes:
    /// - Player.TierId (Guid) mapped to Tier entity
    /// - LeaderboardEntry rows (TierId = Tier.Order) + ranks + xp progress
    /// </summary>
    public sealed class LeaderboardRecalculator
    {
        public const int TierCapacity = 100;

        private readonly IAppDb _db;

        public LeaderboardRecalculator(IAppDb db)
        {
            _db = db;
        }

        public async Task<LeaderboardRecalcResultDto> RecalculateAsync(CancellationToken ct)
        {
            // Load tiers ordered by progression
            var tiers = await _db.Tiers
                .OrderBy(t => t.Order)
                .ToListAsync(ct);

            // If no tiers exist, create a minimal tier set (10 by default).
            // You can tune names later to Bronze/Silver/etc.
            if (tiers.Count == 0)
            {
                tiers = await SeedDefaultTiersAsync(ct);
            }

            // Load all players ordered by score desc (then level desc, then created asc for determinism)
            var players = await _db.Players
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.Level)
                .ThenBy(p => p.CreatedAt)
                .ToListAsync(ct);

            // Load existing leaderboard entries in one query for upsert
            var existingEntries = await _db.LeaderboardEntries
                .ToDictionaryAsync(x => x.PlayerId, ct);

            var now = DateTimeOffset.UtcNow;

            int upserted = 0;
            int globalRank = 0;

            // Ensure enough tiers for population
            var neededTierCount = (int)Math.Ceiling(players.Count / (double)TierCapacity);
            if (neededTierCount > tiers.Count)
            {
                await EnsureTierCountAsync(tiers, neededTierCount, ct);
                // reload with new tiers included
                tiers = await _db.Tiers.OrderBy(t => t.Order).ToListAsync(ct);
            }

            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                globalRank = i + 1;

                var tierIndex = (globalRank - 1) / TierCapacity;           // 0-based tier index
                var tierRank = ((globalRank - 1) % TierCapacity) + 1;      // 1..100

                var tier = tiers[tierIndex];

                // Update Player tier (Guid FK to Tier)
                if (p.TierId != tier.Id)
                {
                    p.SetTier(tier.Id);
                }

                // Compute XP progress: 0..1 of the current level bar
                // Your domain curve is: XP to level up = Level*100
                var denom = Math.Max(1, p.Level * 100);
                var xpProgress = Math.Clamp(p.Xp / denom, 0.0, 1.0);

                // Upsert LeaderboardEntry (uses Tier.Order as the public TierId int)
                if (!existingEntries.TryGetValue(p.Id, out var e))
                {
                    e = new LeaderboardEntry(p.Id, tier.Order, p.Score, xpProgress);
                    e.SetRanks(globalRank, tierRank);
                    _db.LeaderboardEntries.Add(e);
                    existingEntries[p.Id] = e;
                    upserted++;
                }
                else
                {
                    // Update snapshot fields
                    // (LeaderboardEntry has UpdateScore(delta); we set absolute by delta)
                    var delta = p.Score - e.Score;
                    if (delta != 0) e.UpdateScore(delta);

                    // Update tier + xp progress + ranks
                    if (e.TierId != tier.Order)
                    {
                        // There is no setter; re-create logic would be noisy.
                        // If you prefer immutability, add a SetTierId method.
                        // For now: reflect via ranks method + re-instantiation is not desired.
                        // Add a setter method to LeaderboardEntry below.
                    }

                    e.SetRanks(globalRank, tierRank);

                    // Need a setter to update xp progress and tier id in-place.
                    // Implemented below via patch to LeaderboardEntry.
                    e.SetTierSnapshot(tier.Order, p.Score, xpProgress);

                    upserted++;
                }
            }

            await _db.SaveChangesAsync(ct);

            return new LeaderboardRecalcResultDto(
                PlayersProcessed: players.Count,
                TiersUsed: neededTierCount,
                LeaderboardEntriesUpserted: upserted,
                RecalculatedAtUtc: now
            );
        }

        private async Task<List<Tier>> SeedDefaultTiersAsync(CancellationToken ct)
        {
            // Minimal deterministic 10-tier seed if DB is empty
            var names = new[]
            {
                "Bronze", "Silver", "Gold", "Platinum", "Diamond",
                "Master", "Grandmaster", "Legend", "Mythic", "Titan"
            };

            var tiers = new List<Tier>();
            for (int i = 0; i < names.Length; i++)
            {
                // Min/MaxScore are legacy fields; keep them inert.
                var t = new Tier(names[i], order: i + 1, minScore: 0, maxScore: int.MaxValue);
                tiers.Add(t);
                _db.Tiers.Add(t);
            }

            await _db.SaveChangesAsync(ct);
            return tiers;
        }

        private async Task EnsureTierCountAsync(List<Tier> tiers, int neededCount, CancellationToken ct)
        {
            var current = tiers.Count;
            for (int i = current; i < neededCount; i++)
            {
                var order = i + 1;
                var t = new Tier($"Tier {order}", order, 0, int.MaxValue);
                _db.Tiers.Add(t);
            }
            await _db.SaveChangesAsync(ct);
        }
    }
}
