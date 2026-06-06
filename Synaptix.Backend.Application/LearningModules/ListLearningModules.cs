using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Questions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record ListLearningModules(
        Guid? PlayerId,
        string? Category,
        QuestionDifficulty? Difficulty,
        string? Subject = null,
        string? Topic = null,
        string? GradeBand = null,
        string? AgeGroup = null,
        string? Audience = null
    ) : IRequest<IReadOnlyList<LearningModuleListItemDto>>;

    public sealed class ListLearningModulesHandler
        : IRequestHandler<ListLearningModules, IReadOnlyList<LearningModuleListItemDto>>
    {
        private readonly IAppDb _db;

        public ListLearningModulesHandler(IAppDb db) => _db = db;

        public async ValueTask<IReadOnlyList<LearningModuleListItemDto>> Handle(
            ListLearningModules request,
            CancellationToken ct)
        {
            var query = _db.LearningModules
                .AsNoTracking()
                .Where(m => m.IsPublished);

            if (!string.IsNullOrWhiteSpace(request.Category))
            {
                var category = request.Category.Trim();
                var canonical = QuestionTaxonomy.ResolveDefinition(category).Key;
                query = query.Where(m => m.Category == category || m.CanonicalCategory == canonical);
            }

            if (request.Difficulty.HasValue)
                query = query.Where(m => m.Difficulty == request.Difficulty.Value);

            if (!string.IsNullOrWhiteSpace(request.Subject))
                query = query.Where(m => m.Subject == request.Subject.Trim());
            if (!string.IsNullOrWhiteSpace(request.Topic))
                query = query.Where(m => m.Topic == request.Topic.Trim());
            if (!string.IsNullOrWhiteSpace(request.GradeBand))
                query = query.Where(m => m.GradeBand == request.GradeBand.Trim());
            if (!string.IsNullOrWhiteSpace(request.AgeGroup))
                query = query.Where(m => m.AgeGroup == request.AgeGroup.Trim());
            if (!string.IsNullOrWhiteSpace(request.Audience))
                query = query.Where(m => m.Audience == request.Audience.Trim());

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
                    m.CanonicalCategory,
                    m.Subject,
                    m.Topic,
                    m.GradeBand,
                    m.AgeGroup,
                    m.Audience,
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
                    completedIds?.Contains(m.Id) ?? false,
                    m.CanonicalCategory,
                    m.Subject,
                    m.Topic,
                    m.GradeBand,
                    m.AgeGroup,
                    m.Audience
                ))
                .ToList();
        }
    }
}
