using System.Threading.Tasks;
using Synaptix.Shared.Contracts.Realtime.GameEvents;
using Synaptix.Shared.Contracts.Realtime.Guardians;
using Synaptix.Shared.Contracts.Realtime.Missions;
using Synaptix.Shared.Contracts.Realtime.Notifications;
using Synaptix.Shared.Contracts.Realtime.Territory;
using Synaptix.Shared.Contracts.Realtime.Votes;

namespace Synaptix.Backend.Api.Realtime.Clients
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

        Task NotificationInboxUpdated(NotificationInboxUpdatedMessage message);

        Task DirectMessagesUpdated(DirectMessagesUpdatedMessage message);
    }
}
