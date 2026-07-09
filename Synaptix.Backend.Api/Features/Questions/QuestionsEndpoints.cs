using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Synaptix.Backend.Api.Features.Progression;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Questions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Questions
{
    public static class QuestionsEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            // Public gameplay question contract.
            // /questions is the supported backend surface for play-oriented retrieval and grading.
            // Legacy /quiz routes are intentionally not mapped here.
            var g = app.MapGroup("/questions").WithTags("Questions");

            g.MapGet("/set", GetQuestionSet);
            g.MapPost("/mixed", GetMixedQuestionSet);
            g.MapGet("/categories", GetCategories);
            g.MapGet("/metadata", GetMetadata);
            g.MapPost("/preview-set", PreviewQuestionSet);
            g.MapPost("/check", CheckAnswer);
            g.MapPost("/check-batch", CheckAnswersBatch);
        }

        /// <summary>
        /// Serves a random set of questions for gameplay.
        /// Correct answers are NOT included — use /questions/check for server-side grading.
        /// When <paramref name="mode"/> is not "ranked" and a <paramref name="playerId"/> is supplied,
        /// adaptive defaults are applied: the player's weakest category and recommended difficulty
        /// are used when those filters are not explicitly provided.
        /// Ranked play is never influenced by personalization to preserve fairness.
        /// </summary>
        private static async Task<IResult> GetQuestionSet(
            [FromQuery] string? category,
            [FromQuery] QuestionDifficulty? difficulty,
            [FromQuery] int count,
            [FromQuery] Guid? playerId,
            [FromQuery] string? mode,
            [FromQuery] string? gradeBand,
            [FromQuery] string? ageGroup,
            [FromQuery] string? audience,
            [FromQuery] string? subject,
            [FromQuery] string? topic,
            [FromQuery] string? dataset,
            [FromQuery] string[]? tags,
            IAppDb db,
            IObjectStorage storage,
            IPlayerMindProfileService mindProfiles,
            CancellationToken ct)
        {
            // Apply adaptive strategy only for non-ranked practice modes.
            if (playerId.HasValue && !string.Equals(mode, "ranked", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var profile = await mindProfiles.GetOrCreateAsync(playerId.Value, ct);

                    if (string.IsNullOrWhiteSpace(category) && profile.CategoryWeaknesses.Count > 0)
                    {
                        category = profile.CategoryWeaknesses
                            .OrderByDescending(kv => kv.Value)
                            .First().Key;
                    }

                    if (!difficulty.HasValue)
                    {
                        difficulty = profile.ConfidenceLevel switch
                        {
                            < 0.30m => QuestionDifficulty.Easy,
                            < 0.60m => QuestionDifficulty.Medium,
                            < 0.85m => QuestionDifficulty.Hard,
                            _ => QuestionDifficulty.Expert
                        };
                    }
                }
                catch
                {
                    // Personalization must never break question serving.
                }
            }

            var dtos = await QueryGameplayQuestionsAsync(
                db,
                storage,
                count,
                new QuestionTaxonomyFilters(
                    Categories: string.IsNullOrWhiteSpace(category) ? null : new[] { category.Trim() },
                    Difficulties: difficulty.HasValue ? new[] { difficulty.Value } : null,
                    Subjects: string.IsNullOrWhiteSpace(subject) ? null : new[] { subject.Trim() },
                    Topics: string.IsNullOrWhiteSpace(topic) ? null : new[] { topic.Trim() },
                    GradeBands: string.IsNullOrWhiteSpace(gradeBand) ? null : new[] { gradeBand.Trim() },
                    AgeGroups: string.IsNullOrWhiteSpace(ageGroup) ? null : new[] { ageGroup.Trim() },
                    Audiences: string.IsNullOrWhiteSpace(audience) ? null : new[] { audience.Trim() },
                    Datasets: string.IsNullOrWhiteSpace(dataset) ? null : new[] { dataset.Trim() },
                    Tags: tags),
                ct);

            return Results.Ok(new QuestionSetDto(dtos, dtos.Count));
        }

        private static async Task<IResult> GetMixedQuestionSet(
            [FromBody] MixedQuestionSetRequest? req,
            IAppDb db,
            IObjectStorage storage,
            CancellationToken ct)
        {
            req ??= new MixedQuestionSetRequest();
            var clampedCount = req.Count <= 0 ? 10 : Math.Clamp(req.Count, 1, 50);
            var filters = new QuestionTaxonomyFilters(
                req.Categories,
                req.Difficulties,
                req.Subjects,
                req.Topics,
                req.GradeBands,
                req.AgeGroups,
                req.Audiences,
                req.Datasets,
                req.Tags);

            var baseQuery = BuildApprovedQuestionsQuery(db, filters);
            var rows = await baseQuery
                .OrderBy(q => q.CanonicalCategory)
                .ThenBy(q => q.Difficulty)
                .ThenBy(q => q.Id)
                .Take(Math.Max(clampedCount * 5, clampedCount))
                .ToListAsync(ct);

            var selected = SelectBalanced(rows, clampedCount, req.BalanceCategories, req.BalanceDifficulties);
            var dtos = selected.Select(q => ToGameplayDto(q, storage)).ToList();
            return Results.Ok(new QuestionSetDto(dtos, dtos.Count));
        }

        /// <summary>
        /// Returns the approved gameplay category catalog with counts.
        /// This is a discovery surface for filters, not a grading or answer-reveal endpoint.
        /// </summary>
        private static async Task<IResult> GetCategories(
            IAppDb db,
            CancellationToken ct)
        {
            var rows = await BuildApprovedQuestionsQuery(db, categories: null, difficulties: null, includeOptions: false)
                .GroupBy(q => q.Category)
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .OrderBy(x => x.Key)
                .ToListAsync(ct);

            var categories = rows
                .Select(x => new FacetCountDto(x.Key, x.Count))
                .ToList();

            return Results.Ok(new QuestionCategoriesResponseDto(categories));
        }

        /// <summary>
        /// Returns supported discovery metadata for the gameplay question surface.
        /// Gameplay retrieval remains answer-safe; correct answers are not exposed here.
        /// </summary>
        private static async Task<IResult> GetMetadata(
            IAppDb db,
            CancellationToken ct)
        {
            // Single query — fetch only Category+Difficulty columns, compute both facets in memory.
            var rawData = await BuildApprovedQuestionsQuery(db, categories: null, difficulties: null, includeOptions: false)
                .Select(q => new { q.Category, q.Difficulty })
                .ToListAsync(ct);

            var categories = rawData
                .GroupBy(x => x.Category)
                .OrderBy(g => g.Key)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .ToList();

            var difficulties = rawData
                .Select(x => x.Difficulty)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var taxonomyCategories = QuestionTaxonomy.Definitions
                .Select(def =>
                {
                    var count = rawData.Count(x =>
                        string.Equals(QuestionTaxonomy.ResolveDefinition(x.Category).Key, def.Key, StringComparison.OrdinalIgnoreCase));
                    return new QuestionTaxonomyFacetDto(def.Key, def.DisplayName, def.Description, count, def.Aliases);
                })
                .OrderBy(x => x.DisplayName)
                .ToList();

            var taxonomyRows = await BuildApprovedQuestionsQuery(db, new QuestionTaxonomyFilters())
                .Select(q => new
                {
                    q.Subject,
                    q.Topic,
                    q.GradeBand,
                    q.AgeGroup,
                    q.Audience,
                    q.SourceDataset
                })
                .ToListAsync(ct);

            return Results.Ok(new QuestionMetadataResponseDto(
                categories,
                difficulties,
                DefaultCount: 10,
                MaxCount: 50,
                TaxonomyCategories: taxonomyCategories,
                Subjects: BuildFacet(taxonomyRows.Select(x => x.Subject)),
                Topics: BuildFacet(taxonomyRows.Select(x => x.Topic)),
                GradeBands: BuildFacet(taxonomyRows.Select(x => x.GradeBand)),
                AgeGroups: BuildFacet(taxonomyRows.Select(x => x.AgeGroup)),
                Audiences: BuildFacet(taxonomyRows.Select(x => x.Audience)),
                Datasets: BuildFacet(taxonomyRows.Select(x => x.SourceDataset)),
                Aliases: QuestionTaxonomy.Aliases));
        }

        /// <summary>
        /// Returns an answer-safe preview of a question set using explicit category/difficulty filters.
        /// This is intended for discovery workflows and future study-set builders, not grading.
        /// </summary>
        private static async Task<IResult> PreviewQuestionSet(
            [FromBody] PreviewQuestionSetRequest req,
            IAppDb db,
            IObjectStorage storage,
            CancellationToken ct)
        {
            var dtos = await QueryGameplayQuestionsAsync(
                db,
                storage,
                req.Count,
                new QuestionTaxonomyFilters(
                    req.Categories,
                    req.Difficulties,
                    req.Subjects,
                    req.Topics,
                    req.GradeBands,
                    req.AgeGroups,
                    req.Audiences,
                    req.Datasets,
                    req.Tags),
                ct);

            return Results.Ok(new QuestionSetDto(dtos, dtos.Count));
        }

        /// <summary>
        /// Check a single answer server-side. Returns whether the selected option is correct.
        /// </summary>
        private static async Task<IResult> CheckAnswer(
            [FromBody] CheckAnswerRequest req,
            IAppDb db,
            CancellationToken ct)
        {
            var question = await db.Questions
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == req.QuestionId, ct);

            if (question is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Question not found.");

            var isCorrect = string.Equals(question.CorrectOptionId, req.SelectedOptionId, StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new CheckAnswerResponse(
                req.QuestionId,
                req.SelectedOptionId,
                question.CorrectOptionId,
                isCorrect));
        }

        /// <summary>
        /// Batch check answers for a full round/match. Returns per-question results and totals.
        /// </summary>
        private static async Task<IResult> CheckAnswersBatch(
            [FromBody] CheckAnswersBatchRequest req,
            HttpContext httpContext,
            IAppDb db,
            IMemoryCache cache,
            CancellationToken ct)
        {
            if (req.Answers.Count == 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "At least one answer is required.");

            if (req.Answers.Count > 50)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Maximum 50 answers per batch.");

            var questionIds = req.Answers.Select(a => a.QuestionId).Distinct().ToList();

            var questions = await db.Questions
                .AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, ct);

            var results = new List<CheckAnswerResponse>(req.Answers.Count);
            var correct = 0;
            var earnedXp = 0.0;

            foreach (var answer in req.Answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                {
                    results.Add(new CheckAnswerResponse(answer.QuestionId, answer.SelectedOptionId, "", false));
                    continue;
                }

                var isCorrect = string.Equals(question.CorrectOptionId, answer.SelectedOptionId, StringComparison.OrdinalIgnoreCase);
                if (isCorrect)
                {
                    correct++;
                    earnedXp += TierProgression.XpForCorrectAnswer(question.Difficulty);
                }

                results.Add(new CheckAnswerResponse(
                    answer.QuestionId,
                    answer.SelectedOptionId,
                    question.CorrectOptionId,
                    isCorrect));
            }

            var xpAward = await TryAwardQuizXpAsync(req, httpContext, db, cache, earnedXp, ct);

            return Results.Ok(new CheckAnswersBatchResponse(results, results.Count, correct, xpAward));
        }

        /// <summary>
        /// Server-authoritative XP awarding for a graded quiz session.
        /// Awards only when the caller is authenticated and supplied a quiz
        /// session id, which doubles as the idempotency key: retries of the
        /// same session return the original award instead of double-crediting.
        /// The client never chooses the amount — it is derived from the graded
        /// answers above (difficulty × 10 per correct answer).
        /// </summary>
        private static async Task<QuizXpAwardDto?> TryAwardQuizXpAsync(
            CheckAnswersBatchRequest req,
            HttpContext httpContext,
            IAppDb db,
            IMemoryCache cache,
            double earnedXp,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.QuizSessionId))
                return null;

            if (string.Equals(req.Mode, "preview", StringComparison.OrdinalIgnoreCase))
                return null;

            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            if (claim is null || !Guid.TryParse(claim.Value, out var playerId) || playerId == Guid.Empty)
                return null;

            // Idempotency: one award per (player, quiz session). In-memory is
            // sufficient for the accidental-retry window; a multi-instance
            // deployment should replace this with a distributed cache or a
            // persisted award ledger.
            var dedupeKey = $"quiz-xp:{playerId:N}:{req.QuizSessionId}";
            if (cache.TryGetValue<QuizXpAwardDto>(dedupeKey, out var previousAward))
                return previousAward;

            if (earnedXp <= 0)
            {
                var zeroAward = new QuizXpAwardDto(0, 0, false, null);
                cache.Set(dedupeKey, zeroAward, TimeSpan.FromHours(24));
                return zeroAward;
            }

            var player = await db.Players.FirstOrDefaultAsync(p => p.Id == playerId, ct);
            if (player is null)
                return null;

            var previousTier = TierProgression.GetTierForXp(player.Xp);

            player.AddXp(earnedXp);

            var newTier = TierProgression.GetTierForXp(player.Xp);
            var tierUpgraded = previousTier.Id != newTier.Id;

            if (tierUpgraded)
            {
                var tierEntity = await db.Tiers
                    .FirstOrDefaultAsync(t => t.Name == newTier.Name, ct);
                if (tierEntity is not null)
                {
                    player.SetTier(tierEntity.Id);
                }
            }

            await db.SaveChangesAsync(ct);

            var award = new QuizXpAwardDto(
                XpAwarded: earnedXp,
                TotalXp: player.Xp,
                TierUpgraded: tierUpgraded,
                NewTierId: tierUpgraded ? newTier.Id : null);

            cache.Set(dedupeKey, award, TimeSpan.FromHours(24));
            return award;
        }

        private static async Task<List<GameplayQuestionDto>> QueryGameplayQuestionsAsync(
            IAppDb db,
            IObjectStorage storage,
            int count,
            QuestionTaxonomyFilters filters,
            CancellationToken ct)
        {
            var clampedCount = count <= 0 ? 10 : Math.Clamp(count, 1, 50);

            var baseQuery = BuildApprovedQuestionsQuery(db, filters);
            var totalCount = await baseQuery.CountAsync(ct);
            if (totalCount == 0)
                return new List<GameplayQuestionDto>();

            // Count+skip avoids ORDER BY RANDOM() full-table sort on PostgreSQL.
            var skip = totalCount <= clampedCount ? 0 : Random.Shared.Next(0, totalCount - clampedCount);

            var questions = await baseQuery
                .OrderBy(q => q.Id)
                .Skip(skip)
                .Take(clampedCount)
                .ToListAsync(ct);

            return questions.Select(q => ToGameplayDto(q, storage)).ToList();
        }

        private static IQueryable<Domain.Entities.Question> BuildApprovedQuestionsQuery(
            IAppDb db,
            QuestionTaxonomyFilters filters,
            bool includeOptions = true)
        {
            var query = db.Questions
                .AsNoTracking()
                .Where(q => q.Status == "Approved")
                .AsQueryable();

            if (includeOptions)
                query = query.Include(q => q.Options);

            var categoryFilters = filters.Categories?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (categoryFilters is { Length: > 0 })
            {
                var canonicalCategoryFilters = categoryFilters
                    .Select(c => QuestionTaxonomy.ResolveDefinition(c).Key)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                query = query.Where(q => categoryFilters.Contains(q.Category) || canonicalCategoryFilters.Contains(q.CanonicalCategory));
            }

            var difficultyFilters = filters.Difficulties?.Distinct().ToArray();
            if (difficultyFilters is { Length: > 0 })
                query = query.Where(q => difficultyFilters.Contains(q.Difficulty));

            query = ApplyStringFilter(query, filters.Subjects, nameof(Domain.Entities.Question.Subject));
            query = ApplyStringFilter(query, filters.Topics, nameof(Domain.Entities.Question.Topic));
            query = ApplyStringFilter(query, filters.GradeBands, nameof(Domain.Entities.Question.GradeBand));
            query = ApplyStringFilter(query, filters.AgeGroups, nameof(Domain.Entities.Question.AgeGroup));
            query = ApplyStringFilter(query, filters.Audiences, nameof(Domain.Entities.Question.Audience));
            query = ApplyStringFilter(query, filters.Datasets, nameof(Domain.Entities.Question.SourceDataset));

            var tagFilters = filters.Tags?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (tagFilters is { Length: > 0 })
                query = query.Where(q => db.QuestionTags.Any(t => t.QuestionId == q.Id && tagFilters.Contains(t.Tag)));

            return query;
        }

        private static IQueryable<Domain.Entities.Question> BuildApprovedQuestionsQuery(
            IAppDb db,
            IEnumerable<string>? categories,
            IEnumerable<QuestionDifficulty>? difficulties,
            bool includeOptions = true) =>
            BuildApprovedQuestionsQuery(db, new QuestionTaxonomyFilters(categories, difficulties), includeOptions);

        private static IQueryable<Domain.Entities.Question> ApplyStringFilter(
            IQueryable<Domain.Entities.Question> query,
            IEnumerable<string>? values,
            string propertyName)
        {
            var filters = values?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (filters is not { Length: > 0 }) return query;
            return query.Where(q => filters.Contains(EF.Property<string>(q, propertyName)));
        }

        private static GameplayQuestionDto ToGameplayDto(Domain.Entities.Question q, IObjectStorage storage) => new(
            q.Id,
            q.Text,
            q.Category,
            q.Difficulty,
            q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
            MediaKey: q.MediaKey,
            MediaUrl: q.MediaKey is not null ? storage.GetPublicUrl(q.MediaKey) : null,
            Taxonomy: QuestionTaxonomy.ToDto(q));

        private static List<Domain.Entities.Question> SelectBalanced(
            IReadOnlyList<Domain.Entities.Question> rows,
            int count,
            bool balanceCategories,
            bool balanceDifficulties)
        {
            if (rows.Count <= count) return rows.ToList();
            var groups = rows
                .GroupBy(q => (
                    Category: balanceCategories ? q.CanonicalCategory : "all",
                    Difficulty: balanceDifficulties ? q.Difficulty.ToString() : "all"))
                .Select(g => new Queue<Domain.Entities.Question>(g.OrderBy(q => q.Id)))
                .ToList();

            var selected = new List<Domain.Entities.Question>(count);
            while (selected.Count < count && groups.Count > 0)
            {
                foreach (var group in groups.ToList())
                {
                    if (group.Count == 0)
                    {
                        groups.Remove(group);
                        continue;
                    }

                    selected.Add(group.Dequeue());
                    if (selected.Count == count) break;
                }
            }

            return selected;
        }

        private static List<FacetCountDto> BuildFacet(IEnumerable<string?> values) =>
            values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new FacetCountDto(g.Key, g.Count()))
                .OrderBy(x => x.Key)
                .ToList();

        private sealed record QuestionTaxonomyFilters(
            IEnumerable<string>? Categories = null,
            IEnumerable<QuestionDifficulty>? Difficulties = null,
            IEnumerable<string>? Subjects = null,
            IEnumerable<string>? Topics = null,
            IEnumerable<string>? GradeBands = null,
            IEnumerable<string>? AgeGroups = null,
            IEnumerable<string>? Audiences = null,
            IEnumerable<string>? Datasets = null,
            IEnumerable<string>? Tags = null);
    }
}
