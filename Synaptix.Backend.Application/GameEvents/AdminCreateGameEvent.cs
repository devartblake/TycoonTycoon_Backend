using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record AdminCreateGameEvent(CreateGameEventRequest Request) : IRequest<GameEventSummaryDto>;

    public sealed class AdminCreateGameEventHandler(IAppDb db) : IRequestHandler<AdminCreateGameEvent, GameEventSummaryDto>
    {
        private static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase)
        {
            "millionaire", "global_crown", GameEvent.ChampionBattleKind, GameEvent.ChampionVsTierKind
        };

        public async ValueTask<GameEventSummaryDto> Handle(AdminCreateGameEvent r, CancellationToken ct)
        {
            var req = r.Request;

            if (!ValidKinds.Contains(req.Kind))
                throw new ArgumentException($"Invalid game event kind: {req.Kind}");

            var ev = new GameEvent(
                kind: req.Kind.ToLowerInvariant(),
                tierId: req.TierId,
                scheduledAtUtc: req.ScheduledAtUtc,
                openAtUtc: req.OpenAtUtc,
                entryFeeCoins: req.EntryFeeCoins,
                reviveCostGems: req.ReviveCostGems,
                maxParticipants: req.MaxParticipants);

            db.GameEvents.Add(ev);
            await db.SaveChangesAsync(ct);

            return new GameEventSummaryDto(ev.Id, ev.Kind, ev.TierId, ev.Status, ev.ScheduledAtUtc, ev.EntryFeeCoins, ev.MaxParticipants);
        }
    }
}
