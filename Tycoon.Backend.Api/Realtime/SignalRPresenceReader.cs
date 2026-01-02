using Tycoon.Backend.Application.Realtime;

namespace Tycoon.Backend.Api.Realtime
{
    public sealed class SignalRPresenceReader(IConnectionRegistry registry) : IPresenceReader
    {
        public Task<IReadOnlyList<Guid>> GetOnlineAsync(IReadOnlyList<Guid> playerIds, CancellationToken ct)
        {
            // ct unused (in-memory lookup), but kept for signature stability
            var online = registry.GetOnlinePlayers(playerIds);
            return Task.FromResult<IReadOnlyList<Guid>>(online.ToList());
        }
    }
}
