using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Questions
{
    public sealed record AdminListQuestions(
        string? Search,
        IReadOnlyList<string>? Tags,
        TagFilterMode TagMode,
        string? Category,
        QuestionDifficulty? Difficulty,
        string Sort = "updated_desc",
        int Page = 1,
        int PageSize = 30
    ) : IRequest<QuestionListResponseDto>;

    public sealed class AdminListQuestionsHandler(IAppDb db)
        : IRequestHandler<AdminListQuestions, QuestionListResponseDto>
    {
        public async Task<QuestionListResponseDto> Handle(AdminListQuestions r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            var tags = (r.Tags ?? Array.Empty<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Base query
            var q = db.Questions.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(r.Search))
            {
                var s = r.Search.Trim();
                // Simple contains; later you can swap to PostgreSQL full-text.
                q = q.Where(x => x.Text.Contains(s) || x.Category.Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(r.Category))
            {
                var c = r.Category.Trim();
                q = q.Where(x => x.Category == c);
            }

            if (r.Difficulty.HasValue)
            {
                q = q.Where(x => x.Difficulty == r.Difficulty.Value);
            }

            if (tags.Length > 0)
            {
                // Tag filter using join table: ANY = exists one of tags, ALL = must have all tags
                if (r.TagMode == TagFilterMode.Any)
                {
                    q = q.Where(x => db.QuestionTags.Any(t => t.QuestionId == x.Id && tags.Contains(t.Tag)));
                }
                else
                {
                    // ALL tags must exist
                    foreach (var t in tags)
                        q = q.Where(x => db.QuestionTags.Any(qt => qt.QuestionId == x.Id && qt.Tag == t));
                }
            }

            // Sorting
            q = r.Sort switch
            {
                "updated_asc" => q.OrderBy(x => x.UpdatedAtUtc),
                "created_desc" => q.OrderByDescending(x => x.CreatedAtUtc),
                "created_asc" => q.OrderBy(x => x.CreatedAtUtc),
                _ => q.OrderByDescending(x => x.UpdatedAtUtc)
            };

            var total = await q.CountAsync(ct);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new QuestionListItemDto(
                    x.Id,
                    x.Text.Length <= 90 ? x.Text : x.Text.Substring(0, 90) + "…",
                    x.Category,
                    x.Difficulty,
                    db.QuestionTags.Where(t => t.QuestionId == x.Id).Select(t => t.Tag).Take(8).ToList(),
                    x.MediaKey != null,
                    x.UpdatedAtUtc
                ))
                .ToListAsync(ct);

            // Facets (based on current filtered set except the facet dimension itself could be expanded later)
            // For now: compute facets from the filtered result set.
            var filteredIds = q.Select(x => x.Id);

            var tagFacets = await db.QuestionTags.AsNoTracking()
                .Where(t => filteredIds.Contains(t.QuestionId))
                .GroupBy(t => t.Tag)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Key)
                .Take(50)
                .ToListAsync(ct);

            var categoryFacets = await db.Questions.AsNoTracking()
                .Where(x => filteredIds.Contains(x.Id))
                .GroupBy(x => x.Category)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Key)
                .Take(50)
                .ToListAsync(ct);

            var difficultyFacets = await db.Questions.AsNoTracking()
                .Where(x => filteredIds.Contains(x.Id))
                .GroupBy(x => x.Difficulty)
                .Select(g => new FacetCountDto(g.Key.ToString(), g.Count()))
                .OrderByDescending(x => x.Count)
                .ToListAsync(ct);

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
