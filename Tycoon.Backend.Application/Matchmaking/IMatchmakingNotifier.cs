namespace Tycoon.Backend.Application.Matchmaking
{
    public interface IMatchmakingNotifier
    {
        Task NotifyMatchedAsync(Guid playerId, Guid opponentId, string mode, int tier, string scope, Guid ticketId, CancellationToken ct);
    }

    public sealed class NullMatchmakingNotifier : IMatchmakingNotifier
    {
        public Task NotifyMatchedAsync(Guid playerId, Guid opponentId, string mode, int tier, string scope, Guid ticketId, CancellationToken ct)
            => Task.CompletedTask;
    }
}
