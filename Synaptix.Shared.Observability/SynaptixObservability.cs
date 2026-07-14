using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Synaptix.Shared.Observability;

/// <summary>
/// Shared observability primitives used across API + workers.
/// Metric instruments dual-publish under <c>synaptix_*</c> (canonical) and
/// <c>tycoon_*</c> / dotted <c>tycoon.*</c> (legacy) for Wave 3 dashboard continuity.
/// Prefer <see cref="SynaptixObservability"/>; <see cref="TycoonObservability"/> is a type alias.
/// </summary>
public static class SynaptixObservability
{
    public const string ServiceName = "synaptix-backend";
    /// <summary>Legacy service id still accepted in Grafana queries.</summary>
    public const string LegacyServiceName = "tycoon-backend";

    public const string ActivitySourceName = "Synaptix.Backend";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public const string MeterName = "Synaptix.Backend.Metrics";
    public static readonly Meter Meter = new(MeterName);

    // ── Rebuild counters (synaptix_* + tycoon_* dual-write) ─────────────────
    public static readonly DualCounter RebuildRunsTotal = Dual(
        "synaptix_rebuild_runs_total", "tycoon_rebuild_runs_total");

    public static readonly DualCounter RebuildDocsReadTotal = Dual(
        "synaptix_rebuild_docs_read_total", "tycoon_rebuild_docs_read_total");

    public static readonly DualCounter RebuildDocsIndexedTotal = Dual(
        "synaptix_rebuild_docs_indexed_total", "tycoon_rebuild_docs_indexed_total");

    public static readonly DualCounter RebuildDocsFailedTotal = Dual(
        "synaptix_rebuild_docs_failed_total", "tycoon_rebuild_docs_failed_total");

    public static readonly DualHistogram RebuildDurationMs = DualHist(
        "synaptix_rebuild_duration_ms", "tycoon_rebuild_duration_ms", unit: "ms");

    public static readonly DualHistogram MongoReadDurationMs = DualHist(
        "synaptix_mongo_read_duration_ms", "tycoon_mongo_read_duration_ms", unit: "ms");

    public static readonly DualHistogram ElasticBulkDurationMs = DualHist(
        "synaptix_elastic_bulk_duration_ms", "tycoon_elastic_bulk_duration_ms", unit: "ms");

    public static readonly DualCounter AdminOpsRequests = Dual(
        "synaptix.admin_ops.requests",
        "tycoon.admin_ops.requests",
        unit: "{request}",
        description: "Admin ops endpoint requests");

    public static readonly DualCounter RollupRebuildDocsIndexed = Dual(
        "synaptix.rollup_rebuild.docs_indexed",
        "tycoon.rollup_rebuild.docs_indexed",
        unit: "{doc}",
        description: "Rollup rebuild documents indexed into Elastic");

    public static readonly DualHistogram RollupRebuildBatchMs = DualHist(
        "synaptix.rollup_rebuild.batch_ms",
        "tycoon.rollup_rebuild.batch_ms",
        unit: "ms",
        description: "Rollup rebuild batch processing time (ms)");

    private static DualCounter Dual(
        string primary,
        string legacy,
        string? unit = null,
        string? description = null)
        => new(
            Meter.CreateCounter<long>(primary, unit, description),
            Meter.CreateCounter<long>(legacy, unit, description));

    private static DualHistogram DualHist(
        string primary,
        string legacy,
        string? unit = null,
        string? description = null)
        => new(
            Meter.CreateHistogram<double>(primary, unit, description),
            Meter.CreateHistogram<double>(legacy, unit, description));
}

/// <summary>Increments both primary (synaptix_*) and legacy (tycoon_*) counters.</summary>
public sealed class DualCounter
{
    private readonly Counter<long> _primary;
    private readonly Counter<long> _legacy;

    public DualCounter(Counter<long> primary, Counter<long> legacy)
    {
        _primary = primary;
        _legacy = legacy;
    }

    public void Add(long delta = 1)
    {
        _primary.Add(delta);
        _legacy.Add(delta);
    }

    public void Add(long delta, in TagList tags)
    {
        _primary.Add(delta, tags);
        _legacy.Add(delta, tags);
    }

    public void Add(long delta, KeyValuePair<string, object?> tag)
    {
        _primary.Add(delta, tag);
        _legacy.Add(delta, tag);
    }
}

/// <summary>Records on both primary and legacy histograms.</summary>
public sealed class DualHistogram
{
    private readonly Histogram<double> _primary;
    private readonly Histogram<double> _legacy;

    public DualHistogram(Histogram<double> primary, Histogram<double> legacy)
    {
        _primary = primary;
        _legacy = legacy;
    }

    public void Record(double value)
    {
        _primary.Record(value);
        _legacy.Record(value);
    }

    public void Record(double value, in TagList tags)
    {
        _primary.Record(value, tags);
        _legacy.Record(value, tags);
    }
}

/// <summary>Obsolete type name; use <see cref="SynaptixObservability"/>.</summary>
[Obsolete("Use SynaptixObservability. TycoonObservability remains as a compatibility alias.")]
public static class TycoonObservability
{
    public const string ServiceName = SynaptixObservability.ServiceName;
    public const string ActivitySourceName = SynaptixObservability.ActivitySourceName;
    public const string MeterName = SynaptixObservability.MeterName;
    public static ActivitySource ActivitySource => SynaptixObservability.ActivitySource;
    public static Meter Meter => SynaptixObservability.Meter;

    public static DualCounter RebuildRunsTotal => SynaptixObservability.RebuildRunsTotal;
    public static DualCounter RebuildDocsReadTotal => SynaptixObservability.RebuildDocsReadTotal;
    public static DualCounter RebuildDocsIndexedTotal => SynaptixObservability.RebuildDocsIndexedTotal;
    public static DualCounter RebuildDocsFailedTotal => SynaptixObservability.RebuildDocsFailedTotal;
    public static DualHistogram RebuildDurationMs => SynaptixObservability.RebuildDurationMs;
    public static DualHistogram MongoReadDurationMs => SynaptixObservability.MongoReadDurationMs;
    public static DualHistogram ElasticBulkDurationMs => SynaptixObservability.ElasticBulkDurationMs;
    public static DualCounter AdminOpsRequests => SynaptixObservability.AdminOpsRequests;
    public static DualCounter RollupRebuildDocsIndexed => SynaptixObservability.RollupRebuildDocsIndexed;
    public static DualHistogram RollupRebuildBatchMs => SynaptixObservability.RollupRebuildBatchMs;
}
