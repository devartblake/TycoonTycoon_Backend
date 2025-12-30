using MediatR;
using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Domain.Notifications;

namespace Tycoon.Backend.Application.Missions.Handlers
{
    public sealed class RoundCompletedMissionHandler : INotificationHandler<DomainEventNotification>
    {
        private readonly MissionProgressService _missions;

        public RoundCompletedMissionHandler(MissionProgressService missions)
        {
            _missions = missions;
        }

        public async Task Handle(DomainEventNotification notification, CancellationToken ct)
        {
            if (notification.DomainEvent is not RoundCompletedEvent e) return;

            await _missions.ApplyRoundCompletedAsync(
                playerId: e.PlayerId,
                perfectRound: e.PerfectRound,
                avgAnswerTimeMs: e.AvgAnswerTimeMs,
                ct: ct
            );
        }
    }
}
