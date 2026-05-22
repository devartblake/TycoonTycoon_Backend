namespace Synaptix.Shared.Contracts.Dtos;

// ── Experiment management ────────────────────────────────────────────────────

public sealed record ExperimentDto(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    string Status,
    decimal AllocationPercent,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    IReadOnlyList<ExperimentVariantDto> Variants,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record ExperimentVariantDto(
    Guid Id,
    string Key,
    string Name,
    decimal Weight,
    bool IsControl,
    Dictionary<string, object> Config
);

// ── Assignment (returned to clients) ────────────────────────────────────────

public sealed record ExperimentAssignmentDto(
    string ExperimentKey,
    string VariantKey,
    bool IsControl,
    Dictionary<string, object> Config
);

/// <summary>
/// Bulk response: all active experiment assignments for one player.
/// Clients pass their playerId once and receive every running experiment they are enrolled in.
/// </summary>
public sealed record PlayerExperimentsDto(
    Guid PlayerId,
    IReadOnlyList<ExperimentAssignmentDto> Assignments
);

// ── Admin write requests ────────────────────────────────────────────────────

public sealed record CreateExperimentRequest(
    string Key,
    string Name,
    string? Description,
    decimal? AllocationPercent,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    IReadOnlyList<CreateExperimentVariantRequest> Variants
);

public sealed record CreateExperimentVariantRequest(
    string Key,
    string Name,
    decimal Weight,
    bool IsControl,
    Dictionary<string, object>? Config
);

public sealed record UpdateExperimentRequest(
    string? Name,
    string? Description,
    string? Status,
    decimal? AllocationPercent,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt
);

// ── Admin analytics ─────────────────────────────────────────────────────────

public sealed record ExperimentResultsDto(
    Guid ExperimentId,
    string ExperimentKey,
    string Status,
    IReadOnlyList<ExperimentVariantResultDto> Variants,
    int TotalAssignments,
    DateTimeOffset GeneratedAt
);

public sealed record ExperimentVariantResultDto(
    string VariantKey,
    bool IsControl,
    int Assignments,
    int Impressions,
    int Outcomes,
    double ImpressionRate,
    double OutcomeRate,
    double LiftVsControl
);
