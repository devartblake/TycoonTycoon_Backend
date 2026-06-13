using StackExchange.Redis;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class RedisConnectionRegistry : IConnectionRegistry
    {
        private const string OnlinePlayersKey = "synaptix:presence:players";
        private const string PlayerConnectionsPrefix = "synaptix:presence:connections:";
        private readonly IDatabase _db;

        public RedisConnectionRegistry(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public void Add(Guid playerId, string connectionId)
        {
            var player = playerId.ToString("D");
            _db.SetAdd(OnlinePlayersKey, player);
            _db.SetAdd(PlayerConnectionsPrefix + player, connectionId);
        }

        public void Remove(Guid playerId, string connectionId)
        {
            var player = playerId.ToString("D");
            var key = PlayerConnectionsPrefix + player;

            _db.SetRemove(key, connectionId);
            if (_db.SetLength(key) == 0)
            {
                _db.KeyDelete(key);
                _db.SetRemove(OnlinePlayersKey, player);
            }
        }

        public IReadOnlyCollection<string> GetConnections(Guid playerId)
        {
            var values = _db.SetMembers(PlayerConnectionsPrefix + playerId.ToString("D"));
            return values.Select(v => v.ToString()).Where(v => v.Length > 0).ToArray();
        }

        public IReadOnlyCollection<Guid> GetOnlinePlayers(IEnumerable<Guid> playerIds)
        {
            var requested = playerIds.Distinct().ToArray();
            if (requested.Length == 0)
                return Array.Empty<Guid>();

            var values = requested
                .Select(id => (RedisValue)id.ToString("D"))
                .ToArray();
            var online = _db.SetContains(OnlinePlayersKey, values);

            var result = new List<Guid>(requested.Length);
            for (var i = 0; i < requested.Length; i++)
            {
                if (online[i])
                    result.Add(requested[i]);
            }

            return result;
        }
    }
}
