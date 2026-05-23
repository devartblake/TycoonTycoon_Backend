using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.EventStats
{
    public sealed record GetEventSeasonLeaderboard(
        Guid SeasonId,
        string SortBy = "event_wins",
        int Page = 1,
        int PageSize = 50) : IRequest<List<EventSeasonLeaderboardEntryDto>>;

    public sealed class GetEventSeasonLeaderboardHandler(IAppDb db)
        : IRequestHandler<GetEventSeasonLeaderboard, List<EventSeasonLeaderboardEntryDto>>
    {
        public async Task<List<EventSeasonLeaderboardEntryDto>> Handle(GetEventSeasonLeaderboard r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 200);

            var baseQuery = db.PlayerEventStats
                .AsNoTracking()
                .Where(s => s.SeasonId == r.SeasonId);

            IQueryable<PlayerEventStats> sorted = r.SortBy switch
            {
                "events_entered" => baseQuery.OrderByDescending(s => s.EventsEntered),
                "guardian_defences" => baseQuery.OrderByDescending(s => s.GuardianDefencesWon),
                "tiles_owned" => baseQuery.OrderByDescending(s => s.CurrentTilesOwned),
                _ => baseQuery.OrderByDescending(s => s.EventsWon).ThenByDescending(s => s.EventsTop20)
            };

            var rows = await sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return rows.Select(s => new EventSeasonLeaderboardEntryDto(
                s.PlayerId,
                s.EventsWon,
                s.EventsTop20,
                s.EventsEntered,
                s.GuardianDefencesWon,
                s.GuardianDaysTotal,
                s.CurrentTilesOwned,
                s.PeakXpMultiplierBps)).ToList();
        }
    }
}
