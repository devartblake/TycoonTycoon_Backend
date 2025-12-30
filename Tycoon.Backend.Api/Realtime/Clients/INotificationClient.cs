using System.Threading.Tasks;
using Tycoon.Shared.Contracts.Realtime.Missions;

namespace Tycoon.Backend.Api.Realtime.Clients
{
    public interface INotificationClient
    {
        // existing methods...

        Task MissionClaimed(MissionClaimedMessage message);
    }
}
