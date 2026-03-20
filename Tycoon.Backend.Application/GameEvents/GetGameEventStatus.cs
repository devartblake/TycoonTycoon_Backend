using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.GameEvents
{
    public sealed record GetGameEventStatus(Guid GameEventId) : IRequest<GameEventStatusDto?>;

    public sealed class GetGameEventStatusHandler(IAppDb db) : IRequestHandler<GetGameEventStatus, GameEventStatusDto?>
    {
        public async Task<GameEventStatusDto?> Handle(GetGameEventStatus r, CancellationToken ct)
        {
            var ev = await db.GameEvents.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);

            if (ev is null) return null;

            var total = await db.GameEventParticipants.CountAsync(x => x.GameEventId == r.GameEventId, ct);
            var alive = await db.GameEventParticipants.CountAsync(x => x.GameEventId == r.GameEventId && x.EliminatedAt == null, ct);

            return new GameEventStatusDto(ev.Id, ev.Kind, ev.Status, ev.ScheduledAtUtc, total, alive, ev.JackpotPool);
        }
    }
}
