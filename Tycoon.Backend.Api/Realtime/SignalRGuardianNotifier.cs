using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Application.Guardians;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Shared.Contracts.Realtime.Guardians;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRGuardianNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IGuardianNotifier
    {
        public Task NotifyGuardianChangedAsync(GuardianChangedMessage message, CancellationToken ct)
            => hub.Clients.Group($"guardian:{message.SeasonId}:{message.TierNumber}").GuardianChanged(message);
    }
}
