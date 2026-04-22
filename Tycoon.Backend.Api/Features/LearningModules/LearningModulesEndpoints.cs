using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.LearningModules;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.LearningModules
{
    public static class LearningModulesEndpoints
    {
        public static void Map(WebApplication app)
        {
            // Public learning contract.
            // /modules is the supported backend surface for guided mastery and lesson progression.
            var g = app.MapGroup("/modules").WithTags("LearningModules").WithOpenApi();

            // Browse published modules (public)
            // Optional: ?playerId={guid} populates isCompleted per module
            // Optional: ?category=Science &difficulty=2
            g.MapGet("", async (
                [FromQuery] Guid? playerId,
                [FromQuery] string? category,
                [FromQuery] QuestionDifficulty? difficulty,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var list = await mediator.Send(
                    new ListLearningModules(playerId, category, difficulty), ct);
                return Results.Ok(list);
            });

            // Recommended published modules for a player or anonymous learner.
            // Optional: ?playerId={guid}&count=5
            g.MapGet("/recommended", async (
                [FromQuery] Guid? playerId,
                [FromQuery] int count,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetRecommendedLearningModules(playerId, count), ct);
                return Results.Ok(dto);
            });

            // Progress summary across the published learning catalog for one player.
            g.MapGet("/progress/{playerId:guid}", async (
                Guid playerId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetLearningModuleProgress(playerId), ct);
                return Results.Ok(dto);
            });

            // Module overview (public)
            g.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            {
                var dto = await mediator.Send(new GetLearningModule(id), ct);
                return dto is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found.")
                    : Results.Ok(dto);
            });

            // Ordered lessons with questions + correct answers exposed (learning context)
            g.MapGet("/{id:guid}/lessons", async (
                Guid id,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var lessons = await mediator.Send(new GetModuleLessons(id), ct);
                return lessons is null
                    ? ApiResponses.Error(StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found.")
                    : Results.Ok(lessons);
            });

            // Complete a module and grant reward (idempotent)
            // POST /modules/{id}/complete?playerId={guid}
            g.MapPost("/{id:guid}/complete", async (
                Guid id,
                [FromQuery] Guid playerId,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(new CompleteModule(id, playerId), ct);

                return result.Status switch
                {
                    "ModuleNotFound" => ApiResponses.Error(
                        StatusCodes.Status404NotFound, "NOT_FOUND", "Module not found."),
                    _ => Results.Ok(result)
                };
            });
        }
    }
}
