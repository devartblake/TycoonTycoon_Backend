using Tycoon.Backend.Domain.Primitives;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class GuardianChallenge : Entity
    {
        public Guid SeasonId { get; private set; }
        public int TierNumber { get; private set; }
        public Guid ChallengerId { get; private set; }
        public Guid GuardianId { get; private set; }
        public Guid MatchId { get; private set; }
        public ChallengeStatus Status { get; set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }
        public DateTimeOffset? ResolvedAtUtc { get; set; }

        private GuardianChallenge() { }

        public GuardianChallenge(
            Guid seasonId,
            int tierNumber,
            Guid challengerId,
            Guid guardianId,
            Guid matchId)
        {
            SeasonId = seasonId;
            TierNumber = tierNumber;
            ChallengerId = challengerId;
            GuardianId = guardianId;
            MatchId = matchId;
            Status = ChallengeStatus.Pending;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
