using System.Collections.Concurrent;

namespace Tycoon.Backend.Api.Realtime
{
    /// <summary>
    /// Tracks active SignalR connectionIds by PlayerId.
    /// Used for server-side group management (e.g., match:{matchId} auto-join).
    /// </summary>
    public interface IConnectionRegistry
    {
        void Add(Guid playerId, string connectionId);
        void Remove(Guid playerId, string connectionId);
        IReadOnlyCollection<string> GetConnections(Guid playerId);
        IReadOnlyCollection<Guid> GetOnlinePlayers(IEnumerable<Guid> playerIds);
    }

    public sealed class ConnectionRegistry : IConnectionRegistry
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, byte>> _map = new();

        public void Add(Guid playerId, string connectionId)
        {
            var set = _map.GetOrAdd(playerId, _ => new ConcurrentDictionary<string, byte>());
            set.TryAdd(connectionId, 0);
        }

        public void Remove(Guid playerId, string connectionId)
        {
            if (_map.TryGetValue(playerId, out var set))
            {
                set.TryRemove(connectionId, out _);
                if (set.IsEmpty)
                    _map.TryRemove(playerId, out _);
            }
        }

        public IReadOnlyCollection<string> GetConnections(Guid playerId)
        {
            if (_map.TryGetValue(playerId, out var set))
                return set.Keys.ToArray();

            return Array.Empty<string>();
        }

        public IReadOnlyCollection<Guid> GetOnlinePlayers(IEnumerable<Guid> playerIds)
        {
            var list = new List<Guid>();
            foreach (var pid in playerIds)
            {
                if (_map.ContainsKey(pid))
                    list.Add(pid);
            }
            return list;
        }
    }
}
