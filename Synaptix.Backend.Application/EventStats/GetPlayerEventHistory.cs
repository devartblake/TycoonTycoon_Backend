using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.EventStats
{
    public sealed record GetPlayerEventHistory(Guid PlayerId, Guid? SeasonId, int Page = 1, int PageSize = 20)
        : IRequest<List<PlayerEventHistoryDto>>;

    public sealed class GetPlayerEventHistoryHandler(IAppDb db)
        : IRequestHandler<GetPlayerEventHistory, List<PlayerEventHistoryDto>>
    {
        public async ValueTask<List<PlayerEventHistoryDto>> Handle(GetPlayerEventHistory r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            // Base query: all participations for this player
            var participantQuery = db.GameEventParticipants
                .AsNoTracking()
                .Where(p => p.PlayerId == r.PlayerId);

            // Optionally scope to events within a season's date range
            IQueryable<GameEventParticipant> query;
            if (r.SeasonId.HasValue)
            {
                var season = await db.Seasons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == r.SeasonId.Value, ct);

                if (season is not null)
                {
                    query = from p in participantQuery
                            join ev in db.GameEvents on p.GameEventId equals ev.Id
                            where ev.ScheduledAtUtc >= season.StartsAtUtc && ev.ScheduledAtUtc < season.EndsAtUtc
                            select p;
                }
                else
                {
                    query = participantQuery;
                }
            }
            else
            {
                query = participantQuery;
            }

            var participants = await query
                .OrderByDescending(p => p.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var gameEventIds = participants.Select(p => p.GameEventId).Distinct().ToList();

            var events = await db.GameEvents
                .AsNoTracking()
                .Where(e => gameEventIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, ct);

            var claims = await db.GameEventPrizeClaims
                .AsNoTracking()
                .Where(c => c.PlayerId == r.PlayerId && gameEventIds.Contains(c.GameEventId))
                .ToDictionaryAsync(c => c.GameEventId, ct);

            return participants.Select(p =>
            {
                events.TryGetValue(p.GameEventId, out var ev);
                claims.TryGetValue(p.GameEventId, out var claim);
                return new PlayerEventHistoryDto(
                    p.GameEventId,
                    ev?.Kind ?? "unknown",
                    p.FinalRank,
                    claim?.AwardedXp ?? 0,
                    claim?.AwardedCoins ?? 0,
                    p.CreatedAtUtc);
            }).ToList();
        }
    }
}
