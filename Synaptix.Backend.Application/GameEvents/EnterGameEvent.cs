using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.EventStats;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record EnterGameEvent(Guid EventId, Guid GameEventId, Guid PlayerId) : IRequest<EnterGameEventResponse>;

    public sealed class EnterGameEventHandler(IAppDb db, IPlayerTransactionService ptxnSvc, SeasonService seasonSvc, PlayerEventStatsService eventStats, FeatureFlagService flags) : IRequestHandler<EnterGameEvent, EnterGameEventResponse>
    {
        public async ValueTask<EnterGameEventResponse> Handle(EnterGameEvent r, CancellationToken ct)
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

            // Use PlayerTransaction to atomically debit entry fee + create participant
            var currencyChanges = new List<PlayerTransactionCurrencyDto>();

            if (ev.EntryFeeCoins > 0)
            {
                currencyChanges.Add(new PlayerTransactionCurrencyDto(
                    r.PlayerId,
                    new[] { new EconomyLineDto(CurrencyType.Coins, -ev.EntryFeeCoins) }
                ));
            }

            var ptxnResult = await ptxnSvc.ExecuteAsync(new CreatePlayerTransactionRequest(
                EventId: r.EventId,
                Kind: "game-event-entry",
                CorrelatedEventId: r.GameEventId,
                Actors: new[] { new PlayerTransactionActorDto(r.PlayerId, "buyer") },
                CurrencyChanges: currencyChanges.Count > 0 ? currencyChanges : null,
                Note: $"game-event:{r.GameEventId}"
            ), ct);

            if (ptxnResult.Status == "Duplicate")
                return new EnterGameEventResponse(r.EventId, "Duplicate");

            if (ptxnResult.Status == "InsufficientFunds")
                return new EnterGameEventResponse(r.EventId, "InsufficientFunds");

            if (ptxnResult.Status == "Failed")
                return new EnterGameEventResponse(r.EventId, "Failed");

            if (ev.FeedsJackpot)
                ev.AddToJackpot(ev.EntryFeeCoins);

            var participant = new GameEventParticipant(r.GameEventId, r.PlayerId, r.EventId);
            db.GameEventParticipants.Add(participant);

            var activeSeason = await seasonSvc.GetActiveAsync(ct);
            if (activeSeason is not null)
            {
                var stats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, r.PlayerId, ct);
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
