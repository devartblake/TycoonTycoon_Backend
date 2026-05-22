namespace Synaptix.Shared.Contracts.Realtime.Guardians
{
    public sealed record GuardianChangedMessage(
        Guid SeasonId,
        int TierNumber,
        Guid NewGuardianId,
        Guid? PreviousGuardianId
    );
}
