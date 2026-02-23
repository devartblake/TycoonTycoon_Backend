using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Questions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminQuestions
{
    public static class AdminQuestionsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            // Contract-compliant route surface: /admin/questions
            var g = admin.MapGroup("/questions").WithTags("Admin/Questions").WithOpenApi();

            // Backward-compatible legacy route surface: /admin/admin/questions
            var legacy = admin.MapGroup("/admin/questions").WithTags("Admin/Questions (Legacy)").WithOpenApi();

            MapRoutes(g);
            MapRoutes(legacy);
        }

        private static void MapRoutes(RouteGroupBuilder g)
        {
            g.MapGet("", async (
                [FromQuery] string? q,
                [FromQuery] string? category,
                [FromQuery] string[]? tag,
                [FromQuery] string? sortBy,
                [FromQuery] string? sortOrder,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var tags = tag?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
                var normalizedSort = BuildSort(sortBy, sortOrder);

                var dto = await mediator.Send(new AdminListQuestions(
                    Search: q,
                    Tags: tags,
                    TagMode: TagFilterMode.Any,
                    Category: category,
                    Difficulty: null,
                    Sort: normalizedSort,
                    Page: page <= 0 ? 1 : page,
                    PageSize: page is <= 0 ? 25 : Math.Clamp(pageSize, 1, 200)
                ), ct);

                var pageEnvelope = AdminApiResponses.Page(dto.Items, dto.Page, dto.PageSize, dto.Total);
                return Results.Ok(pageEnvelope);
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
                return Results.Created($"/admin/questions/{dto.Id}", new { id = dto.Id });
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

            g.MapPost("/bulk", async ([FromBody] ImportQuestionsRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminImportQuestions(req), ct);
                return Results.Ok(dto);
            });

            g.MapGet("/export", async (
                [FromQuery] string? q,
                [FromQuery] string? category,
                [FromQuery] string[]? tag,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var tags = tag?.Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new List<string>();
                var dto = await mediator.Send(new AdminListQuestions(
                    Search: q,
                    Tags: tags,
                    TagMode: TagFilterMode.Any,
                    Category: category,
                    Difficulty: null,
                    Sort: "updated_desc",
                    Page: 1,
                    PageSize: 1000
                ), ct);

                return Results.Ok(dto.Items);
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
        }

        private static string BuildSort(string? sortBy, string? sortOrder)
        {
            var by = string.IsNullOrWhiteSpace(sortBy) ? "updated" : sortBy.Trim().ToLowerInvariant();
            var order = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
            return $"{by}_{order}";
        }
    }
}
