using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions
{
    public sealed record AdminListQuestions(
        string? Search,
        IReadOnlyList<string>? Tags,
        TagFilterMode TagMode,
        string? Category,
        string? Status,
        QuestionDifficulty? Difficulty,
        string Sort = "updated_desc",
        int Page = 1,
        int PageSize = 30
    ) : IRequest<QuestionListResponseDto>;

    public sealed class AdminListQuestionsHandler(IAppDb db)
        : IRequestHandler<AdminListQuestions, QuestionListResponseDto>
    {
        public async ValueTask<QuestionListResponseDto> Handle(AdminListQuestions r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            var tags = (r.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Base filtered query (no ordering yet). Keep this query reusable for
            // count/facets to avoid provider-specific translation issues from reusing
            // an already-ordered query as a subquery.
            var filtered = db.Questions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(r.Search))
            {
                var s = r.Search.Trim();
                // Simple contains; later you can swap to PostgreSQL full-text.
                filtered = filtered.Where(x => x.Text.Contains(s) || x.Category.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(r.Category))
            {
                var c = r.Category.Trim();
                var canonical = QuestionTaxonomy.ResolveDefinition(c).Key;
                filtered = filtered.Where(x => x.Category == c || x.CanonicalCategory == canonical);
            }

            if (r.Difficulty.HasValue)
            {
                filtered = filtered.Where(x => x.Difficulty == r.Difficulty.Value);
            }

            if (tags.Length > 0)
            {
                // Tag filter using join table: ANY = exists one of tags, ALL = must have all tags
                if (r.TagMode == TagFilterMode.Any)
                {
                    filtered = filtered.Where(x => db.QuestionTags.Any(t => t.QuestionId == x.Id && tags.Contains(t.Tag)));
                }
                else
                {
                    // ALL tags must exist
                    foreach (var t in tags)
                        filtered = filtered.Where(x => db.QuestionTags.Any(qt => qt.QuestionId == x.Id && qt.Tag == t));
                }
            }

            // Sorting
            var sorted = r.Sort switch
            {
                "updated_asc" => filtered.OrderBy(x => x.UpdatedAtUtc),
                "created_desc" => filtered.OrderByDescending(x => x.CreatedAtUtc),
                "created_asc" => filtered.OrderBy(x => x.CreatedAtUtc),
                _ => filtered.OrderByDescending(x => x.UpdatedAtUtc)
            };

            var total = await filtered.CountAsync(ct);

            var pageRows = await sorted
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Text,
                    x.Category,
                    x.Difficulty,
                    x.MediaKey,
                    x.CanonicalCategory,
                    x.DisplayCategory,
                    x.Subject,
                    x.Topic,
                    x.Subtopic,
                    x.GradeBand,
                    x.AgeGroup,
                    x.Audience,
                    x.SourceDataset,
                    x.SourceQuestionId,
                    x.QuestionType,
                    x.MediaType,
                    x.TaxonomyTagsJson,
                    x.UpdatedAtUtc
                })
                .ToListAsync(ct);

            var pageIds = pageRows.Select(x => x.Id).ToArray();
            var pageTags = await db.QuestionTags.AsNoTracking()
                .Where(t => pageIds.Contains(t.QuestionId))
                .OrderBy(t => t.Tag)
                .ToListAsync(ct);

            var tagsByQuestionId = pageTags
                .GroupBy(t => t.QuestionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Tag).Take(8).ToList());

            var items = pageRows
                .Select(x => new QuestionListItemDto(
                    x.Id,
                    x.Text.Length <= 90 ? x.Text : x.Text.Substring(0, 90) + "…",
                    x.Category,
                    x.Difficulty,
                    x.MediaKey,
                    tagsByQuestionId.TryGetValue(x.Id, out var tagsForQuestion) ? tagsForQuestion : [],
                    new QuestionTaxonomyDto(
                        x.CanonicalCategory,
                        x.DisplayCategory,
                        x.Subject,
                        x.Topic,
                        x.Subtopic,
                        x.GradeBand,
                        x.AgeGroup,
                        x.Audience,
                        x.SourceDataset,
                        x.SourceQuestionId,
                        x.QuestionType,
                        x.MediaType,
                        QuestionTaxonomy.ParseTagsJson(x.TaxonomyTagsJson)),
                    x.MediaKey != null,
                    x.UpdatedAtUtc
                ))
                .ToList();

            // Facets (based on current filtered set except the facet dimension itself could be expanded later)
            // For now: compute facets from the filtered result set.
            var filteredIdList = await filtered.Select(x => x.Id).ToListAsync(ct);

            var facetTags = await db.QuestionTags.AsNoTracking()
                .Where(t => filteredIdList.Contains(t.QuestionId))
                .ToListAsync(ct);
            var tagFacets = facetTags
                .GroupBy(t => t.Tag)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Key)
                .Take(50)
                .ToList();

            var facetQuestions = await db.Questions.AsNoTracking()
                .Where(x => filteredIdList.Contains(x.Id))
                .Select(x => new { x.Category, x.Difficulty })
                .ToListAsync(ct);
            var categoryFacets = facetQuestions
                .GroupBy(x => x.Category)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Key)
                .Take(50)
                .ToList();

            var difficultyFacets = facetQuestions
                .GroupBy(x => x.Difficulty)
                .Select(g => new FacetCountDto(g.Key.ToString(), g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();

            var echo = new QuestionQueryEchoDto(
                r.Search,
                tags,
                r.TagMode,
                r.Category,
                r.Difficulty,
                r.Sort,
                page,
                pageSize
            );

            return new QuestionListResponseDto(items, total, page, pageSize, echo, tagFacets, categoryFacets, difficultyFacets);
        }
    }
}
