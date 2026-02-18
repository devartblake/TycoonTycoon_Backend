namespace Tycoon.MigrationService.Options
{
    public sealed class MigrationServiceOptions
    {
        /// <summary>
        /// Controls what the migration service does on startup.
        /// Valid values: MigrateAndSeed | MigrateSeedAndRebuildElastic | RebuildElastic
        /// </summary>
        public string Mode { get; set; } = "MigrateAndSeed";

        /// <summary>
        /// When true, drops the database before applying migrations.
        /// DANGER: destroys all data. Only use in dev/CI.
        /// </summary>
        public bool ResetDatabase { get; set; } = false;

        /// <summary>
        /// When true, allows EnsureCreated fallback when no EF migrations exist.
        /// Dev-only safety net — prefer proper migrations in all environments.
        /// </summary>
        public bool AllowEnsureCreated { get; set; } = false;

        /// <summary>
        /// When true, drops and re-migrates the database if critical tables are
        /// missing after a migration run (schema drift recovery).
        /// </summary>
        public bool AutoRepairOnMissingTables { get; set; } = true;

        /// <summary>
        /// When true, suppresses the EF PendingModelChangesWarning.
        /// Useful during active development when the model is ahead of migrations.
        /// </summary>
        public bool SuppressPendingModelWarnings { get; set; } = false;

        public RebuildElasticOptions RebuildElastic { get; set; } = new();

        public sealed class RebuildElasticOptions
        {
            public bool Enabled { get; set; } = false;

            /// <summary>Optional ISO date (yyyy-MM-dd) to start the rebuild from.</summary>
            public DateOnly? FromUtcDate { get; set; }

            /// <summary>Optional ISO date (yyyy-MM-dd) to end the rebuild at.</summary>
            public DateOnly? ToUtcDate { get; set; }
        }
    }
}