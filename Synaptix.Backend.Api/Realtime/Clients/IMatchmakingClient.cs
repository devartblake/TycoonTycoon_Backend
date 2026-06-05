using Synaptix.Shared.Contracts.Realtime.Matchmaking;

namespace Synaptix.Backend.Api.Realtime.Clients
{
    public interface IMatchmakingClient
    {
        Task Queued(MatchmakingQueuedMessage message);
        Task Matched(MatchmakingMatchedMessage message);
        Task Cancelled(MatchmakingCancelledMessage message);
    }
}
