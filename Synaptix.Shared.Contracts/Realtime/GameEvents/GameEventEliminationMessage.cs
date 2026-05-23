namespace Synaptix.Shared.Contracts.Realtime.GameEvents
{
    public sealed record GameEventEliminationMessage(
        Guid GameEventId,
        Guid PlayerId,
        int SurvivorsRemaining,
        DateTimeOffset At
    );
}
