using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record EliminateParticipant(Guid GameEventId, Guid PlayerId, DateTimeOffset At) : IRequest;

    public sealed class EliminateParticipantHandler(IAppDb db, IGameEventNotifier notifier)
        : IRequestHandler<EliminateParticipant>
    {
        private const int EliminationJackpotIncrement = 50;

        public async ValueTask<Unit> Handle(EliminateParticipant r, CancellationToken ct)
        {
            var participant = await db.GameEventParticipants
                .FirstOrDefaultAsync(x => x.GameEventId == r.GameEventId && x.PlayerId == r.PlayerId, ct);

            if (participant is null || participant.EliminatedAt.HasValue)
                return Unit.Value;

            participant.EliminatedAt = r.At;

            var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);
            if (ev?.FeedsJackpot == true)
                ev.AddToJackpot(EliminationJackpotIncrement);

            await db.SaveChangesAsync(ct);

            var survivors = await db.GameEventParticipants
                .CountAsync(x => x.GameEventId == r.GameEventId && x.EliminatedAt == null, ct);

            await notifier.NotifyEliminationAsync(new GameEventEliminationMessage(
                r.GameEventId, r.PlayerId, survivors, r.At), ct);
            return Unit.Value;
        }
    }
}
