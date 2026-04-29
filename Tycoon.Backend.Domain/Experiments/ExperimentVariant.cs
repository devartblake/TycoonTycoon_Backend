namespace Tycoon.Backend.Domain.Experiments;

/// <summary>
/// A named variant within an experiment (e.g., "control", "treatment_a").
/// Weights are relative — they are normalised to 100 at assignment time.
/// </summary>
public sealed class ExperimentVariant
{
    public Guid Id { get; set; }
    public Guid ExperimentId { get; set; }

    /// <summary>Stable variant slug used in code (e.g. "control", "treatment_a").</summary>
    public string Key { get; set; } = "";

    public string Name { get; set; } = "";

    /// <summary>Relative traffic weight (0–100). Variants are normalised so weights sum to 100.</summary>
    public decimal Weight { get; set; } = 50m;

    /// <summary>True for the baseline variant — used to compute lift in analytics.</summary>
    public bool IsControl { get; set; } = false;

    /// <summary>Variant-specific configuration payload delivered to clients alongside the assignment.</summary>
    public string ConfigJson { get; set; } = "{}";

    public Experiment? Experiment { get; set; }
}
