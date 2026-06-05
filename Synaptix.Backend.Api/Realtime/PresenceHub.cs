using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Shared.Contracts.Realtime.Presence;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Presence hub — tracks player online/offline status and pushes changes to
    /// subscribed friends.
    ///
    /// Route: /ws/presence
    /// Auth:  JWT via ?access_token=&lt;jwt&gt; or ?playerId=&lt;guid&gt; (same as NotificationHub)
    ///
    /// Client flow:
    ///   1. Connect with ?playerId={id}&amp;access_token={jwt}
    ///      → auto-joins player:{id} group
    ///   2. Call SubscribeFriends(friendIds[])
    ///      → joins player:{friendId}:watchers group for each friend
    ///      → receives presence.snapshot of which friends are currently online
    ///   3. Server pushes presence.changed whenever a watched friend connects/disconnects
    /// </summary>
    public sealed class PresenceHub(
        IConnectionRegistry registry,
        IPresenceReader presenceReader) : Hub<IPresenceClient>
    {
        public override async Task OnConnectedAsync()
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (Guid.TryParse(playerIdStr, out var playerId))
            {
                registry.Add(playerId, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"player:{playerId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var playerIdStr = Context.GetHttpContext()?.Request.Query["playerId"].ToString();
            if (Guid.TryParse(playerIdStr, out var playerId))
            {
                registry.Remove(playerId, Context.ConnectionId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"player:{playerId}");

                // Notify all watchers that this player went offline.
                await Clients.Group($"player:{playerId}:watchers").PresenceChanged(
                    new PlayerPresenceChangedMessage(playerId, "offline", DateTimeOffset.UtcNow));
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Subscribe to presence events for a set of friends.
        /// Joins each friend's watcher group and immediately returns a snapshot
        /// of which friends are currently online.
        /// </summary>
        public async Task SubscribeFriends(Guid[] friendIds)
        {
            foreach (var friendId in friendIds)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"player:{friendId}:watchers");

            var online = await presenceReader.GetOnlineAsync(friendIds, Context.ConnectionAborted);
            var entries = online.Select(id => new PlayerPresenceEntry(id, "online")).ToList();
            await Clients.Caller.PresenceSnapshot(
                new PlayerPresenceSnapshotMessage(entries, DateTimeOffset.UtcNow));
        }

        public Task UnsubscribeFriend(Guid friendId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"player:{friendId}:watchers");
    }
}
