using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Economy;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record ReviveInGameEvent(Guid EventId, Guid GameEventId, Guid PlayerId) : IRequest<ReviveResponse>;

    public sealed class ReviveInGameEventHandler(IAppDb db, EconomyService econ) : IRequestHandler<ReviveInGameEvent, ReviveResponse>
    {
        public async Task<ReviveResponse> Handle(ReviveInGameEvent r, CancellationToken ct)
        {
            var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);
            if (ev is null)
                return new ReviveResponse(r.EventId, "NotFound", 0);

            if (ev.Kind != "global_crown")
                return new ReviveResponse(r.EventId, "NotAllowed", 0);

            if (ev.Status != GameEventStatus.Live)
                return new ReviveResponse(r.EventId, "InvalidStatus", 0);

            var participant = await db.GameEventParticipants
                .FirstOrDefaultAsync(x => x.GameEventId == r.GameEventId && x.PlayerId == r.PlayerId, ct);

            if (participant is null)
                return new ReviveResponse(r.EventId, "NotParticipant", 0);

            if (participant.EliminatedAt is null)
                return new ReviveResponse(r.EventId, "NotEliminated", 0);

            var econResult = await econ.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: r.EventId,
                PlayerId: r.PlayerId,
                Kind: "game-event-revive",
                Lines: new[] { new EconomyLineDto(CurrencyType.Diamonds, -ev.ReviveCostGems) },
                Note: $"game-event:{r.GameEventId}:revive"
            ), ct);

            if (econResult.Status == EconomyTxnStatus.InsufficientFunds)
                return new ReviveResponse(r.EventId, "InsufficientFunds", participant.RevivesUsed);

            participant.EliminatedAt = null;
            participant.RevivesUsed++;
            await db.SaveChangesAsync(ct);

            return new ReviveResponse(r.EventId, "Revived", participant.RevivesUsed);
        }
    }
}
