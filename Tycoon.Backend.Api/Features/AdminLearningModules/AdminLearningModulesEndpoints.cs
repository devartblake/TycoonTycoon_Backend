using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.LearningModules;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminLearningModules
{
    public static class AdminLearningModulesEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/modules").WithTags("Admin/LearningModules").WithOpenApi();

            // List all modules (including unpublished)
            g.MapGet("", async (
                [FromQuery] string? category,
                [FromQuery] bool? isPublished,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var list = await mediator.Send(
                    new AdminListLearningModules(category, isPublished), ct);
                return Results.Ok(list);
            });

            // Create a new module (starts unpublished)
            g.MapPost("", async (
                [FromBody] CreateLearningModuleRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminCreateLearningModule(req), ct);
                return Results.Created($"/admin/modules/{dto.Id}", new { id = dto.Id });
            });

            // Update module fields
            g.MapPatch("/{id:guid}", async (
                Guid id,
                [FromBody] UpdateLearningModuleRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new AdminUpdateLearningModule(id, req), ct);
                return dto is null
                    ? AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found.")
                    : Results.Ok(dto);
            });

            // Publish a module (makes it visible to players)
            g.MapPatch("/{id:guid}/publish", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var ok = await mediator.Send(new AdminPublishLearningModule(id), ct);
                return ok
                    ? Results.Ok(new { id, isPublished = true })
                    : AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found.");
            });

            // Unpublish a module (hides from players, keeps data)
            g.MapPatch("/{id:guid}/unpublish", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var ok = await mediator.Send(new AdminUnpublishLearningModule(id), ct);
                return ok
                    ? Results.Ok(new { id, isPublished = false })
                    : AdminApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found.");
            });

            // Add a lesson (question + explanation) to a module
            g.MapPost("/{id:guid}/lessons", async (
                Guid id,
                [FromBody] AddModuleLessonRequest req,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new AdminAddLesson(id, req), ct);
                return result.Success
                    ? Results.Created($"/admin/modules/{id}/lessons/{result.LessonId}",
                        new { lessonId = result.LessonId })
                    : AdminApiResponses.Error(StatusCodes.Status400BadRequest,
                        "VALIDATION_ERROR", result.Error ?? "Could not add lesson.");
            });

            // Remove a lesson from a module
            g.MapDelete("/{id:guid}/lessons/{lessonId:guid}", async (
                Guid id,
                Guid lessonId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var ok = await mediator.Send(new AdminRemoveLesson(id, lessonId), ct);
                return ok
                    ? Results.NoContent()
                    : AdminApiResponses.Error(StatusCodes.Status404NotFound,
                        "NOT_FOUND", "Lesson not found.");
            });
        }
    }
}
