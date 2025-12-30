using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Missions
{
    public sealed record ApplyMatchCompletedProgress(MatchCompletedProgressDto Dto) : IRequest<ProgressAppliedDto>;

    public sealed class ApplyMatchCompletedProgressHandler(
        IAppDb db,
        MissionProgressService progress)
        : IRequestHandler<ApplyMatchCompletedProgress, ProgressAppliedDto>
    {
        public async Task<ProgressAppliedDto> Handle(ApplyMatchCompletedProgress r, CancellationToken ct)
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
