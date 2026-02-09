using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Application.Questions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminQuestions
{
    public static class AdminQuestionsEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/admin/questions").WithTags("Admin/Questions").WithOpenApi();

            g.MapGet("/", async (
                [FromQuery] string? search,
                [FromQuery] string? tags,
                [FromQuery] TagFilterMode tagMode,
                [FromQuery] string? category,
                [FromQuery] QuestionDifficulty? difficulty,
                [FromQuery] string? sort,
                [FromQuery] int page,
                [FromQuery] int pageSize,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var tagList = (tags ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                var dto = await mediator.Send(new AdminListQuestions(
                    Search: search,
                    Tags: tagList,
                    TagMode: tagMode == 0 ? TagFilterMode.Any : tagMode,
                    Category: category,
                    Difficulty: difficulty,
                    Sort: string.IsNullOrWhiteSpace(sort) ? "updated_desc" : sort!,
                    Page: page == 0 ? 1 : page,
                    PageSize: pageSize == 0 ? 30 : pageSize
                ), ct);

                return Results.Ok(dto);
            });

            g.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminGetQuestion(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            g.MapPost("/", async ([FromBody] CreateQuestionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminCreateQuestion(req), ct);
                return Results.Ok(dto);
            });

            g.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateQuestionRequest req, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminUpdateQuestion(id, req), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            });

            g.MapDelete("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var ok = await mediator.Send(new AdminDeleteQuestion(id), ct);
                return ok ? Results.NoContent() : Results.NotFound();
            });

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

            // Export JSON (grid-friendly for your admin export)
            g.MapGet("/export", async (
                [FromQuery] string? search,
                [FromQuery] string? tags,
                [FromQuery] TagFilterMode tagMode,
                [FromQuery] string? category,
                [FromQuery] QuestionDifficulty? difficulty,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var tagList = (tags ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();

                // Reuse list query but request a large page
                var dto = await mediator.Send(new AdminListQuestions(
                    Search: search,
                    Tags: tagList,
                    TagMode: tagMode == 0 ? TagFilterMode.Any : tagMode,
                    Category: category,
                    Difficulty: difficulty,
                    Sort: "updated_desc",
                    Page: 1,
                    PageSize: 1000
                ), ct);

                return Results.Ok(dto);
            });
        }
    }
}
