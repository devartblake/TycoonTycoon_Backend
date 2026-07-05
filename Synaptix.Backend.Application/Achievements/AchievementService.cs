using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Achievements
{
    public sealed class AchievementService
    {
        private readonly IAppDb _db;

        public AchievementService(IAppDb db)
        {
            _db = db;
        }

        public async Task<AchievementCatalogDto> GetCatalogAsync(CancellationToken ct)
        {
            var items = await _db.Achievements.AsNoTracking()
                .OrderBy(x => x.Category).ThenBy(x => x.Points).ThenBy(x => x.Key)
                .Select(x => new AchievementDto(
                    x.Key, x.Title, x.Description, x.Category, x.Points, x.IconUrl, x.IsSecret))
                .ToListAsync(ct);

            return new AchievementCatalogDto(items);
        }

        public async Task<PlayerAchievementsDto> GetPlayerAsync(Guid playerId, CancellationToken ct)
        {
            var unlocked = await _db.PlayerAchievements.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.UnlockedAtUtc)
                .Select(x => new PlayerAchievementDto(x.AchievementKey, x.UnlockedAtUtc))
                .ToListAsync(ct);

            return new PlayerAchievementsDto(playerId, unlocked);
        }

        public async Task<UnlockAchievementResultDto> UnlockAsync(UnlockAchievementRequest req, CancellationToken ct)
        {
            var key = req.AchievementKey.Trim();

            var exists = await _db.Achievements.AsNoTracking().AnyAsync(x => x.Key == key, ct);
            if (!exists)
                return new UnlockAchievementResultDto(req.PlayerId, key, "NotFound", null);

            var already = await _db.PlayerAchievements.AsNoTracking()
                .Where(x => x.PlayerId == req.PlayerId && x.AchievementKey == key)
                .Select(x => (DateTimeOffset?)x.UnlockedAtUtc)
                .FirstOrDefaultAsync(ct);
            if (already is not null)
                return new UnlockAchievementResultDto(req.PlayerId, key, "Duplicate", already);

            var unlock = new PlayerAchievement(req.PlayerId, key);
            _db.PlayerAchievements.Add(unlock);
            try
            {
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Unique (PlayerId, AchievementKey) index — concurrent unlock lost the race.
                return new UnlockAchievementResultDto(req.PlayerId, key, "Duplicate", null);
            }

            return new UnlockAchievementResultDto(req.PlayerId, key, "Unlocked", unlock.UnlockedAtUtc);
        }

        // Admin seeding (idempotent by key)
        public async Task<int> UpsertAsync(IEnumerable<AchievementDto> achievements, CancellationToken ct)
        {
            var count = 0;

            foreach (var a in achievements)
            {
                var existing = await _db.Achievements.FirstOrDefaultAsync(x => x.Key == a.Key, ct);
                if (existing is null)
                {
                    _db.Achievements.Add(new Achievement(
                        a.Key, a.Title, a.Description, a.Category, a.Points, a.IconUrl, a.IsSecret));
                    count++;
                }
                else
                {
                    existing.Update(a.Title, a.Description, a.Category, a.Points, a.IconUrl, a.IsSecret);
                }
            }

            await _db.SaveChangesAsync(ct);
            return count;
        }
    }
}
