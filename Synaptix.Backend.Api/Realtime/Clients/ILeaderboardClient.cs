using Synaptix.Shared.Contracts.Realtime.Leaderboard;

namespace Synaptix.Backend.Api.Realtime.Clients
{
    public interface ILeaderboardClient
    {
        Task RankChanged(LeaderboardRankChangedMessage message);
        Task LeaderboardSnapshot(LeaderboardSnapshotMessage message);
    }
}
