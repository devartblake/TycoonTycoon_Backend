using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons
{
    public sealed class SeasonService(IAppDb db)
    {
        public async Task<SeasonDto> CreateAsync(CreateSeasonRequest req, CancellationToken ct)
        {
            var season = new Season(req.SeasonNumber, req.Name, req.StartsAtUtc, req.EndsAtUtc);
            db.Seasons.Add(season);
            await db.SaveChangesAsync(ct);

            return ToDto(season);
        }

        public async Task<SeasonDto?> ActivateAsync(ActivateSeasonRequest req, CancellationToken ct)
        {
            var season = await db.Seasons.FirstOrDefaultAsync(x => x.Id == req.SeasonId, ct);
            if (season is null) return null;

            // Only one active season at a time
            var active = await db.Seasons.Where(x => x.Status == SeasonStatus.Active).ToListAsync(ct);
            foreach (var s in active)
                s.Close(DateTimeOffset.UtcNow); // close any prior active season defensively

            season.Activate();
            await db.SaveChangesAsync(ct);

            return ToDto(season);
        }

        public async Task<(SeasonDto? closed, SeasonDto? next)> CloseAsync(CloseSeasonRequest req, CancellationToken ct)
        {
            var season = await db.Seasons.FirstOrDefaultAsync(x => x.Id == req.SeasonId, ct);
            if (season is null) return (null, null);

            season.Close(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(ct);

            SeasonDto? nextDto = null;

            if (req.CreateNextSeason)
            {
                var nextSeasonNumber = season.SeasonNumber + 1;
                var next = new Season(nextSeasonNumber, req.NextSeasonName ?? $"Season {nextSeasonNumber}",
                    startsAtUtc: DateTimeOffset.UtcNow,
                    endsAtUtc: DateTimeOffset.UtcNow.AddDays(30))
                {
                };

                next.Activate(); // immediately active by default
                db.Seasons.Add(next);
                await db.SaveChangesAsync(ct);

                // Carryover: create next profiles from previous season profiles
                var pct = Math.Clamp(req.CarryoverPercent, 0, 100);

                var prevProfiles = await db.PlayerSeasonProfiles
                    .Where(x => x.SeasonId == season.Id)
                    .ToListAsync(ct);

                foreach (var p in prevProfiles)
                {
                    var carry = (int)Math.Floor(p.RankPoints * (pct / 100.0));
                    var exists = await db.PlayerSeasonProfiles.AnyAsync(x => x.SeasonId == next.Id && x.PlayerId == p.PlayerId, ct);
                    if (!exists)
                        db.PlayerSeasonProfiles.Add(new PlayerSeasonProfile(next.Id, p.PlayerId, carry));
                }

                await db.SaveChangesAsync(ct);
                nextDto = ToDto(next);
            }

            return (ToDto(season), nextDto);
        }

        public async Task<SeasonListResponseDto> ListAsync(int page, int pageSize, CancellationToken ct)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var q = db.Seasons.AsNoTracking().OrderByDescending(x => x.SeasonNumber);
            var total = await q.CountAsync(ct);

            var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
                .Select(x => new SeasonDto(x.Id, x.SeasonNumber, x.Name, x.Status, x.StartsAtUtc, x.EndsAtUtc))
                .ToListAsync(ct);

            return new SeasonListResponseDto(page, pageSize, total, items);
        }

        public async Task<SeasonDto?> GetActiveAsync(CancellationToken ct)
        {
            var s = await db.Seasons.AsNoTracking().FirstOrDefaultAsync(x => x.Status == SeasonStatus.Active, ct);
            return s is null ? null : new SeasonDto(s.Id, s.SeasonNumber, s.Name, s.Status, s.StartsAtUtc, s.EndsAtUtc);
        }

        private static SeasonDto ToDto(Season s) =>
            new(s.Id, s.SeasonNumber, s.Name, s.Status, s.StartsAtUtc, s.EndsAtUtc);
    }
}
