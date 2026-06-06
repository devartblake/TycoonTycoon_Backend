using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Questions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record GetRecommendedLearningModules(Guid? PlayerId, int Count)
        : IRequest<RecommendedLearningModulesResponseDto>;

    public sealed class GetRecommendedLearningModulesHandler
        : IRequestHandler<GetRecommendedLearningModules, RecommendedLearningModulesResponseDto>
    {
        // Private projection type to avoid repeated anonymous-type definitions.
        private sealed record ModuleRow(
            Guid Id, string Title, string Description, string Category,
            QuestionDifficulty Difficulty, int LessonCount, int RewardXp, int RewardCoins,
            string CanonicalCategory, string? Subject, string? Topic, string? GradeBand, string? AgeGroup, string? Audience);

        private readonly IAppDb _db;
        private readonly IPlayerMindProfileService? _mindProfiles;

        public GetRecommendedLearningModulesHandler(IAppDb db, IPlayerMindProfileService? mindProfiles = null)
        {
            _db = db;
            _mindProfiles = mindProfiles;
        }

        public async ValueTask<RecommendedLearningModulesResponseDto> Handle(
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

            // Resolve weak categories from the personalization profile when available.
            HashSet<string>? weakCategories = null;
            if (request.PlayerId.HasValue && _mindProfiles is not null)
            {
                try
                {
                    var profile = await _mindProfiles.GetOrCreateAsync(request.PlayerId.Value, ct);
                    if (profile.CategoryWeaknesses.Count > 0)
                    {
                        weakCategories = profile.CategoryWeaknesses
                            .Where(kv => kv.Value > 0)
                            .Select(kv => QuestionTaxonomy.ResolveDefinition(kv.Key).Key)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }
                }
                catch
                {
                    // Personalization must never break module recommendations.
                }
            }

            List<ModuleRow> orderedModules;

            if (weakCategories is { Count: > 0 })
            {
                // Fetch weak-category modules first (always surfaced, up to `take`).
                var weakModules = await ProjectQuery(query
                    .Where(m => weakCategories.Contains(m.CanonicalCategory))
                    .OrderBy(m => m.Difficulty)
                    .ThenBy(m => m.Title)
                    .Take(take), ct);

                // Fill remaining slots with modules from other categories.
                var remaining = take - weakModules.Count;
                if (remaining > 0)
                {
                    var weakIds = weakModules.Select(m => m.Id).ToHashSet();
                    var otherModules = await ProjectQuery(query
                        .Where(m => !weakCategories.Contains(m.CanonicalCategory) && !weakIds.Contains(m.Id))
                        .OrderBy(m => m.Difficulty)
                        .ThenBy(m => m.Title)
                        .Take(remaining), ct);

                    orderedModules = [.. weakModules, .. otherModules];
                }
                else
                {
                    orderedModules = weakModules;
                }
            }
            else
            {
                orderedModules = await ProjectQuery(query
                    .OrderBy(m => m.Difficulty)
                    .ThenBy(m => m.Title)
                    .Take(take), ct);
            }

            var items = orderedModules
                .Select(m => new LearningModuleListItemDto(
                    m.Id, m.Title, m.Description, m.Category, m.Difficulty,
                    m.LessonCount, m.RewardXp, m.RewardCoins,
                    completedIds is not null && completedIds.Contains(m.Id),
                    m.CanonicalCategory,
                    m.Subject,
                    m.Topic,
                    m.GradeBand,
                    m.AgeGroup,
                    m.Audience))
                .ToList();

            return new RecommendedLearningModulesResponseDto(items);
        }

        private static Task<List<ModuleRow>> ProjectQuery(
            IQueryable<Domain.Entities.LearningModule> source, CancellationToken ct) =>
            source
                .Select(m => new ModuleRow(
                    m.Id, m.Title, m.Description, m.Category, m.Difficulty,
                    m.Lessons.Count, m.RewardXp, m.RewardCoins,
                    m.CanonicalCategory, m.Subject, m.Topic, m.GradeBand, m.AgeGroup, m.Audience))
                .ToListAsync(ct);
    }
}
