using Hangfire;
using MediatR;
using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Application.Missions.Jobs;
using Tycoon.Backend.Domain.Notifications;

namespace Tycoon.Backend.Application.Missions.Handlers
{
    public sealed class QuestionAnsweredMissionEnqueueHandler : INotificationHandler<DomainEventNotification>
    {
        private readonly IBackgroundJobClient _jobs;

        public QuestionAnsweredMissionEnqueueHandler(IBackgroundJobClient jobs)
        {
            _jobs = jobs;
        }

        public Task Handle(DomainEventNotification notification, CancellationToken ct)
        {
            if (notification.DomainEvent is not QuestionAnsweredEvent e) return Task.CompletedTask;

            // Enqueue. If volume becomes huge, convert to batching via RoundCompleted only.
            _jobs.Enqueue<QuestionAnsweredMissionJob>(job =>
                job.RunAsync(
                    e.MatchId,
                    e.PlayerId,
                    e.Mode,
                    e.Category,
                    e.Difficulty, 
                    e.IsCorrect,
                    e.AnswerTimeMs,
                    default));

            return Task.CompletedTask;
        }
    }
}
