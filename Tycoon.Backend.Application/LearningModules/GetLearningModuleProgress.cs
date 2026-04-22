using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.LearningModules
{
    public sealed record GetLearningModuleProgress(Guid PlayerId)
        : IRequest<LearningModuleProgressDto>;

    public sealed class GetLearningModuleProgressHandler
        : IRequestHandler<GetLearningModuleProgress, LearningModuleProgressDto>
    {
        private readonly IAppDb _db;

        public GetLearningModuleProgressHandler(IAppDb db) => _db = db;

        public async Task<LearningModuleProgressDto> Handle(
            GetLearningModuleProgress request,
            CancellationToken ct)
        {
            var publishedModuleIds = await _db.LearningModules
                .AsNoTracking()
                .Where(m => m.IsPublished)
                .Select(m => m.Id)
                .ToListAsync(ct);

            var completedModuleIds = await _db.ModuleCompletions
                .AsNoTracking()
                .Where(c => c.PlayerId == request.PlayerId && publishedModuleIds.Contains(c.ModuleId))
                .Select(c => c.ModuleId)
                .Distinct()
                .ToListAsync(ct);

            var total = publishedModuleIds.Count;
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
