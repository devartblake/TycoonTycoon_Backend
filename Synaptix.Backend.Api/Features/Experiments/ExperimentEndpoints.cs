using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Experiments;

namespace Synaptix.Backend.Api.Features.Experiments;

public static class ExperimentEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/experiments")
            .WithTags("Experiments")
            .RequireAuthorization()
            .AddEndpointFilter(async (ctx, next) =>
            {
                var flags = ctx.HttpContext.RequestServices.GetRequiredService<FeatureFlagService>();
                if (!await flags.IsEnabledAsync("experiments_enabled", ctx.HttpContext.RequestAborted))
                    return Results.Json(new { error = new { code = "FeatureDisabled", message = "This feature is not available in the current release.", details = new { } } }, statusCode: StatusCodes.Status403Forbidden);
                return await next(ctx);
            });

        // Bootstrap all active experiment assignments for a player at session start
        g.MapGet("/player/{playerId:guid}", GetAllAssignments);

        // Single-experiment assignment (used when a specific feature checks its own flag)
        g.MapGet("/player/{playerId:guid}/{experimentKey}", GetAssignment);

        // Telemetry — called when the player actually sees the variant UI
        g.MapPost("/player/{playerId:guid}/{experimentKey}/impression", RecordImpression);

        // Telemetry — called when the player converts (purchase, completion, etc.)
        g.MapPost("/player/{playerId:guid}/{experimentKey}/outcome", RecordOutcome);
    }

    private static async Task<IResult> GetAllAssignments(
        Guid playerId, IExperimentService experiments, CancellationToken ct)
    {
        var result = await experiments.GetAllAssignmentsAsync(playerId, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAssignment(
        Guid playerId, string experimentKey, IExperimentService experiments, CancellationToken ct)
    {
        var assignment = await experiments.GetAssignmentAsync(playerId, experimentKey, ct);
        if (assignment is null)
            return Results.Ok(new { enrolled = false, experimentKey });
        return Results.Ok(new { enrolled = true, experimentKey, assignment });
    }

    private static async Task<IResult> RecordImpression(
        Guid playerId, string experimentKey, IExperimentService experiments, CancellationToken ct)
    {
        await experiments.RecordImpressionAsync(playerId, experimentKey, ct);
        return Results.Accepted();
    }

    private static async Task<IResult> RecordOutcome(
        Guid playerId, string experimentKey,
        IExperimentService experiments,
        CancellationToken ct)
    {
        await experiments.RecordOutcomeAsync(playerId, experimentKey, null, ct);
        return Results.Accepted();
    }
}
