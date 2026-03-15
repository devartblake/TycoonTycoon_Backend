using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;
using Tycoon.Shared.Contracts.Realtime.GameEvents;

namespace Tycoon.Backend.Application.GameEvents
{
    public sealed record EliminateParticipant(Guid GameEventId, Guid PlayerId, DateTimeOffset At) : IRequest;

    public sealed class EliminateParticipantHandler(IAppDb db, IGameEventNotifier notifier)
        : IRequestHandler<EliminateParticipant>
    {
        private const int ChampionBattleEliminationIncrement = 50;

        public async Task Handle(EliminateParticipant r, CancellationToken ct)
        {
            var participant = await db.GameEventParticipants
                .FirstOrDefaultAsync(x => x.GameEventId == r.GameEventId && x.PlayerId == r.PlayerId, ct);

            if (participant is null || participant.EliminatedAt.HasValue)
                return;

            participant.EliminatedAt = r.At;

            var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);
            if (ev?.Kind == "champion_battle")
                ev.AddToJackpot(ChampionBattleEliminationIncrement);

            await db.SaveChangesAsync(ct);

            var survivors = await db.GameEventParticipants
                .CountAsync(x => x.GameEventId == r.GameEventId && x.EliminatedAt == null, ct);

            await notifier.NotifyEliminationAsync(new GameEventEliminationMessage(
                r.GameEventId, r.PlayerId, survivors, r.At), ct);
        }
    }
}
