using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Application.EventStats;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;
using Synaptix.Shared.Contracts.Realtime.Guardians;

namespace Synaptix.Backend.Application.Guardians
{
    public sealed record ResolveGuardianChallenge(Guid MatchId) : IRequest;

    public sealed class ResolveGuardianChallengeHandler(
        IAppDb db,
        IEconomyService econ,
        IGuardianNotifier notifier,
        IOptions<GuardianOptions> opts,
        SeasonService seasonSvc,
        PlayerEventStatsService eventStats)
        : IRequestHandler<ResolveGuardianChallenge>
    {
        public async ValueTask<Unit> Handle(ResolveGuardianChallenge r, CancellationToken ct)
        {
            var challenge = await db.GuardianChallenges
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (challenge is null || challenge.Status != ChallengeStatus.Pending)
                return Unit.Value;

            // Determine winner from match result
            var result = await db.MatchResults
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (result is null) return Unit.Value;

            var parts = await db.MatchParticipantResults
                .AsNoTracking()
                .Where(x => x.MatchResultId == result.Id)
                .ToListAsync(ct);

            var challengerScore = parts.FirstOrDefault(x => x.PlayerId == challenge.ChallengerId)?.Score ?? 0;
            var guardianScore = parts.FirstOrDefault(x => x.PlayerId == challenge.GuardianId)?.Score ?? 0;

            var challengerWon = challengerScore > guardianScore;

            challenge.Status = challengerWon ? ChallengeStatus.ChallengersWin : ChallengeStatus.GuardiansWin;
            challenge.ResolvedAtUtc = DateTimeOffset.UtcNow;

            if (challengerWon)
            {
                // Remove old guardian row and install challenger
                var oldGuardian = await db.TierGuardians
                    .FirstOrDefaultAsync(x => x.SeasonId == challenge.SeasonId
                                           && x.TierNumber == challenge.TierNumber
                                           && x.PlayerId == challenge.GuardianId, ct);

                Guid? previousGuardianId = oldGuardian?.PlayerId;
                if (oldGuardian is not null)
                    db.TierGuardians.Remove(oldGuardian);

                var tomorrow = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(2);
                var newGuardian = new TierGuardian(
                    challenge.SeasonId,
                    challenge.TierNumber,
                    challenge.ChallengerId,
                    new DateTimeOffset(tomorrow, TimeSpan.Zero));

                db.TierGuardians.Add(newGuardian);

                // Promotion bonus
                var promotionEventId = DeterministicGuid(challenge.Id, challenge.ChallengerId);
                await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: promotionEventId,
                    PlayerId: challenge.ChallengerId,
                    Kind: "guardian-promotion",
                    Lines: new[] { new EconomyLineDto(CurrencyType.Xp, opts.Value.PromotionBonusXp) },
                    Note: $"guardian-challenge:{challenge.Id}"
                ), ct);

                // Update event stats: challenger promoted, previous guardian lost a defence
                var activeSeason = await seasonSvc.GetActiveAsync(ct);
                if (activeSeason is not null)
                {
                    var challengerStats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, challenge.ChallengerId, ct);
                    challengerStats.GuardianPromotions++;
                    challengerStats.UpdatedAtUtc = DateTimeOffset.UtcNow;

                    if (previousGuardianId.HasValue)
                    {
                        var guardianStats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, previousGuardianId.Value, ct);
                        guardianStats.GuardianDefencesLost++;
                        guardianStats.UpdatedAtUtc = DateTimeOffset.UtcNow;
                    }
                }

                await db.SaveChangesAsync(ct);

                await notifier.NotifyGuardianChangedAsync(new GuardianChangedMessage(
                    challenge.SeasonId, challenge.TierNumber, challenge.ChallengerId, previousGuardianId), ct);
            }
            else
            {
                // Guardian defended
                var guardian = await db.TierGuardians
                    .FirstOrDefaultAsync(x => x.SeasonId == challenge.SeasonId
                                           && x.TierNumber == challenge.TierNumber
                                           && x.PlayerId == challenge.GuardianId, ct);

                if (guardian is not null)
                    guardian.DefencesWon++;

                // Update event stats: guardian won a defence
                var activeSeason = await seasonSvc.GetActiveAsync(ct);
                if (activeSeason is not null)
                {
                    var guardianStats = await eventStats.GetOrCreateAsync(activeSeason.SeasonId, challenge.GuardianId, ct);
                    guardianStats.GuardianDefencesWon++;
                    guardianStats.UpdatedAtUtc = DateTimeOffset.UtcNow;
                }

                await db.SaveChangesAsync(ct);
            }
            return Unit.Value;
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
