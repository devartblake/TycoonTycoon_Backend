namespace Synaptix.Backend.Application.Matchmaking
{
    public interface IMatchmakingNotifier
    {
        Task NotifyMatchedAsync(Guid playerId, Guid opponentId, string mode, int tier, string scope, Guid ticketId, CancellationToken ct);
        Task NotifyCancelledAsync(Guid playerId, Guid ticketId, string reason, CancellationToken ct);
    }

    public sealed class NullMatchmakingNotifier : IMatchmakingNotifier
    {
        public Task NotifyMatchedAsync(Guid playerId, Guid opponentId, string mode, int tier, string scope, Guid ticketId, CancellationToken ct)
            => Task.CompletedTask;

        public Task NotifyCancelledAsync(Guid playerId, Guid ticketId, string reason, CancellationToken ct)
            => Task.CompletedTask;
    }
}
