using System.Threading.Tasks;
using Tycoon.Shared.Contracts.Realtime.GameEvents;
using Tycoon.Shared.Contracts.Realtime.Guardians;
using Tycoon.Shared.Contracts.Realtime.Missions;
using Tycoon.Shared.Contracts.Realtime.Territory;
using Tycoon.Shared.Contracts.Realtime.Votes;

namespace Tycoon.Backend.Api.Realtime.Clients
{
    public interface INotificationClient
    {
        // existing methods...

        Task MissionClaimed(MissionClaimedMessage message);

        Task VoteCast(VoteCastMessage message);

        Task GameEventElimination(GameEventEliminationMessage message);

        Task GameEventClosed(GameEventClosedMessage message);

        Task GuardianChanged(GuardianChangedMessage message);

        Task TerritoryCapture(TerritoryCaptureMesage message);
    }
}
