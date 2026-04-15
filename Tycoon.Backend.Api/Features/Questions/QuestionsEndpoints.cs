using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Questions
{
    public static class QuestionsEndpoints
    {
        private sealed record CheckAnswerCompatibilityRequest(
            Guid QuestionId,
            string? SelectedOptionId,
            string? SelectedAnswer,
            string? Answer);

        private sealed record CheckAnswersBatchCompatibilityRequest(
            IReadOnlyList<CheckAnswerCompatibilityRequest>? Answers);

        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/questions").WithTags("Questions").WithOpenApi();

            g.MapGet("/set", GetQuestionSet);
            g.MapGet("/mixed", GetMixedQuestionSetCompatibility);
            g.MapPost("/check", CheckAnswer);
            g.MapPost("/check-batch", CheckAnswersBatch);

            var quiz = app.MapGroup("/quiz").WithTags("Quiz Compatibility").WithOpenApi();
            quiz.MapGet("/daily", GetDailyQuestionSetCompatibility);
            quiz.MapGet("/mixed", GetMixedQuestionSetCompatibility);
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

            return Results.Ok(new
            {
                questions = dtos,
                count = dtos.Count,
                source = "backend"
            });
        }

        private static async Task<IResult> GetMixedQuestionSetCompatibility(
            [FromQuery] string? categories,
            [FromQuery] string? difficulties,
            [FromQuery] int count,
            [FromQuery] bool? balanceDifficulties,
            IAppDb db,
            CancellationToken ct)
        {
            var categoryFilters = SplitCsv(categories);
            var difficultyFilters = ParseDifficultyCsv(difficulties);

            var dtos = await QueryGameplayQuestionsAsync(
                db,
                count,
                categoryFilters,
                difficultyFilters,
                ct);

            return Results.Ok(new
            {
                items = dtos,
                questions = dtos,
                data = dtos,
                meta = new
                {
                    source = "backend",
                    count = dtos.Count,
                    balanceDifficulties = balanceDifficulties ?? false,
                    mode = "mixed"
                }
            });
        }

        private static async Task<IResult> GetDailyQuestionSetCompatibility(
            [FromQuery] int count,
            IAppDb db,
            CancellationToken ct)
        {
            var dtos = await QueryGameplayQuestionsAsync(db, count, null, null, ct);

            return Results.Ok(new
            {
                items = dtos,
                questions = dtos,
                data = dtos,
                meta = new
                {
                    source = "backend",
                    count = dtos.Count,
                    mode = "daily"
                }
            });
        }

        /// <summary>
        /// Check a single answer server-side. Returns whether the selected option is correct.
        /// </summary>
        private static async Task<IResult> CheckAnswer(
            [FromBody] CheckAnswerCompatibilityRequest req,
            IAppDb db,
            CancellationToken ct)
        {
            var question = await db.Questions
                .AsNoTracking()
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == req.QuestionId, ct);

            if (question is null)
                return ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Question not found.");

            var selectedOptionId = ResolveSelectedOptionId(question, req);
            var correctAnswer = question.Options
                .FirstOrDefault(o => string.Equals(o.OptionId, question.CorrectOptionId, StringComparison.OrdinalIgnoreCase))
                ?.Text ?? string.Empty;

            var isCorrect = string.Equals(question.CorrectOptionId, selectedOptionId, StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new
            {
                questionId = req.QuestionId,
                selectedOptionId,
                correctOptionId = question.CorrectOptionId,
                isCorrect,
                correctAnswer,
                expectedAnswer = correctAnswer,
                source = "backend"
            });
        }

        /// <summary>
        /// Batch check answers for a full round/match. Returns per-question results and totals.
        /// </summary>
        private static async Task<IResult> CheckAnswersBatch(
            [FromBody] CheckAnswersBatchCompatibilityRequest req,
            IAppDb db,
            CancellationToken ct)
        {
            var answers = req.Answers ?? Array.Empty<CheckAnswerCompatibilityRequest>();

            if (answers.Count == 0)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "At least one answer is required.");

            if (answers.Count > 50)
                return ApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Maximum 50 answers per batch.");

            var questionIds = answers.Select(a => a.QuestionId).Distinct().ToList();

            var questions = await db.Questions
                .AsNoTracking()
                .Include(q => q.Options)
                .Where(q => questionIds.Contains(q.Id))
                .ToDictionaryAsync(q => q.Id, ct);

            var results = new List<object>(answers.Count);
            var correct = 0;

            foreach (var answer in answers)
            {
                if (!questions.TryGetValue(answer.QuestionId, out var question))
                {
                    results.Add(new
                    {
                        questionId = answer.QuestionId,
                        selectedOptionId = answer.SelectedOptionId ?? string.Empty,
                        correctOptionId = string.Empty,
                        isCorrect = false,
                        correctAnswer = string.Empty,
                        source = "backend"
                    });
                    continue;
                }

                var selectedOptionId = ResolveSelectedOptionId(question, answer);
                var correctAnswer = question.Options
                    .FirstOrDefault(o => string.Equals(o.OptionId, question.CorrectOptionId, StringComparison.OrdinalIgnoreCase))
                    ?.Text ?? string.Empty;

                var isCorrect = string.Equals(question.CorrectOptionId, selectedOptionId, StringComparison.OrdinalIgnoreCase);
                if (isCorrect) correct++;

                results.Add(new
                {
                    questionId = answer.QuestionId,
                    selectedOptionId,
                    correctOptionId = question.CorrectOptionId,
                    isCorrect,
                    correctAnswer,
                    expectedAnswer = correctAnswer,
                    source = "backend"
                });
            }

            return Results.Ok(new
            {
                results,
                items = results,
                answers = results,
                data = results,
                total = results.Count,
                correct,
                source = "backend"
            });
        }

        private static IQueryable<Question> BuildApprovedQuestionsQuery(
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

        private static string ResolveSelectedOptionId(Question question, CheckAnswerCompatibilityRequest req)
        {
            if (!string.IsNullOrWhiteSpace(req.SelectedOptionId))
                return req.SelectedOptionId.Trim();

            var answerText = req.SelectedAnswer ?? req.Answer;
            if (string.IsNullOrWhiteSpace(answerText))
                return string.Empty;

            var normalized = answerText.Trim();

            var option = question.Options.FirstOrDefault(o =>
                string.Equals(o.OptionId, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(o.Text, normalized, StringComparison.OrdinalIgnoreCase));

            return option?.OptionId ?? string.Empty;
        }

        private static IReadOnlyList<string>? SplitCsv(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return tokens.Length == 0 ? null : tokens;
        }

        private static IReadOnlyList<QuestionDifficulty>? ParseDifficultyCsv(string? value)
        {
            var tokens = SplitCsv(value);
            if (tokens is null)
                return null;

            var parsed = new List<QuestionDifficulty>();

            foreach (var token in tokens)
            {
                if (Enum.TryParse<QuestionDifficulty>(token, ignoreCase: true, out var byName))
                {
                    parsed.Add(byName);
                    continue;
                }

                if (int.TryParse(token, out var raw) && Enum.IsDefined(typeof(QuestionDifficulty), raw))
                    parsed.Add((QuestionDifficulty)raw);
            }

            return parsed.Count == 0 ? null : parsed;
        }
    }
}
