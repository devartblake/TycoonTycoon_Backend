using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Experiments;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Experiments;

public sealed class ExperimentService : IExperimentService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly IAppDb _db;

    public ExperimentService(IAppDb db)
    {
        _db = db;
    }

    public async Task<ExperimentAssignmentDto?> GetAssignmentAsync(
        Guid playerId, string experimentKey, CancellationToken ct = default)
    {
        var experiment = await _db.Experiments
            .AsNoTracking()
            .Include(e => e.Variants)
            .FirstOrDefaultAsync(e => e.Key == experimentKey && e.Status == "running", ct);

        if (experiment is null || experiment.Variants.Count == 0)
            return null;

        // Allocation gating: deterministic bucket 0.0–1.0 vs allocation threshold
        var bucket = GetBucket(playerId, experimentKey);
        if (bucket > (double)(experiment.AllocationPercent / 100m))
            return null;

        // Return existing assignment if one already exists
        var existing = await _db.ExperimentAssignments
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.ExperimentId == experiment.Id, ct);

        if (existing is not null)
            return ToDto(existing, experiment);

        // Assign to a variant using deterministic bucket mapped over normalised weights
        var variant = SelectVariant(experiment.Variants, bucket);
        if (variant is null)
            return null;

        var assignment = new ExperimentAssignment
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            ExperimentId = experiment.Id,
            ExperimentKey = experimentKey,
            VariantKey = variant.Key,
            AssignedAt = DateTimeOffset.UtcNow
        };

        _db.ExperimentAssignments.Add(assignment);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Race: another request created the assignment concurrently — re-load it
            _db.Entry(assignment).State = EntityState.Detached;
            existing = await _db.ExperimentAssignments
                .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.ExperimentId == experiment.Id, ct);
            if (existing is null)
                return null;
            return ToDto(existing, experiment);
        }

        return ToDto(assignment, experiment);
    }

    public async Task<PlayerExperimentsDto> GetAllAssignmentsAsync(
        Guid playerId, CancellationToken ct = default)
    {
        var runningExperiments = await _db.Experiments
            .AsNoTracking()
            .Include(e => e.Variants)
            .Where(e => e.Status == "running")
            .ToListAsync(ct);

        var results = new List<ExperimentAssignmentDto>();

        foreach (var experiment in runningExperiments)
        {
            var dto = await GetAssignmentAsync(playerId, experiment.Key, ct);
            if (dto is not null)
                results.Add(dto);
        }

        return new PlayerExperimentsDto(playerId, results);
    }

    public async Task RecordImpressionAsync(
        Guid playerId, string experimentKey, CancellationToken ct = default)
    {
        var assignment = await _db.ExperimentAssignments
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.ExperimentKey == experimentKey, ct);
        if (assignment is null)
            return;

        assignment.ImpressionCount++;
        assignment.FirstSeenAt ??= DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordOutcomeAsync(
        Guid playerId, string experimentKey,
        Dictionary<string, object>? outcomeData = null,
        CancellationToken ct = default)
    {
        var assignment = await _db.ExperimentAssignments
            .FirstOrDefaultAsync(a => a.PlayerId == playerId && a.ExperimentKey == experimentKey, ct);
        if (assignment is null)
            return;

        assignment.OutcomeCount++;
        if (outcomeData is { Count: > 0 })
        {
            var existing = ParseJson(assignment.OutcomeJson);
            foreach (var kv in outcomeData)
                existing[kv.Key] = kv.Value;
            assignment.OutcomeJson = JsonSerializer.Serialize(existing, _json);
        }

        await _db.SaveChangesAsync(ct);
    }

    // ── Internals ────────────────────────────────────────────────────────────

    /// <summary>
    /// Deterministic bucket [0.0, 1.0) derived from MD5(playerId:experimentKey).
    /// The same player always maps to the same bucket for a given experiment.
    /// </summary>
    private static double GetBucket(Guid playerId, string experimentKey)
    {
        var input = $"{playerId}:{experimentKey}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var value = BitConverter.ToUInt32(hash, 0);
        return (double)value / uint.MaxValue;
    }

    /// <summary>
    /// Maps a bucket value to a variant using normalised cumulative weights.
    /// Variants are sorted by Key for determinism regardless of insertion order.
    /// </summary>
    private static ExperimentVariant? SelectVariant(
        ICollection<ExperimentVariant> variants, double bucket)
    {
        var sorted = variants.OrderBy(v => v.Key).ToList();
        var totalWeight = sorted.Sum(v => (double)v.Weight);
        if (totalWeight <= 0)
            return null;

        var cursor = 0.0;
        foreach (var variant in sorted)
        {
            cursor += (double)variant.Weight / totalWeight;
            if (bucket < cursor)
                return variant;
        }

        return sorted.Last();
    }

    private ExperimentAssignmentDto ToDto(ExperimentAssignment assignment, Experiment experiment)
    {
        var variant = experiment.Variants.FirstOrDefault(v => v.Key == assignment.VariantKey);
        return new ExperimentAssignmentDto(
            ExperimentKey: assignment.ExperimentKey,
            VariantKey: assignment.VariantKey,
            IsControl: variant?.IsControl ?? false,
            Config: ParseJson(variant?.ConfigJson ?? "{}")
                .ToDictionary(k => k.Key, k => k.Value ?? (object)"")
        );
    }

    private static Dictionary<string, object?> ParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, _json) ?? []; }
        catch { return []; }
    }
}
