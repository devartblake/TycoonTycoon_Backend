using Mediator;
using Synaptix.Backend.Domain.Events;
using Synaptix.Backend.Domain.Notifications;

namespace Synaptix.Backend.Application.Missions.Handlers
{
    public sealed class MatchCompletedMissionHandler : INotificationHandler<DomainEventNotification>
    {
        private readonly MissionProgressService _missions;

        public MatchCompletedMissionHandler(MissionProgressService missions)
        {
            _missions = missions;
        }

        public async ValueTask Handle(DomainEventNotification notification, CancellationToken ct)
        {
            if (notification.DomainEvent is not MatchCompletedEvent e) return;

            await _missions.ApplyMatchCompletedAsync(
                playerId: e.PlayerId,
                isWin: e.IsWin,
                correctAnswers: e.CorrectAnswers,
                totalQuestions: e.TotalQuestions,
                durationSeconds: e.DurationSeconds,
                ct: ct
            );
        }
    }
}
