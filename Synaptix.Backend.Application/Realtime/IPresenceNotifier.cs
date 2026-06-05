namespace Synaptix.Backend.Application.Realtime
{
    public interface IPresenceNotifier
    {
        Task NotifyPresenceChangedAsync(
            Guid playerId,
            string status,
            IReadOnlyList<Guid> watcherIds,
            CancellationToken ct);
    }

    public sealed class NullPresenceNotifier : IPresenceNotifier
    {
        public Task NotifyPresenceChangedAsync(
            Guid playerId,
            string status,
            IReadOnlyList<Guid> watcherIds,
            CancellationToken ct) => Task.CompletedTask;
    }
}
