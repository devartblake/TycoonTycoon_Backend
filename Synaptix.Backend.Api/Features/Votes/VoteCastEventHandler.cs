using Mediator;
using Microsoft.AspNetCore.SignalR;
using Synaptix.Backend.Api.Realtime;
using Synaptix.Backend.Api.Realtime.Clients;
using Synaptix.Backend.Domain.Events;
using Synaptix.Shared.Contracts.Realtime.Votes;

namespace Synaptix.Backend.Domain.Events
{
    public sealed class VoteCastEventHandler(IHubContext<NotificationHub, INotificationClient> hub)
        : INotificationHandler<VoteCastEvent>
    {
        public async ValueTask Handle(VoteCastEvent evt, CancellationToken ct)
        {
            var message = new VoteCastMessage(
                evt.VoteId,
                evt.PlayerId,
                evt.Option,
                evt.Topic,
                evt.CastAtUtc
            );

            await hub.Clients.Group($"topic:{evt.Topic}").VoteCast(message);
        }
    }
}
