using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons
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
