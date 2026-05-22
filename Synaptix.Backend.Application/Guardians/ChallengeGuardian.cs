using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Matches;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Guardians
{
    public sealed record ChallengeGuardian(
        Guid EventId,
        Guid SeasonId,
        int TierNumber,
        Guid ChallengerId,
        Guid GuardianId) : IRequest<ChallengeGuardianResponse>;

    public sealed class ChallengeGuardianHandler(IAppDb db, IMediator mediator, FeatureFlagService flags)
        : IRequestHandler<ChallengeGuardian, ChallengeGuardianResponse>
    {
        public async Task<ChallengeGuardianResponse> Handle(ChallengeGuardian r, CancellationToken ct)
        {
            if (!await flags.IsEnabledAsync(FeatureFlagService.GuardianEnabled, ct))
                return new ChallengeGuardianResponse(r.EventId, "FeatureDisabled", Guid.Empty);

            var now = DateTimeOffset.UtcNow;

            // Verify guardian is active
            var guardian = await db.TierGuardians
                .FirstOrDefaultAsync(x => x.SeasonId == r.SeasonId
                                       && x.TierNumber == r.TierNumber
                                       && x.PlayerId == r.GuardianId
                                       && x.ExpiresAtUtc > now, ct);

            if (guardian is null)
                return new ChallengeGuardianResponse(r.EventId, "GuardianNotFound", Guid.Empty);

            // Guard: no open challenge between the same pair
            var openChallenge = await db.GuardianChallenges
                .AnyAsync(x => x.SeasonId == r.SeasonId
                            && x.TierNumber == r.TierNumber
                            && x.ChallengerId == r.ChallengerId
                            && x.GuardianId == r.GuardianId
                            && x.Status == ChallengeStatus.Pending, ct);

            if (openChallenge)
                return new ChallengeGuardianResponse(r.EventId, "ChallengeAlreadyPending", Guid.Empty);

            // Start a guardian_duel match
            var startResult = await mediator.Send(new StartMatch(r.ChallengerId, "guardian_duel"), ct);

            var challenge = new GuardianChallenge(
                r.SeasonId, r.TierNumber, r.ChallengerId, r.GuardianId, startResult.MatchId);

            db.GuardianChallenges.Add(challenge);
            await db.SaveChangesAsync(ct);

            return new ChallengeGuardianResponse(r.EventId, "Pending", startResult.MatchId);
        }
    }
}
