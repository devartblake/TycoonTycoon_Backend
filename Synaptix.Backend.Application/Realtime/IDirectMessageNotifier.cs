namespace Synaptix.Backend.Application.Realtime
{
    public interface IDirectMessageNotifier
    {
        Task NotifyDirectMessagesUpdatedAsync(Guid playerId, Guid? conversationId, int unreadCount, string reason, CancellationToken ct);
    }

    public sealed class NullDirectMessageNotifier : IDirectMessageNotifier
    {
        public Task NotifyDirectMessagesUpdatedAsync(Guid playerId, Guid? conversationId, int unreadCount, string reason, CancellationToken ct)
            => Task.CompletedTask;
    }
}
