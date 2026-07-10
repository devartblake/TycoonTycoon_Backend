using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Application.EventStats;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Contracts.Realtime.GameEvents;

namespace Synaptix.Backend.Application.GameEvents
{
    public sealed record CloseGameEventAndDistributePrizes(Guid GameEventId) : IRequest<CloseGameEventResponse>;

    public sealed class CloseGameEventAndDistributePrizesHandler(
        IAppDb db,
        IEconomyService econ,
        IGameEventNotifier notifier,
        SeasonService seasonSvc,
        PlayerEventStatsService eventStats)
        : IRequestHandler<CloseGameEventAndDistributePrizes, CloseGameEventResponse>
    {
        private const int Top20BonusXp = 200;
        private const int Top20BonusCoins = 100;
        private const int WinnerBonusXp = 500;
        private const int WinnerBonusCoins = 250;

        public async ValueTask<CloseGameEventResponse> Handle(CloseGameEventAndDistributePrizes r, CancellationToken ct)
        {
            var ev = await db.GameEvents.FirstOrDefaultAsync(x => x.Id == r.GameEventId, ct);
            if (ev is null || ev.Status == GameEventStatus.Closed)
                return new CloseGameEventResponse(r.GameEventId, 0, 0);

            var participants = await db.GameEventParticipants
                .Where(x => x.GameEventId == r.GameEventId)
                .ToListAsync(ct);

            // Assign ranks: survivors first (by entry order), then eliminated (last eliminated = highest among eliminated)
            var survivors = participants.Where(x => x.EliminatedAt == null).ToList();
            var eliminated = participants.Where(x => x.EliminatedAt.HasValue)
                .OrderByDescending(x => x.EliminatedAt)
                .ToList();

            // Champion vs Tier is asymmetric: if the seeded champion survived,
            // they defended the crown and take rank 1. If they were dethroned
            // (eliminated), the last surviving challenger wins — the default
            // survivors-first ordering already yields that.
            if (ev.Kind == GameEvent.ChampionVsTierKind && ev.ChampionPlayerId is Guid championId)
            {
                var champion = survivors.FirstOrDefault(x => x.PlayerId == championId);
                if (champion is not null)
                {
                    survivors.Remove(champion);
                    survivors.Insert(0, champion);
                }
            }

            var ranked = survivors.Concat(eliminated).ToList();
            for (int i = 0; i < ranked.Count; i++)
                ranked[i].FinalRank = i + 1;

            // Sponsor multiplier is applied to the jackpot at payout time.
            var effectiveJackpot = ev.EffectiveJackpot;
            int jackpotDistributed = 0;

            // Distribute prizes to top 20
            foreach (var p in ranked.Take(20))
            {
                int xp = p.FinalRank == 1 ? WinnerBonusXp : Top20BonusXp;
                int coins = p.FinalRank == 1 ? WinnerBonusCoins : Top20BonusCoins;

                // Rank 1 in a jackpot event takes the (multiplied) jackpot.
                if (p.FinalRank == 1 && ev.FeedsJackpot)
                {
                    coins += effectiveJackpot;
                    jackpotDistributed = effectiveJackpot;
                }

                var prizeEventId = DeterministicGuid(ev.Id, p.PlayerId);

                // Idempotency: skip if already claimed
                var alreadyClaimed = await db.GameEventPrizeClaims
                    .AnyAsync(x => x.EventId == prizeEventId, ct);
                if (alreadyClaimed) continue;

                await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: prizeEventId,
                    PlayerId: p.PlayerId,
                    Kind: "game-event-prize",
                    Lines: new[]
                    {
                        new EconomyLineDto(CurrencyType.Xp, xp),
                        new EconomyLineDto(CurrencyType.Coins, coins)
                    },
                    Note: $"game-event:{ev.Id}:rank:{p.FinalRank}"
                ), ct);

                db.GameEventPrizeClaims.Add(new GameEventPrizeClaim(
                    ev.Id, p.PlayerId, prizeEventId, xp, coins, p.FinalRank!.Value));
            }

            // Update per-player event stats for top-20 winners
            var activeSeason = await seasonSvc.GetActiveAsync(ct);
            if (activeSeason is not null)
            {
                foreach (var p in ranked.Take(20))
                {
                    int xp = p.FinalRank == 1 ? WinnerBonusXp : Top20BonusXp;
                    int coins = p.FinalRank == 1 ? WinnerBonusCoins : Top20BonusCoins;
                    if (p.FinalRank == 1 && ev.FeedsJackpot)
                        coins += effectiveJackpot;

                    var stats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, p.PlayerId, ct);
                    stats.EventsTop20++;
                    if (p.FinalRank == 1) stats.EventsWon++;
                    stats.TotalEventXpEarned += xp;
                    stats.TotalEventCoinsEarned += coins;
                    stats.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }
            }

            ev.Close(DateTimeOffset.UtcNow, participants.Count);
            await db.SaveChangesAsync(ct);

            await notifier.NotifyEventClosedAsync(new GameEventClosedMessage(
                ev.Id, ev.Kind, participants.Count, jackpotDistributed), ct);

            return new CloseGameEventResponse(ev.Id, participants.Count, jackpotDistributed);
        }

        private static Guid DeterministicGuid(Guid a, Guid b)
        {
            Span<byte> bytes = stackalloc byte[32];
            a.TryWriteBytes(bytes[..16]);
            b.TryWriteBytes(bytes[16..]);
            Span<byte> result = stackalloc byte[16];
            for (int i = 0; i < 16; i++)
                result[i] = (byte)(bytes[i] ^ bytes[i + 16]);
            return new Guid(result);
        }
    }
}
