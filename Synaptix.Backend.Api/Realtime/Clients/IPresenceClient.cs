using Synaptix.Shared.Contracts.Realtime.Presence;

namespace Synaptix.Backend.Api.Realtime.Clients
{
    public interface IPresenceClient
    {
        Task PresenceChanged(PlayerPresenceChangedMessage message);
        Task PresenceSnapshot(PlayerPresenceSnapshotMessage message);
    }
}
