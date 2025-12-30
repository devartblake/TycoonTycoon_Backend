using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Missions
{
    public sealed record ApplyRoundCompletedProgress(RoundCompletedProgressDto Dto) : IRequest<ProgressAppliedDto>;

    public sealed class ApplyRoundCompletedProgressHandler(
        IAppDb db,
        MissionProgressService progress)
        : IRequestHandler<ApplyRoundCompletedProgress, ProgressAppliedDto>
    {
        public async Task<ProgressAppliedDto> Handle(ApplyRoundCompletedProgress r, CancellationToken ct)
        {
            var dto = r.Dto;
            var now = DateTimeOffset.UtcNow;

            if (await db.ProcessedGameplayEvents.AsNoTracking().AnyAsync(x => x.EventId == dto.EventId, ct))
            {
                return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Duplicate", now);
            }

            db.ProcessedGameplayEvents.Add(new ProcessedGameplayEvent(dto.EventId, dto.PlayerId, "round-completed"));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Duplicate", now);
            }

            await progress.ApplyRoundCompletedAsync(
                playerId: dto.PlayerId,
                perfectRound: dto.PerfectRound,
                avgAnswerTimeMs: dto.AvgAnswerTimeMs,
                ct: ct);

            return new ProgressAppliedDto(dto.EventId, dto.PlayerId, "Applied", now);
        }
    }
}
