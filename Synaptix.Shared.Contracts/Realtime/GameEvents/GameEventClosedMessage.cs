namespace Synaptix.Shared.Contracts.Realtime.GameEvents
{
    public sealed record GameEventClosedMessage(
        Guid GameEventId,
        string Kind,
        int TotalParticipants,
        int JackpotDistributed
    );
}
