namespace Synaptix.Shared.Contracts.Realtime.Presence
{
    public sealed record PlayerPresenceChangedMessage(
        Guid PlayerId,
        string Status,
        DateTimeOffset Timestamp);
}
