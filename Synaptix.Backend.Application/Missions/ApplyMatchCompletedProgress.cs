using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Missions
{
    public sealed record ApplyMatchCompletedProgress(MatchCompletedProgressDto Dto) : IRequest<ProgressAppliedDto>;

    public sealed class ApplyMatchCompletedProgressHandler(
        IAppDb db,
        MissionProgressService progress)
        : IRequestHandler<ApplyMatchCompletedProgress, ProgressAppliedDto>
    {
        public async ValueTask<ProgressAppliedDto> Handle(ApplyMatchCompletedProgress r, CancellationToken ct)
        {
            var dto = r.Dto;
            var now = DateTimeOffset.UtcNow;

            // Idempotency gate
            if (await db.ProcessedGameplayEvents.AsNoTracking().AnyAsync(x => x.EventId == dto.EventId, ct))
            {
                return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Duplicate", now);
            }

            db.ProcessedGameplayEvents.Add(new ProcessedGameplayEvent(dto.EventId, dto.PlayerId, "match-completed"));

            try
            {
                // Reserve the EventId first (unique constraint prevents races)
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Duplicate", now);
            }

            await progress.ApplyMatchCompletedAsync(
                playerId: dto.PlayerId,
                isWin: dto.IsWin,
                correctAnswers: dto.CorrectAnswers,
                totalQuestions: dto.TotalQuestions,
                durationSeconds: dto.DurationSeconds,
                ct: ct);

            return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Applied", now);
        }
    }
}
