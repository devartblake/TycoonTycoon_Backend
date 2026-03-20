using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Application.EventStats;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Shared.Contracts.Dtos;
using Tycoon.Shared.Contracts.Realtime.Territory;

namespace Tycoon.Backend.Application.Territory
{
    public sealed record ResolveTerritoryDuel(Guid MatchId) : IRequest;

    public sealed class ResolveTerritoryDuelHandler(
        IAppDb db,
        EconomyService econ,
        ITerritoryNotifier notifier,
        SeasonService seasonSvc,
        PlayerEventStatsService eventStats)
        : IRequestHandler<ResolveTerritoryDuel>
    {
        private const int BaseMultiplierBps = 200;
        private const int PerTileIncrementBps = 100;
        private const int MaxMultiplierBps = 1000;
        private const int TerritoryCaptureBonusXp = 100;

        public async Task Handle(ResolveTerritoryDuel r, CancellationToken ct)
        {
            var duel = await db.TerritoryDuels
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (duel is null || duel.Outcome.HasValue)
                return;

            var result = await db.MatchResults.AsNoTracking()
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (result is null) return;

            var parts = await db.MatchParticipantResults.AsNoTracking()
                .Where(x => x.MatchResultId == result.Id)
                .ToListAsync(ct);

            var challengerScore = parts.FirstOrDefault(x => x.PlayerId == duel.ChallengerId)?.Score ?? 0;
            var defenderScore = duel.DefenderId.HasValue
                ? parts.FirstOrDefault(x => x.PlayerId == duel.DefenderId)?.Score ?? 0
                : -1; // unclaimed: challenger always wins

            TerritoryDuelOutcome outcome;
            if (challengerScore > defenderScore)
                outcome = TerritoryDuelOutcome.ChallengerWon;
            else if (challengerScore < defenderScore)
                outcome = TerritoryDuelOutcome.DefenderWon;
            else
                outcome = TerritoryDuelOutcome.Draw;

            duel.Outcome = outcome;
            duel.ResolvedAtUtc = DateTimeOffset.UtcNow;

            // Update tile ownership
            var tile = await db.TerritoryTiles
                .FirstOrDefaultAsync(x => x.SeasonId == duel.SeasonId
                                       && x.TierNumber == duel.TierNumber
                                       && x.Category == duel.Category, ct);

            if (outcome == TerritoryDuelOutcome.ChallengerWon && tile is not null)
            {
                tile.OwnerId = duel.ChallengerId;
                tile.CapturedAtUtc = DateTimeOffset.UtcNow;

                // Recalculate multipliers for all tiles owned by challenger in this tier
                var ownedTiles = await db.TerritoryTiles
                    .Where(x => x.SeasonId == duel.SeasonId
                             && x.TierNumber == duel.TierNumber
                             && x.OwnerId == duel.ChallengerId)
                    .ToListAsync(ct);

                // Include the just-captured tile
                int totalOwned = ownedTiles.Count;
                int newMultiplierBps = Math.Min(MaxMultiplierBps, BaseMultiplierBps + (totalOwned - 1) * PerTileIncrementBps);

                foreach (var t in ownedTiles)
                    t.XpMultiplierBps = newMultiplierBps;

                // Award capture bonus
                var captureEventId = DeterministicGuid(duel.Id, duel.ChallengerId);
                await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: captureEventId,
                    PlayerId: duel.ChallengerId,
                    Kind: "territory-capture",
                    Lines: new[] { new EconomyLineDto(CurrencyType.Xp, TerritoryCaptureBonusXp) },
                    Note: $"territory:{tile.Id}"
                ), ct);

                // Update event stats for challenger
                var activeSeason = await seasonSvc.GetActiveAsync(ct);
                if (activeSeason is not null)
                {
                    var stats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, duel.ChallengerId, ct);
                    stats.TilesEverCaptured++;
                    stats.CurrentTilesOwned = totalOwned;
                    if (newMultiplierBps > stats.PeakXpMultiplierBps)
                        stats.PeakXpMultiplierBps = newMultiplierBps;
                    stats.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                await db.SaveChangesAsync(ct);

                await notifier.NotifyTileCapturedAsync(new TerritoryCaptureMesage(
                    duel.SeasonId, duel.TierNumber, duel.Category, duel.ChallengerId, newMultiplierBps), ct);
            }
            else
            {
                await db.SaveChangesAsync(ct);
            }
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
