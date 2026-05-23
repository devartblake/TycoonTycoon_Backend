using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Application.Guardians;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Shared.Contracts.Realtime.Guardians;

namespace Synaptix.Backend.Api.Realtime
{
    public sealed class SignalRGuardianNotifier(IHubContext<NotificationHub, INotificationClient> hub)
        : IGuardianNotifier
    {
        public Task NotifyGuardianChangedAsync(GuardianChangedMessage message, CancellationToken ct)
            => hub.Clients.Group($"guardian:{message.SeasonId}:{message.TierNumber}").GuardianChanged(message);
    }
}
