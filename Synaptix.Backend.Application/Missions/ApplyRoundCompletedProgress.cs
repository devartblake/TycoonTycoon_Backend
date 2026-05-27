using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Missions
{
    public sealed record ApplyRoundCompletedProgress(RoundCompletedProgressDto Dto) : IRequest<ProgressAppliedDto>;

    public sealed class ApplyRoundCompletedProgressHandler(
        IAppDb db,
        MissionProgressService progress)
        : IRequestHandler<ApplyRoundCompletedProgress, ProgressAppliedDto>
    {
        public async ValueTask<ProgressAppliedDto> Handle(ApplyRoundCompletedProgress r, CancellationToken ct)
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
