namespace Synaptix.Shared.Contracts.Dtos
{
    public enum ChallengeStatus
    {
        Pending = 1,
        ChallengersWin = 2,
        GuardiansWin = 3
    }

    public sealed record TierGuardianDto(
        Guid Id,
        Guid SeasonId,
        int TierNumber,
        Guid PlayerId,
        DateTimeOffset AssignedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        int DefencesWon,
        int DefencesLost
    );

    public sealed record ChallengeGuardianRequest(
        Guid EventId,
        Guid SeasonId,
        int TierNumber,
        Guid ChallengerId,
        Guid GuardianId
    );

    public sealed record ChallengeGuardianResponse(
        Guid EventId,
        string Status,
        Guid MatchId
    );
}
