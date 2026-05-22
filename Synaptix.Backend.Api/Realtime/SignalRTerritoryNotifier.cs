using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Application.Territory;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Shared.Contracts.Realtime.Territory;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRTerritoryNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : ITerritoryNotifier
    {
        public Task NotifyTileCapturedAsync(TerritoryCaptureMesage message, CancellationToken ct)
            => hub.Clients.Group($"territory:{message.SeasonId}:{message.TierNumber}").TerritoryCapture(message);
    }
}
