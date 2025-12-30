using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tycoon.Shared.Observability
{
    public static class TycoonObservability
    {
        /// <summary>
        /// Shared observability primitives used across API + workers:
        /// - ActivitySource (traces)
        /// - Meter (metrics)
        /// - Standard instrument names
        /// </summary>
        public const string ServiceName = "tycoon-backend";

        // Tracing
        public const string ActivitySourceName = "Tycoon.Backend";
        public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
        
        // Metrics
        public const string MeterName = "Tycoon.Backend.Metrics";
        public static readonly Meter Meter = new(MeterName);

        // Counters
        public static readonly Counter<long> RebuildRunsTotal =
            Meter.CreateCounter<long>("tycoon_rebuild_runs_total");

        public static readonly Counter<long> RebuildDocsReadTotal =
            Meter.CreateCounter<long>("tycoon_rebuild_docs_read_total");

        public static readonly Counter<long> RebuildDocsIndexedTotal =
            Meter.CreateCounter<long>("tycoon_rebuild_docs_indexed_total");

        public static readonly Counter<long> RebuildDocsFailedTotal =
            Meter.CreateCounter<long>("tycoon_rebuild_docs_failed_total");

        // Histograms (ms)
        public static readonly Histogram<double> RebuildDurationMs =
            Meter.CreateHistogram<double>("tycoon_rebuild_duration_ms");

        public static readonly Histogram<double> MongoReadDurationMs =
            Meter.CreateHistogram<double>("tycoon_mongo_read_duration_ms");

        public static readonly Histogram<double> ElasticBulkDurationMs =
            Meter.CreateHistogram<double>("tycoon_elastic_bulk_duration_ms");

        // Example instruments you can use immediately (optional)
        public static readonly Counter<long> AdminOpsRequests =
            Meter.CreateCounter<long>("tycoon.admin_ops.requests", unit: "{request}", description: "Admin ops endpoint requests");

        public static readonly Counter<long> RollupRebuildDocsIndexed =
            Meter.CreateCounter<long>("tycoon.rollup_rebuild.docs_indexed", unit: "{doc}", description: "Rollup rebuild documents indexed into Elastic");

        public static readonly Histogram<double> RollupRebuildBatchMs =
            Meter.CreateHistogram<double>("tycoon.rollup_rebuild.batch_ms", unit: "ms", description: "Rollup rebuild batch processing time (ms)");
    }
}
