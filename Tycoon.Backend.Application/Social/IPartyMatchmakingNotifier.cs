using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Social
{
    public interface IPartyMatchmakingNotifier
    {
        Task NotifyPartyMatchedAsync(
            Guid partyId,
            Guid opponentPartyId,
            Guid matchId,
            IReadOnlyList<Guid> memberPlayerIds,
            string mode,
            int tier,
            string scope,
            Guid ticketId,
            CancellationToken ct);

        Task NotifyRosterUpdatedAsync(
            PartyRosterDto roster,
            IReadOnlyList<Guid> memberPlayerIds,
            IReadOnlyList<Guid> onlinePlayerIds,
            CancellationToken ct);
    }

    public sealed class NullPartyMatchmakingNotifier : IPartyMatchmakingNotifier
    {
        public Task NotifyPartyMatchedAsync(
            Guid partyId,
            Guid opponentPartyId,
            Guid matchId,
            IReadOnlyList<Guid> memberPlayerIds,
            string mode,
            int tier,
            string scope,
            Guid ticketId,
            CancellationToken ct) => Task.CompletedTask;

        public Task NotifyRosterUpdatedAsync(
            PartyRosterDto roster,
            IReadOnlyList<Guid> memberPlayerIds,
            IReadOnlyList<Guid> onlinePlayerIds,
            CancellationToken ct) => Task.CompletedTask;
    }
}
