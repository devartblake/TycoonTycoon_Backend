using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record ListUpcomingGameEvents(int? TierNumber) : IRequest<List<GameEventSummaryDto>>;

    public sealed class ListUpcomingGameEventsHandler(IAppDb db) : IRequestHandler<ListUpcomingGameEvents, List<GameEventSummaryDto>>
    {
        private static readonly GameEventStatus[] ActiveStatuses =
        {
            GameEventStatus.Scheduled,
            GameEventStatus.Open,
            GameEventStatus.Live
        };

        public async ValueTask<List<GameEventSummaryDto>> Handle(ListUpcomingGameEvents r, CancellationToken ct)
        {
            var query = db.GameEvents.AsNoTracking()
                .Where(x => ActiveStatuses.Contains(x.Status));

            if (r.TierNumber.HasValue)
                query = query.Where(x => x.TierId == 0 || x.TierId == r.TierNumber.Value);

            return await query
                .OrderBy(x => x.ScheduledAtUtc)
                .Select(x => new GameEventSummaryDto(x.Id, x.Kind, x.TierId, x.Status, x.ScheduledAtUtc, x.EntryFeeCoins, x.MaxParticipants))
                .ToListAsync(ct);
        }
    }
}
