using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Questions
{
    public static class QuestionsEndpoints
    {
        public static void Map(WebApplication app)
        {
            // Public gameplay question contract.
            // /questions is the supported backend surface for play-oriented retrieval and grading.
            // Legacy /quiz routes are intentionally not mapped here.
            var g = app.MapGroup("/questions").WithTags("Questions").WithOpenApi();

            g.MapGet("/set", GetQuestionSet);
            g.MapPost("/check", CheckAnswer);
            g.MapPost("/check-batch", CheckAnswersBatch);
        }

        /// <summary>
        /// Serves a random set of questions for gameplay.
        /// Correct answers are NOT included — use /questions/check for server-side grading.
        /// </summary>
        private static async Task<IResult> GetQuestionSet(
            [FromQuery] string? category,
            [FromQuery] QuestionDifficulty? difficulty,
            [FromQuery] int count,
            IAppDb db,
            CancellationToken ct)
        {
            var dtos = await QueryGameplayQuestionsAsync(
                db,
                count,
                string.IsNullOrWhiteSpace(category) ? null : new[] { category.Trim() },
                difficulty.HasValue ? new[] { difficulty.Value } : null,
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
            IAppDb db,
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

            foreach (var answer in req.Answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                {
                    results.Add(new CheckAnswerResponse(answer.QuestionId, answer.SelectedOptionId, "", false));
                    continue;
                }

                var isCorrect = string.Equals(question.CorrectOptionId, answer.SelectedOptionId, StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correct++;

                results.Add(new CheckAnswerResponse(
                    answer.QuestionId,
                    answer.SelectedOptionId,
                    question.CorrectOptionId,
                    isCorrect));
            }

            return Results.Ok(new CheckAnswersBatchResponse(results, results.Count, correct));
        }

        private static async Task<List<GameplayQuestionDto>> QueryGameplayQuestionsAsync(
            IAppDb db,
            int count,
            IEnumerable<string>? categories,
            IEnumerable<QuestionDifficulty>? difficulties,
            CancellationToken ct)
        {
            var clampedCount = count <= 0 ? 10 : Math.Clamp(count, 1, 50);

            var questions = await BuildApprovedQuestionsQuery(db, categories, difficulties)
                .OrderBy(_ => EF.Functions.Random())
                .Take(clampedCount)
                .ToListAsync(ct);

            return questions.Select(q => new GameplayQuestionDto(
                q.Id,
                q.Text,
                q.Category,
                q.Difficulty,
                q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
                q.MediaKey
            )).ToList();
        }

        private static IQueryable<Domain.Entities.Question> BuildApprovedQuestionsQuery(
            IAppDb db,
            IEnumerable<string>? categories,
            IEnumerable<QuestionDifficulty>? difficulties)
        {
            var query = db.Questions
                .AsNoTracking()
                .Include(q => q.Options)
                .Where(q => q.Status == "Approved")
                .AsQueryable();

            var categoryFilters = categories?
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (categoryFilters is { Length: > 0 })
                query = query.Where(q => categoryFilters.Contains(q.Category));

            var difficultyFilters = difficulties?.Distinct().ToArray();
            if (difficultyFilters is { Length: > 0 })
                query = query.Where(q => difficultyFilters.Contains(q.Difficulty));

            return query;
        }
    }
}
