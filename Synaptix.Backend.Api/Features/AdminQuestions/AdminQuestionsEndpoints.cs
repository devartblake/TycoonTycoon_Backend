using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Application.Questions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminQuestions
{
    public static class AdminQuestionsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            // Contract-compliant route surface: /admin/questions
            var g = admin.MapGroup("/questions").WithTags("Admin/Questions");

            MapRoutes(g);
        }

        private static void MapRoutes(RouteGroupBuilder g)
        {
            g.MapGet("", async (
                [FromQuery] string? q,
                [FromQuery] string? category,
                [FromQuery] string? status,
                [FromQuery] string[]? tag,
                [FromQuery] string[]? tags,
                [FromQuery] string? tagMode,
                [FromQuery] string? sortBy,
                [FromQuery] string? sortOrder,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var normalizedTags = NormalizeTags(tag, tags);
                var normalizedTagMode = NormalizeTagMode(tagMode);
                var normalizedSort = BuildSort(sortBy, sortOrder);

                var dto = await mediator.Send(new AdminListQuestions(
                    Search: q,
                    Tags: normalizedTags,
                    TagMode: normalizedTagMode,
                    Category: category,
                    Status: status,
                    Difficulty: null,
                    Sort: normalizedSort,
                    Page: page <= 0 ? 1 : page,
                    PageSize: pageSize <= 0 ? 25 : Math.Clamp(pageSize, 1, 200)
                ), ct);

                return Results.Ok(dto);
            });

            g.MapGet("/stats", async (IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminQuestionStats(), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/categories", async (IMediator mediator, CancellationToken ct) =>
            {
                var categories = await mediator.Send(new AdminListQuestionCategories(), ct);
                return Results.Ok(categories);
            });

            g.MapPost("/bulk-review", async ([FromBody] BulkReviewQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var verdict = req.Verdict?.Trim().ToLowerInvariant();
                if (verdict is not ("approve" or "reject"))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Verdict must be 'approve' or 'reject'.");

                var dto = await mediator.Send(new AdminBulkReviewQuestions(req.Ids ?? Array.Empty<Guid>(), verdict), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminGetQuestion(id), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(dto);
            });

            g.MapPost("", async ([FromBody] CreateQuestionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminCreateQuestion(req), ct);
                return Results.Created($"/admin/questions/{dto.Id}", dto);
            });

            g.MapPatch("/{id:guid}", async (Guid id, [FromBody] UpdateQuestionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminUpdateQuestion(id, req), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(new { id = dto.Id, updatedAt = DateTime.UtcNow });
            });

            // Legacy support
            g.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateQuestionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminUpdateQuestion(id, req), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(dto);
            });

            g.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var ok = await mediator.Send(new AdminDeleteQuestion(id), ct);
                return ok ? Results.NoContent() : AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.");
            });

            g.MapPost("/{id:guid}/approve", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminSetQuestionStatus(id, "Approved"), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(dto);
            });

            g.MapPost("/{id:guid}/reject", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminSetQuestionStatus(id, "Rejected"), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(dto);
            });

            g.MapPost("/bulk", async ([FromBody] ImportQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminImportQuestions(req), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/export", async (
                [FromQuery] string? q,
                [FromQuery] string? category,
                [FromQuery] string? status,
                [FromQuery] string[]? tag,
                [FromQuery] string[]? tags,
                [FromQuery] string? tagMode,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var normalizedTags = NormalizeTags(tag, tags);
                var normalizedTagMode = NormalizeTagMode(tagMode);
                var dto = await mediator.Send(new AdminListQuestions(
                    Search: q,
                    Tags: normalizedTags,
                    TagMode: normalizedTagMode,
                    Category: category,
                    Status: status,
                    Difficulty: null,
                    Sort: "updated_desc",
                    Page: 1,
                    PageSize: 1000
                ), ct);

                return Results.Ok(dto);
            });

            // Legacy endpoints
            g.MapPost("/bulk-delete", async ([FromBody] BulkDeleteQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminBulkDelete(req), ct);
                return Results.Ok(dto);
            });

            g.MapPost("/import", async ([FromBody] ImportQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminImportQuestions(req), ct);
                return Results.Ok(dto);
            });

            g.MapPost("/import-taxonomy", async ([FromBody] TaxonomyImportQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminImportTaxonomyQuestions(req), ct);
                return Results.Ok(dto);
            });

            g.MapPost("/{id:guid}/taxonomy/suggest", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminSuggestQuestionTaxonomy(id), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found or Sidecar taxonomy suggestions are unavailable.")
                    : Results.Ok(dto);
            });

            g.MapGet("/{id:guid}/taxonomy/suggestions", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminListQuestionTaxonomySuggestions(id), ct);
                return Results.Ok(dto);
            });

            g.MapPost("/{id:guid}/taxonomy/apply", async (
                Guid id,
                [FromBody] ApplyQuestionTaxonomySuggestionRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminApplyQuestionTaxonomySuggestion(id, req), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Resource not found.")
                    : Results.Ok(dto);
            });

            g.MapPost("/estimate-difficulty", async (
                [FromBody] QuestionDifficultyEstimateRequest req,
                IConfiguration cfg,
                IHttpClientFactory httpClientFactory,
                CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(req.Text))
                    return AdminApiResponses.Error(StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "Text is required.");

                var deployedModelUrl = cfg["MlModels:QuestionDifficultyUrl"];
                var deployedModelApiKey = cfg["MlModels:ApiKey"];
                if (!string.IsNullOrWhiteSpace(deployedModelUrl))
                {
                    var deployedPayload = await TryEstimateDifficultyFromHttpAsync(
                        httpClientFactory,
                        deployedModelUrl,
                        req.Text,
                        ct,
                        deployedModelApiKey);
                    if (deployedPayload is not null)
                    {
                        var mapped = MapDifficulty(deployedPayload.Difficulty, deployedPayload.Score);
                        return Results.Ok(new QuestionDifficultyEstimateResponse(mapped, deployedPayload.Score, "deployed-model"));
                    }
                }

                var enableSidecar = cfg.GetValue("SidecarInference:EnableQuestionDifficultyEstimator", false);
                var sidecarUrl = cfg["SidecarInference:QuestionDifficultyUrl"];

                if (enableSidecar && !string.IsNullOrWhiteSpace(sidecarUrl))
                {
                    var sidecarPayload = await TryEstimateDifficultyFromHttpAsync(
                        httpClientFactory,
                        sidecarUrl,
                        req.Text,
                        ct);
                    if (sidecarPayload is not null)
                    {
                        var mapped = MapDifficulty(sidecarPayload.Difficulty, sidecarPayload.Score);
                        return Results.Ok(new QuestionDifficultyEstimateResponse(mapped, sidecarPayload.Score, "sidecar"));
                    }
                }

                var heuristic = EstimateDifficultyHeuristic(req.Text);
                return Results.Ok(new QuestionDifficultyEstimateResponse(heuristic, 0.55m, "heuristic"));
            });
        }

        private static string BuildSort(string? sortBy, string? sortOrder)
        {
            var by = string.IsNullOrWhiteSpace(sortBy) ? "updated" : sortBy.Trim().ToLowerInvariant();
            var order = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            return $"{by}_{order}";
        }

        private static List<string> NormalizeTags(string[]? tag, string[]? tags)
        {
            return (tags is { Length: > 0 } ? tags : tag)
                ?.Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .ToList()
                ?? new List<string>();
        }

        private static TagFilterMode NormalizeTagMode(string? tagMode)
        {
            return string.Equals(tagMode, "All", StringComparison.OrdinalIgnoreCase)
                ? TagFilterMode.All
                : TagFilterMode.Any;
        }

        private static QuestionDifficulty EstimateDifficultyHeuristic(string text)
        {
            var length = text.Trim().Length;
            return length switch
            {
                < 55 => QuestionDifficulty.Easy,
                < 110 => QuestionDifficulty.Medium,
                < 180 => QuestionDifficulty.Hard,
                _ => QuestionDifficulty.Expert
            };
        }

        private static QuestionDifficulty MapDifficulty(string? difficulty, decimal score)
        {
            if (!string.IsNullOrWhiteSpace(difficulty)
                && Enum.TryParse<QuestionDifficulty>(difficulty, true, out var parsed))
                return parsed;

            if (score < 0.35m) return QuestionDifficulty.Easy;
            if (score < 0.60m) return QuestionDifficulty.Medium;
            if (score < 0.82m) return QuestionDifficulty.Hard;
            return QuestionDifficulty.Expert;
        }

        private static async Task<SidecarDifficultyPayload?> TryEstimateDifficultyFromHttpAsync(
            IHttpClientFactory httpClientFactory,
            string url,
            string text,
            CancellationToken ct,
            string? bearerToken = null)
        {
            try
            {
                var http = httpClientFactory.CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(new { text })
                };

                if (!string.IsNullOrWhiteSpace(bearerToken))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

                using var resp = await http.SendAsync(req, ct);
                if (!resp.IsSuccessStatusCode)
                    return null;

                return await resp.Content.ReadFromJsonAsync<SidecarDifficultyPayload>(cancellationToken: ct);
            }
            catch
            {
                return null;
            }
        }

        private sealed record SidecarDifficultyPayload(string? Difficulty, decimal Score);
    }
}
