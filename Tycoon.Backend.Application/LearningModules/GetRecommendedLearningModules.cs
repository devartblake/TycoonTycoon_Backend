using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.LearningModules
{
    public sealed record GetRecommendedLearningModules(Guid? PlayerId, int Count)
        : IRequest<RecommendedLearningModulesResponseDto>;

    public sealed class GetRecommendedLearningModulesHandler
        : IRequestHandler<GetRecommendedLearningModules, RecommendedLearningModulesResponseDto>
    {
        private readonly IAppDb _db;

        public GetRecommendedLearningModulesHandler(IAppDb db) => _db = db;

        public async Task<RecommendedLearningModulesResponseDto> Handle(
            GetRecommendedLearningModules request,
            CancellationToken ct)
        {
            var take = request.Count <= 0 ? 5 : Math.Clamp(request.Count, 1, 20);

            var query = _db.LearningModules
                .AsNoTracking()
                .Where(m => m.IsPublished);

            HashSet<Guid>? completedIds = null;
            if (request.PlayerId.HasValue)
            {
                completedIds = (await _db.ModuleCompletions
                    .AsNoTracking()
                    .Where(c => c.PlayerId == request.PlayerId.Value)
                    .Select(c => c.ModuleId)
                    .ToListAsync(ct))
                    .ToHashSet();

                query = query.Where(m => !completedIds.Contains(m.Id));
            }

            var modules = await query
                .OrderBy(m => m.Difficulty)
                .ThenBy(m => m.Title)
                .Take(take)
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Category,
                    m.Difficulty,
                    LessonCount = m.Lessons.Count,
                    m.RewardXp,
                    m.RewardCoins
                })
                .ToListAsync(ct);

            var items = modules
                .Select(m => new LearningModuleListItemDto(
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Category,
                    m.Difficulty,
                    m.LessonCount,
                    m.RewardXp,
                    m.RewardCoins,
                    completedIds is not null && completedIds.Contains(m.Id)
                ))
                .ToList();

            return new RecommendedLearningModulesResponseDto(items);
        }
    }
}
