using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Api.Realtime;
using Tycoon.Backend.Api.Realtime.Clients;
using Tycoon.Backend.Domain.Events;
using Tycoon.Shared.Abstractions.Core.Domain.Events;
using Tycoon.Shared.Contracts.Realtime.Votes;

namespace Tycoon.Backend.Api.Features.Votes
{
    public sealed class VoteCastEventHandler(IHubContext<NotificationHub, INotificationClient> hub)
        : IDomainEventHandler<VoteCastEvent>
    {
        public Task Handle(VoteCastEvent evt, CancellationToken ct)
        {
            var message = new VoteCastMessage(
                evt.VoteId,
                evt.PlayerId,
                evt.Option,
                evt.Topic,
                evt.CastAtUtc
            );

            return hub.Clients.Group($"topic:{evt.Topic}").VoteCast(message);
        }
    }
}
