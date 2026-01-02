namespace Tycoon.Backend.Application.Realtime
{
    /// <summary>
    /// Reads realtime presence for a set of players.
    /// Implemented by the API layer (SignalR connection registry).
    /// </summary>
    public interface IPresenceReader
    {
        Task<IReadOnlyList<Guid>> GetOnlineAsync(IReadOnlyList<Guid> playerIds, CancellationToken ct);
    }

    public sealed class NullPresenceReader : IPresenceReader
    {
        public Task<IReadOnlyList<Guid>> GetOnlineAsync(IReadOnlyList<Guid> playerIds, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }
}
