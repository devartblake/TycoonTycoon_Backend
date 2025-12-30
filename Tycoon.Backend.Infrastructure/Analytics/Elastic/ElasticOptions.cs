namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    public sealed class ElasticOptions
    {
        public string Url { get; set; } = string.Empty;
        // Read aliases (use these for queries)
        public string DailyReadAlias { get; set; } = "tycoon-qa-daily-rollups";
        public string PlayerDailyReadAlias { get; set; } = "tycoon-qa-player-daily-rollups";

        // Write aliases (use these for indexing)
        public string DailyWriteAlias { get; set; } = "tycoon-qa-daily-rollups-write";
        public string PlayerDailyWriteAlias { get; set; } = "tycoon-qa-player-daily-rollups-write";

        // First concrete indices (created once, then rollover)
        public string DailyInitialIndex { get; set; } = "tycoon-qa-daily-rollups-000001";
        public string PlayerDailyInitialIndex { get; set; } = "tycoon-qa-player-daily-rollups-000001";

    }
}
