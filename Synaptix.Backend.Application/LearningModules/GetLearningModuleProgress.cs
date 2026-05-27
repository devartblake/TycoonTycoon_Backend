using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record GetLearningModuleProgress(Guid PlayerId)
        : IRequest<LearningModuleProgressDto>;

    public sealed class GetLearningModuleProgressHandler
        : IRequestHandler<GetLearningModuleProgress, LearningModuleProgressDto>
    {
        private readonly IAppDb _db;

        public GetLearningModuleProgressHandler(IAppDb db) => _db = db;

        public async ValueTask<LearningModuleProgressDto> Handle(
            GetLearningModuleProgress request,
            CancellationToken ct)
        {
            // COUNT avoids loading all IDs into memory; JOIN avoids large IN (...) parameter list.
            var total = await _db.LearningModules
                .AsNoTracking()
                .CountAsync(m => m.IsPublished, ct);

            var completedModuleIds = await _db.ModuleCompletions
                .AsNoTracking()
                .Where(c => c.PlayerId == request.PlayerId)
                .Join(
                    _db.LearningModules.Where(m => m.IsPublished),
                    c => c.ModuleId,
                    m => m.Id,
                    (c, m) => c.ModuleId)
                .Distinct()
                .ToListAsync(ct);
            var completed = completedModuleIds.Count;
            var remaining = Math.Max(0, total - completed);
            var completionRate = total == 0
                ? 0m
                : Math.Round((decimal)completed / total, 4, MidpointRounding.AwayFromZero);

            return new LearningModuleProgressDto(
                request.PlayerId,
                total,
                completed,
                remaining,
                completionRate,
                completedModuleIds);
        }
    }
}
