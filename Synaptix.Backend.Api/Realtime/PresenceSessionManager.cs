using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed record PresenceActivity(
        string Status,
        string? Activity,
        JsonElement? GameActivity
    );

    public interface IPresenceSessionManager
    {
        void Register(Guid playerId, WebSocket socket);
        void Unregister(Guid playerId);
        void SetActivity(Guid playerId, PresenceActivity? activity);
        PresenceActivity? GetActivity(Guid playerId);
        Task SendToPlayerAsync(Guid playerId, string json, CancellationToken ct);
        IReadOnlyCollection<Guid> GetConnectedPlayerIds();
    }

    public sealed class PresenceSessionManager : IPresenceSessionManager
    {
        private readonly ConcurrentDictionary<Guid, (WebSocket Socket, PresenceActivity? Activity)> _sessions = new();

        public void Register(Guid playerId, WebSocket socket)
            => _sessions[playerId] = (socket, null);

        public void Unregister(Guid playerId)
            => _sessions.TryRemove(playerId, out _);

        public void SetActivity(Guid playerId, PresenceActivity? activity)
        {
            if (_sessions.TryGetValue(playerId, out var entry))
                _sessions[playerId] = (entry.Socket, activity);
        }

        public PresenceActivity? GetActivity(Guid playerId)
            => _sessions.TryGetValue(playerId, out var entry) ? entry.Activity : null;

        public async Task SendToPlayerAsync(Guid playerId, string json, CancellationToken ct)
        {
            if (!_sessions.TryGetValue(playerId, out var entry))
                return;

            if (entry.Socket.State != WebSocketState.Open)
                return;

            var bytes = Encoding.UTF8.GetBytes(json);
            try
            {
                await entry.Socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    ct);
            }
            catch (WebSocketException)
            {
                // Connection dropped; will be cleaned up on disconnect
            }
        }

        public IReadOnlyCollection<Guid> GetConnectedPlayerIds()
            => _sessions.Keys.ToArray();
    }
}
