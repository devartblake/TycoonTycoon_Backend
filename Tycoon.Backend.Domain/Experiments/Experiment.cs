namespace Tycoon.Backend.Domain.Experiments;

/// <summary>
/// An A/B experiment definition. Status lifecycle: draft → running → paused → completed.
/// </summary>
public sealed class Experiment
{
    public Guid Id { get; set; }

    /// <summary>Stable slug used in application code to look up this experiment (e.g. "store-cta-color").</summary>
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    /// <summary>draft | running | paused | completed</summary>
    public string Status { get; set; } = "draft";

    /// <summary>Percentage of eligible players to enroll (0–100). Remaining players get no variant (control by exclusion).</summary>
    public decimal AllocationPercent { get; set; } = 100m;

    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }

    /// <summary>Arbitrary operator-defined metadata (hypothesis, ticket links, etc.).</summary>
    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ExperimentVariant> Variants { get; set; } = [];
}
