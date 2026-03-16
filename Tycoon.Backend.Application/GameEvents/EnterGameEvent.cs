using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Application.EventStats;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.GameEvents
{
    public sealed record EnterGameEvent(Guid EventId, Guid GameEventId, Guid PlayerId) : IRequest<EnterGameEventResponse>;

    public sealed class EnterGameEventHandler(IAppDb db, EconomyService econ, SeasonService seasonSvc, PlayerEventStatsService eventStats, FeatureFlagService flags) : IRequestHandler<EnterGameEvent, EnterGameEventResponse>
    {
        private const int ChampionBattleEliminationIncrement = 50;

        public async Task<EnterGameEventResponse> Handle(EnterGameEvent r, CancellationToken ct)
        {
            if (!await flags.IsEnabledAsync(FeatureFlagService.GameEventsEnabled, ct))
                return new EnterGameEventResponse(r.EventId, "FeatureDisabled");

            var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);
            if (ev is null)
                return new EnterGameEventResponse(r.EventId, "NotFound");

            if (ev.Status != GameEventStatus.Open)
                return new EnterGameEventResponse(r.EventId, "InvalidStatus");

            var alreadyIn = await db.GameEventParticipants
                .AnyAsync(x => x.GameEventId == r.GameEventId && x.PlayerId == r.PlayerId, ct);
            if (alreadyIn)
                return new EnterGameEventResponse(r.EventId, "Duplicate");

            if (ev.EntryFeeCoins > 0)
            {
                var econResult = await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: r.EventId,
                    PlayerId: r.PlayerId,
                    Kind: "game-event-entry",
                    Lines: new[] { new EconomyLineDto(CurrencyType.Coins, -ev.EntryFeeCoins) },
                    Note: $"game-event:{r.GameEventId}"
                ), ct);

                if (econResult.Status == EconomyTxnStatus.InsufficientFunds)
                    return new EnterGameEventResponse(r.EventId, "InsufficientFunds");
            }

            if (ev.Kind == "champion_battle")
                ev.AddToJackpot(ev.EntryFeeCoins);

            var participant = new GameEventParticipant(r.GameEventId, r.PlayerId, r.EventId);
            db.GameEventParticipants.Add(participant);

            var activeSeason = await seasonSvc.GetActiveAsync(ct);
            if (activeSeason is not null)
            {
                var stats = await eventStats.GetOrCreateAsync(activeSeason.Id, r.PlayerId, ct);
                stats.EventsEntered++;
                stats.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return new EnterGameEventResponse(r.EventId, "Duplicate");
            }

            return new EnterGameEventResponse(r.EventId, "Entered");
        }
    }
}
