namespace Synaptix.Shared.Contracts.Realtime.Presence
{
    public sealed record PlayerPresenceEntry(Guid PlayerId, string Status);

    public sealed record PlayerPresenceSnapshotMessage(
        IReadOnlyList<PlayerPresenceEntry> OnlinePlayers,
        DateTimeOffset Timestamp);
}
