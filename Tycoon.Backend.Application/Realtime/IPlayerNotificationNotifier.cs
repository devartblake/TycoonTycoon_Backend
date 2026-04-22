namespace Tycoon.Backend.Application.Realtime
{
    public interface IPlayerNotificationNotifier
    {
        Task NotifyInboxUpdatedAsync(Guid playerId, int unreadCount, string reason, CancellationToken ct);
    }

    public sealed class NullPlayerNotificationNotifier : IPlayerNotificationNotifier
    {
        public Task NotifyInboxUpdatedAsync(Guid playerId, int unreadCount, string reason, CancellationToken ct)
            => Task.CompletedTask;
    }
}
