namespace Synaptix.Shared.Contracts.Realtime.Leaderboard
{
    public sealed record LeaderboardSnapshotEntry(
        Guid PlayerId,
        string Handle,
        string CountryCode,
        int Score,
        int TierRank,
        int GlobalRank);

    public sealed record LeaderboardRankChangedMessage(
        Guid PlayerId,
        int TierId,
        int OldRank,
        int NewRank,
        int NewScore,
        DateTimeOffset Timestamp);

    public sealed record LeaderboardSnapshotMessage(
        int TierId,
        IReadOnlyList<LeaderboardSnapshotEntry> Entries,
        DateTimeOffset SnapshotAtUtc);
}
