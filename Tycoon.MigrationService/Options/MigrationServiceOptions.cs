namespace Tycoon.MigrationService.Options
{
    public sealed class MigrationServiceOptions
    {
        public string Mode { get; set; } = "MigrateAndSeed";

        public RebuildElasticOptions RebuildElastic { get; set; } = new();

        public sealed class RebuildElasticOptions
        {
            public bool Enabled { get; set; } = false;

            // Optional ISO date like "2025-12-01"
            public DateOnly? FromUtcDate { get; set; }
            public DateOnly? ToUtcDate { get; set; }
        }
    }
}
