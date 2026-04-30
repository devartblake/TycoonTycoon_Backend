using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Experiments;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminExperiments;

public static class AdminExperimentEndpoints
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/experiments").WithTags("Admin/Experiments").WithOpenApi();

        g.MapGet("/", ListExperiments);
        g.MapGet("/{id:guid}", GetExperiment);
        g.MapPost("/", CreateExperiment);
        g.MapPut("/{id:guid}", UpdateExperiment);
        g.MapPost("/{id:guid}/start", StartExperiment);
        g.MapPost("/{id:guid}/pause", PauseExperiment);
        g.MapPost("/{id:guid}/complete", CompleteExperiment);
        g.MapGet("/{id:guid}/results", GetResults);
        g.MapDelete("/{id:guid}", DeleteExperiment);
    }

    private static async Task<IResult> ListExperiments(
        IAppDb db, string? status, CancellationToken ct)
    {
        var q = db.Experiments.AsNoTracking().Include(e => e.Variants).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(e => e.Status == status);

        var list = await q.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);
        return Results.Ok(list.Select(ToDto).ToList());
    }

    private static async Task<IResult> GetExperiment(Guid id, IAppDb db, CancellationToken ct)
    {
        var exp = await db.Experiments.AsNoTracking()
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return exp is null ? Results.NotFound() : Results.Ok(ToDto(exp));
    }

    private static async Task<IResult> CreateExperiment(
        [FromBody] CreateExperimentRequest req, IAppDb db, CancellationToken ct)
    {
        if (await db.Experiments.AnyAsync(e => e.Key == req.Key, ct))
            return Results.Conflict(new { error = "experiment_key_exists", key = req.Key });

        if (req.Variants is null || req.Variants.Count < 2)
            return Results.BadRequest(new { error = "min_two_variants" });

        var experiment = new Experiment
        {
            Id = Guid.NewGuid(),
            Key = req.Key.Trim().ToLowerInvariant(),
            Name = req.Name.Trim(),
            Description = req.Description?.Trim() ?? "",
            Status = "draft",
            AllocationPercent = req.AllocationPercent ?? 100m,
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var v in req.Variants)
        {
            experiment.Variants.Add(new ExperimentVariant
            {
                Id = Guid.NewGuid(),
                ExperimentId = experiment.Id,
                Key = v.Key.Trim().ToLowerInvariant(),
                Name = v.Name.Trim(),
                Weight = v.Weight,
                IsControl = v.IsControl,
                ConfigJson = v.Config is not null
                    ? JsonSerializer.Serialize(v.Config, _json)
                    : "{}"
            });
        }

        db.Experiments.Add(experiment);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/admin/experiments/{experiment.Id}", ToDto(experiment));
    }

    private static async Task<IResult> UpdateExperiment(
        Guid id, [FromBody] UpdateExperimentRequest req, IAppDb db, CancellationToken ct)
    {
        var exp = await db.Experiments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exp is null) return Results.NotFound();

        if (req.Name is not null) exp.Name = req.Name.Trim();
        if (req.Description is not null) exp.Description = req.Description.Trim();
        if (req.Status is not null) exp.Status = req.Status;
        if (req.AllocationPercent.HasValue) exp.AllocationPercent = req.AllocationPercent.Value;
        if (req.StartsAt.HasValue) exp.StartsAt = req.StartsAt;
        if (req.EndsAt.HasValue) exp.EndsAt = req.EndsAt;
        exp.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        // Reload variants for response
        var updated = await db.Experiments.AsNoTracking()
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return Results.Ok(ToDto(updated!));
    }

    private static Task<IResult> StartExperiment(Guid id, IAppDb db, CancellationToken ct)
        => SetStatus(id, "running", db, ct);

    private static Task<IResult> PauseExperiment(Guid id, IAppDb db, CancellationToken ct)
        => SetStatus(id, "paused", db, ct);

    private static Task<IResult> CompleteExperiment(Guid id, IAppDb db, CancellationToken ct)
        => SetStatus(id, "completed", db, ct);

    private static async Task<IResult> SetStatus(Guid id, string status, IAppDb db, CancellationToken ct)
    {
        var exp = await db.Experiments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exp is null) return Results.NotFound();
        exp.Status = status;
        exp.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { id, status });
    }

    private static async Task<IResult> GetResults(Guid id, IAppDb db, CancellationToken ct)
    {
        var exp = await db.Experiments.AsNoTracking()
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exp is null) return Results.NotFound();

        var assignments = await db.ExperimentAssignments
            .AsNoTracking()
            .Where(a => a.ExperimentId == id)
            .ToListAsync(ct);

        var total = assignments.Count;
        var controlOutcomeRate = 0.0;

        var variantResults = exp.Variants
            .OrderBy(v => v.Key)
            .Select(v =>
            {
                var group = assignments.Where(a => a.VariantKey == v.Key).ToList();
                var cnt = group.Count;
                var impressions = group.Sum(a => a.ImpressionCount);
                var outcomes = group.Sum(a => a.OutcomeCount);
                var impRate = cnt == 0 ? 0.0 : Math.Round((double)impressions / cnt, 4);
                var outRate = cnt == 0 ? 0.0 : Math.Round((double)outcomes / cnt, 4);
                if (v.IsControl) controlOutcomeRate = outRate;
                return new { v, cnt, impressions, outcomes, impRate, outRate };
            })
            .ToList();

        var results = variantResults.Select(x => new ExperimentVariantResultDto(
            VariantKey: x.v.Key,
            IsControl: x.v.IsControl,
            Assignments: x.cnt,
            Impressions: x.impressions,
            Outcomes: x.outcomes,
            ImpressionRate: x.impRate,
            OutcomeRate: x.outRate,
            LiftVsControl: x.v.IsControl ? 0.0
                : controlOutcomeRate == 0.0 ? 0.0
                : Math.Round((x.outRate - controlOutcomeRate) / controlOutcomeRate, 4)
        )).ToList();

        return Results.Ok(new ExperimentResultsDto(
            ExperimentId: exp.Id,
            ExperimentKey: exp.Key,
            Status: exp.Status,
            Variants: results,
            TotalAssignments: total,
            GeneratedAt: DateTimeOffset.UtcNow));
    }

    private static async Task<IResult> DeleteExperiment(Guid id, IAppDb db, CancellationToken ct)
    {
        var exp = await db.Experiments.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (exp is null) return Results.NotFound();
        if (exp.Status == "running")
            return Results.Conflict(new { error = "cannot_delete_running_experiment" });

        db.Experiments.Remove(exp);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { deleted = true, id });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static ExperimentDto ToDto(Experiment e) =>
        new(e.Id, e.Key, e.Name, e.Description, e.Status, e.AllocationPercent,
            e.StartsAt, e.EndsAt,
            e.Variants.OrderBy(v => v.Key).Select(ToVariantDto).ToList(),
            e.CreatedAt, e.UpdatedAt);

    private static ExperimentVariantDto ToVariantDto(ExperimentVariant v) =>
        new(v.Id, v.Key, v.Name, v.Weight, v.IsControl,
            ParseJson(v.ConfigJson).ToDictionary(k => k.Key, k => k.Value ?? (object)""));

    private static Dictionary<string, object?> ParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, _json) ?? []; }
        catch { return []; }
    }
}
