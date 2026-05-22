using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Seasons
{
    public sealed class SeasonPointsService(IAppDb db)
    {
        public async Task<ApplySeasonPointsResultDto> ApplyAsync(ApplySeasonPointsRequest req, CancellationToken ct)
        {
            var duplicate = await db.SeasonPointTransactions.AsNoTracking()
                .AnyAsync(x => x.EventId == req.EventId, ct);

            if (duplicate)
            {
                var profileDup = await GetOrCreateProfileAsync(req.SeasonId, req.PlayerId, 0, ct);
                return new ApplySeasonPointsResultDto(req.EventId, req.SeasonId, req.PlayerId, "Duplicate", profileDup.RankPoints);
            }

            var profile = await GetOrCreateProfileAsync(req.SeasonId, req.PlayerId, 0, ct);

            db.SeasonPointTransactions.Add(new SeasonPointTransaction(
                req.EventId, req.SeasonId, req.PlayerId, req.Kind, req.Delta, req.Note));

            profile.ApplyPoints(req.Delta);

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // Race duplicate - treat as duplicate
                var profileRace = await GetOrCreateProfileAsync(req.SeasonId, req.PlayerId, 0, ct);
                return new ApplySeasonPointsResultDto(req.EventId, req.SeasonId, req.PlayerId, "Duplicate", profileRace.RankPoints);
            }

            return new ApplySeasonPointsResultDto(req.EventId, req.SeasonId, req.PlayerId, "Applied", profile.RankPoints);
        }

        public async Task<SeasonPointHistoryDto> GetHistoryAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.SeasonPointTransactions.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new SeasonPointTxnListItemDto(
                    x.EventId, x.SeasonId, x.Kind, x.Delta, x.Note, x.CreatedAtUtc))
                .ToListAsync(ct);

            return new SeasonPointHistoryDto(playerId, page, pageSize, total, items);
        }

        public async Task<Season?> GetActiveSeasonAsync(CancellationToken ct)
        {
            return await db.Seasons.AsNoTracking().FirstOrDefaultAsync(x => x.Status == SeasonStatus.Active, ct);
        }

        private async Task<PlayerSeasonProfile> GetOrCreateProfileAsync(Guid seasonId, Guid playerId, int initialPoints, CancellationToken ct)
        {
            var existing = await db.PlayerSeasonProfiles
                .FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.PlayerId == playerId, ct);

            if (existing is not null) return existing;

            var created = new PlayerSeasonProfile(seasonId, playerId, initialPoints);
            db.PlayerSeasonProfiles.Add(created);
            await db.SaveChangesAsync(ct);
            return created;
        }
    }
}
