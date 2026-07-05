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

    public sealed record MyGuardianStatusDto(
        Guid PlayerId,
        bool IsGuardian,
        int? Tier,
        int DefenceCount, // successful defences (DefencesWon)
        Guid? CurrentMatchId // pending challenge match against this guardian, if any
    );
}
