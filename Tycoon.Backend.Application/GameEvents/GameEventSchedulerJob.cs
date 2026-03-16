using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Config;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.GameEvents
{
    public sealed class GameEventSchedulerJob(IAppDb db, IMediator mediator, ILogger<GameEventSchedulerJob> logger, FeatureFlagService flags)
    {
        public async Task RunAsync(CancellationToken ct)
        {
            if (!await flags.IsEnabledAsync(FeatureFlagService.GameEventsEnabled, ct))
            {
                logger.LogInformation("GameEventSchedulerJob: game_events_enabled=false, skipping.");
                return;
            }

            var now = DateTimeOffset.UtcNow;

            // Open scheduled events whose open window has arrived
            var toOpen = await db.GameEvents
                .Where(x => x.Status == GameEventStatus.Scheduled
                         && x.OpenAtUtc != null
                         && x.OpenAtUtc <= now)
                .ToListAsync(ct);

            foreach (var e in toOpen)
                e.Open(now);

            // Start open events at their scheduled time
            var toStart = await db.GameEvents
                .Where(x => x.Status == GameEventStatus.Open && x.ScheduledAtUtc <= now)
                .ToListAsync(ct);

            foreach (var e in toStart)
                e.Start(now);

            if (toOpen.Count > 0 || toStart.Count > 0)
                await db.SaveChangesAsync(ct);

            // Auto-close live events past a 2-hour duration window
            var toClose = await db.GameEvents
                .Where(x => x.Status == GameEventStatus.Live && x.ScheduledAtUtc <= now.AddHours(-2))
                .Select(x => x.Id)
                .ToListAsync(ct);

            foreach (var id in toClose)
            {
                BackgroundJob.Enqueue<CloseGameEventWorker>(w => w.RunAsync(id, CancellationToken.None));
                logger.LogInformation("Enqueued close job for game event {EventId}", id);
            }
        }
    }
}
