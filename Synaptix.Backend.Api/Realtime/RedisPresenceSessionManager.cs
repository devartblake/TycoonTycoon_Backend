using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class RedisPresenceSessionManager : BackgroundService, IPresenceSessionManager
    {
        private const string OnlinePlayersKey = "synaptix:presence:players";
        private const string ActivityPrefix = "synaptix:presence:activity:";
        private const string DeliveryChannel = "synaptix:presence:deliver";
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ConcurrentDictionary<Guid, (WebSocket Socket, PresenceActivity? Activity)> _localSessions = new();
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly ILogger<RedisPresenceSessionManager> _logger;

        public RedisPresenceSessionManager(
            IConnectionMultiplexer redis,
            ILogger<RedisPresenceSessionManager> logger)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public void Register(Guid playerId, WebSocket socket)
        {
            _localSessions[playerId] = (socket, null);
            _db.SetAdd(OnlinePlayersKey, playerId.ToString("D"));
        }

        public void Unregister(Guid playerId)
        {
            _localSessions.TryRemove(playerId, out _);
            _db.SetRemove(OnlinePlayersKey, playerId.ToString("D"));
            _db.KeyDelete(ActivityPrefix + playerId.ToString("D"));
        }

        public void SetActivity(Guid playerId, PresenceActivity? activity)
        {
            if (_localSessions.TryGetValue(playerId, out var entry))
                _localSessions[playerId] = (entry.Socket, activity);

            var key = ActivityPrefix + playerId.ToString("D");
            if (activity is null)
            {
                _db.KeyDelete(key);
            }
            else
            {
                _db.StringSet(key, JsonSerializer.Serialize(activity, JsonOptions), TimeSpan.FromHours(12));
            }
        }

        public PresenceActivity? GetActivity(Guid playerId)
        {
            if (_localSessions.TryGetValue(playerId, out var entry) && entry.Activity is not null)
                return entry.Activity;

            var json = _db.StringGet(ActivityPrefix + playerId.ToString("D"));
            if (!json.HasValue)
                return null;

            try
            {
                return JsonSerializer.Deserialize<PresenceActivity>(json.ToString(), JsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize Redis presence activity for player {PlayerId}", playerId);
                return null;
            }
        }

        public async Task SendToPlayerAsync(Guid playerId, string json, CancellationToken ct)
        {
            var message = JsonSerializer.Serialize(new RedisPresenceDelivery(playerId, json), JsonOptions);
            await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal(DeliveryChannel), message);
        }

        public IReadOnlyCollection<Guid> GetConnectedPlayerIds()
        {
            return _db.SetMembers(OnlinePlayersKey)
                .Select(value => Guid.TryParse(value.ToString(), out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToArray();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var queue = await _redis.GetSubscriber().SubscribeAsync(RedisChannel.Literal(DeliveryChannel));
            queue.OnMessage(message =>
            {
                _ = Task.Run(
                    async () =>
                    {
                        try
                        {
                            var delivery = JsonSerializer.Deserialize<RedisPresenceDelivery>(message.Message.ToString(), JsonOptions);
                            if (delivery is not null)
                                await SendLocalAsync(delivery.PlayerId, delivery.Json, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process Redis presence delivery.");
                        }
                    },
                    stoppingToken);
            });

            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }

        private async Task SendLocalAsync(Guid playerId, string json, CancellationToken ct)
        {
            if (!_localSessions.TryGetValue(playerId, out var entry))
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
                // Disconnect cleanup happens in the request loop.
            }
        }

        private sealed record RedisPresenceDelivery(Guid PlayerId, string Json);
    }
}
