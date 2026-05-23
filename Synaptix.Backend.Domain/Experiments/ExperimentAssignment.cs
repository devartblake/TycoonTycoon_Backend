namespace Synaptix.Backend.Domain.Experiments;

/// <summary>
/// Persistent record of which variant a player was assigned to in an experiment.
/// Assignments are deterministic and created on first call; never re-randomised.
/// </summary>
public sealed class ExperimentAssignment
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid ExperimentId { get; set; }

    /// <summary>Denormalised for fast lookups without joining Experiment.</summary>
    public string ExperimentKey { get; set; } = "";

    public string VariantKey { get; set; } = "";

    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Set on first RecordImpression call — when the player actually saw this variant.</summary>
    public DateTimeOffset? FirstSeenAt { get; set; }

    public int ImpressionCount { get; set; } = 0;
    public int OutcomeCount { get; set; } = 0;

    /// <summary>JSON bag of outcome metrics (e.g. {"purchased": true, "revenue": 4.99}).</summary>
    public string OutcomeJson { get; set; } = "{}";
}
