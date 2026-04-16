using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.LearningModules
{
    public sealed record ListLearningModules(
        Guid? PlayerId,
        string? Category,
        QuestionDifficulty? Difficulty
    ) : IRequest<IReadOnlyList<LearningModuleListItemDto>>;

    public sealed class ListLearningModulesHandler
        : IRequestHandler<ListLearningModules, IReadOnlyList<LearningModuleListItemDto>>
    {
        private readonly IAppDb _db;

        public ListLearningModulesHandler(IAppDb db) => _db = db;

        public async Task<IReadOnlyList<LearningModuleListItemDto>> Handle(
            ListLearningModules request,
            CancellationToken ct)
        {
            var query = _db.LearningModules
                .AsNoTracking()
                .Where(m => m.IsPublished);

            if (!string.IsNullOrWhiteSpace(request.Category))
                query = query.Where(m => m.Category == request.Category.Trim());

            if (request.Difficulty.HasValue)
                query = query.Where(m => m.Difficulty == request.Difficulty.Value);

            // Materialise with lesson counts
            var modules = await query
                .OrderBy(m => m.Difficulty)
                .ThenBy(m => m.Title)
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

            // Resolve completion state when a player id is provided
            HashSet<Guid>? completedIds = null;
            if (request.PlayerId.HasValue)
            {
                var ids = modules.Select(m => m.Id).ToList();
                completedIds = (await _db.ModuleCompletions
                    .AsNoTracking()
                    .Where(c => c.PlayerId == request.PlayerId.Value && ids.Contains(c.ModuleId))
                    .Select(c => c.ModuleId)
                    .ToListAsync(ct))
                    .ToHashSet();
            }

            return modules
                .Select(m => new LearningModuleListItemDto(
                    m.Id,
                    m.Title,
                    m.Description,
                    m.Category,
                    m.Difficulty,
                    m.LessonCount,
                    m.RewardXp,
                    m.RewardCoins,
                    IsCompleted: completedIds?.Contains(m.Id) ?? false
                ))
                .ToList();
        }
    }
}
