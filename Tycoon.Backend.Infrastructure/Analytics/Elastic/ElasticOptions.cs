namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    public sealed class ElasticOptions
    {
        public string Url { get; set; } = string.Empty;
        public string Username { get; set; } = "elastic";
        public string Password { get; set; } = string.Empty;
        // Read aliases (use these for queries)
        public string DailyReadAlias { get; set; } = "synaptix-daily-rollups";
        public string PlayerDailyReadAlias { get; set; } = "synaptix-player-daily-rollups";

        // Write aliases (use these for indexing)
        public string DailyWriteAlias { get; set; } = "synaptix-daily-rollups-write";
        public string PlayerDailyWriteAlias { get; set; } = "synaptix-player-daily-rollups-write";

        // First concrete indices (created once, then rollover)
        public string DailyInitialIndex { get; set; } = "synaptix-daily-rollups-000001";
        public string PlayerDailyInitialIndex { get; set; } = "synaptix-player-daily-rollups-000001";

    }
}
