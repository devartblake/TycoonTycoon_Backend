using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Application.Realtime;
using Synaptix.Shared.Contracts.Realtime.Presence;

namespace Synaptix.Backend.Api.Realtime
{
    /// <summary>
    /// Pushes presence changes to all players who have subscribed to watch a given player.
    /// Called when a player's online/offline status changes (e.g. on hub connect/disconnect,
    /// or from any application service that changes presence state).
    /// </summary>
    public sealed class SignalRPresenceNotifier(IHubContext<PresenceHub, IPresenceClient> hub)
        : IPresenceNotifier
    {
        public Task NotifyPresenceChangedAsync(
            Guid playerId,
            string status,
            IReadOnlyList<Guid> watcherIds,
            CancellationToken ct)
        {
            var message = new PlayerPresenceChangedMessage(playerId, status, DateTimeOffset.UtcNow);

            // Push to the player's dedicated watchers group (subscribers who called SubscribeFriends).
            return hub.Clients.Group($"player:{playerId}:watchers").PresenceChanged(message);
        }
    }
}
