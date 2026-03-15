using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;
using Tycoon.Shared.Contracts.Realtime.Guardians;

namespace Tycoon.Backend.Application.Guardians
{
    public sealed record ResolveGuardianChallenge(Guid MatchId) : IRequest;

    public sealed class ResolveGuardianChallengeHandler(
        IAppDb db,
        EconomyService econ,
        IGuardianNotifier notifier,
        IOptions<GuardianOptions> opts)
        : IRequestHandler<ResolveGuardianChallenge>
    {
        public async Task Handle(ResolveGuardianChallenge r, CancellationToken ct)
        {
            var challenge = await db.GuardianChallenges
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (challenge is null || challenge.Status != ChallengeStatus.Pending)
                return;

            // Determine winner from match result
            var result = await db.MatchResults
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.MatchId == r.MatchId, ct);

            if (result is null) return;

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
