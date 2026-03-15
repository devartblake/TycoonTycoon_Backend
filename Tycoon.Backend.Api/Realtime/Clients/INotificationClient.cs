using System.Threading.Tasks;
using Tycoon.Shared.Contracts.Realtime.Missions;
using Tycoon.Shared.Contracts.Realtime.Votes;

namespace Tycoon.Backend.Api.Realtime.Clients
{
    public interface INotificationClient
    {
        // existing methods...

        Task MissionClaimed(MissionClaimedMessage message);

        Task VoteCast(VoteCastMessage message);
    }
}
