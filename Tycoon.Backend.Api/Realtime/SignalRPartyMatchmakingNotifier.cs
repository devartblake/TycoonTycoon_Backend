using Microsoft.AspNetCore.SignalR;
using Tycoon.Backend.Application.Social;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRPartyMatchmakingNotifier(
        IHubContext<MatchHub> hub,
        IConnectionRegistry registry) : IPartyMatchmakingNotifier
    {
        public async Task NotifyPartyMatchedAsync(
            Guid partyId,
            Guid opponentPartyId,
            Guid matchId,
            IReadOnlyList<Guid> memberPlayerIds,
            string mode,
            int tier,
            string scope,
            Guid ticketId,
            CancellationToken ct)
        {
            // auto-join ALL online connections to match:{matchId}
            foreach (var pid in memberPlayerIds)
            {
                var conns = registry.GetConnections(pid);
                foreach (var cid in conns)
                {
                    await hub.Groups.AddToGroupAsync(cid, $"match:{matchId}", ct);
                }
            }

            // Notify each member via player group
            var payload = new
            {
                TicketId = ticketId,
                PartyId = partyId,
                OpponentPartyId = opponentPartyId,
                MatchId = matchId,
                Mode = mode,
                Tier = tier,
                Scope = scope
            };

            // Send to each member’s player group
            foreach (var pid in memberPlayerIds)
            {
                await hub.Clients.Group($"player:{pid}")
                    .SendAsync("party.matched", payload, ct);
            }
        }

        public async Task NotifyRosterUpdatedAsync(
            PartyRosterDto roster,
            IReadOnlyList<Guid> memberPlayerIds,
            IReadOnlyList<Guid> onlinePlayerIds,
            CancellationToken ct)
        {
            var payload = new
            {
                Roster = roster,
                OnlinePlayerIds = onlinePlayerIds
            };

            foreach (var pid in memberPlayerIds)
            {
                await hub.Clients.Group($"player:{pid}")
                    .SendAsync("party.roster.updated", payload, ct);
            }
        }

        public async Task NotifyPartyClosedAsync(
            Guid partyId,
            Guid matchId,
            IReadOnlyList<Guid> memberPlayerIds,
            string reason,
            CancellationToken ct)
        {
            var payload = new
            {
                PartyId = partyId,
                MatchId = matchId,
                Reason = reason
            };

            foreach (var pid in memberPlayerIds)
            {
                await hub.Clients.Group($"player:{pid}")
                    .SendAsync("party.closed", payload, ct);
            }
        }

    }
}
