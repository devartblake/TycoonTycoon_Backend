using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Application.Territory;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Shared.Contracts.Realtime.Territory;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRTerritoryNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : ITerritoryNotifier
    {
        public Task NotifyTileCapturedAsync(TerritoryCaptureMesage message, CancellationToken ct)
            => hub.Clients.Group($"territory:{message.SeasonId}:{message.TierNumber}").TerritoryCapture(message);
    }
}
