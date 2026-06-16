using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Synaptix.MigrationService.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using System.Globalization;
using Synaptix.Backend.Application.Analytics.Abstractions;
using Synaptix.Backend.Infrastructure.Analytics.Elastic;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.MigrationService.Seeding;

namespace Synaptix.MigrationService;

public sealed class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _cfg;
    private readonly Serilog.ILogger _log;

    private readonly MigrationServiceOptions _options;

    public MigrationWorker(
        IServiceProvider sp,
        IHostApplicationLifetime lifetime,
        IConfiguration cfg,
        IOptions<MigrationServiceOptions> options)
    {
        _sp = sp;
        _lifetime = lifetime;
        _cfg = cfg;
        _options = options.Value;
        _log = Log.ForContext<MigrationWorker>();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _sp.CreateScope();

            // ---------------------------------------------
            // 0) Read mode flags
            // ---------------------------------------------
            var mode = _options.Mode.Trim();
            var resetDatabase = _options.ResetDatabase;
            var allowEnsureCreated = _options.AllowEnsureCreated;
            var autoRepairOnMissingTables = _options.AutoRepairOnMissingTables;

            var rebuildEnabled = _options.RebuildElastic.Enabled;

            var modeRebuildOnly = mode.Equals("RebuildElastic", StringComparison.OrdinalIgnoreCase);
            var modeMigrateSeedAndRebuild = mode.Equals("MigrateSeedAndRebuildElastic", StringComparison.OrdinalIgnoreCase);

            var doRebuild = rebuildEnabled || modeRebuildOnly || modeMigrateSeedAndRebuild;

            DateOnly? fromUtcDate = _options.RebuildElastic.FromUtcDate;
            DateOnly? toUtcDate = _options.RebuildElastic.ToUtcDate;

            // ---------------------------------------------
            // 1) Optional Elastic bootstrap (never blocks DB)
            // ---------------------------------------------
            var elasticEnabled = !bool.TryParse(_cfg["Elastic:Enabled"], out var eEnabled) || eEnabled;

            if (elasticEnabled)
            {
                await TryEnsureElasticTemplatesAsync(scope, stoppingToken);
                await TryEnsureElasticIndicesAsync(scope, stoppingToken);
            }
            else
            {
                _log.Information("Elastic disabled via config (Elastic:Enabled=false). Skipping Elastic bootstrap.");
            }

            // ---------------------------------------------
            // 2) Migrate + Seed + Reset
            // ---------------------------------------------
            if (!modeRebuildOnly)
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDb>();

                // Model guard: log only, never block
                try
                {
                    db.LogEntitiesMissingPrimaryKeysForMigrations(_log);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Model key guard threw unexpectedly; continuing.");
                }

                var migrationsAssembly = db.GetService<IMigrationsAssembly>();
                var migrationsCount = migrationsAssembly.Migrations.Count;

                if (resetDatabase)
                {
                    _log.Warning("ResetDatabase enabled. Deleting database before applying migrations.");
                    await db.Database.EnsureDeletedAsync(stoppingToken);
                }

                if (migrationsCount == 0)
                {
                    if (!allowEnsureCreated)
                    {
                        _log.Error(
                            "No EF migrations were found in the configured migrations assembly. " +
                            "Create an initial migration in Synaptix.Backend.Migrations and ensure UseNpgsql(...).MigrationsAssembly(\"Synaptix.Backend.Migrations\") is set.");

                        throw new InvalidOperationException("No EF migrations found. Create and apply migrations before seeding.");
                    }

                    _log.Warning("No EF migrations found. Running EnsureCreated for dev-only bootstrap.");
                    await db.Database.EnsureCreatedAsync(stoppingToken);
                }
                else
                {
                    _log.Information("Detected {MigrationsCount} migrations in assembly {MigrationsAssembly}.",
                        migrationsCount, migrationsAssembly.Assembly.GetName().Name);

                    var recoveredFromSchemaDrift = await TryBaselineOrRepairFromPreExistingSchemaAsync(
                        db,
                        autoRepairOnMissingTables,
                        stoppingToken);

                    if (!recoveredFromSchemaDrift)
                    {
                        const long AdvisoryLockKey = 987654321L;
                        await AcquireAdvisoryLockAsync(db, AdvisoryLockKey, _log, stoppingToken);
                        try
                        {
                            _log.Information("Applying EF migrations…");
                            await db.Database.MigrateAsync(stoppingToken);
                            _log.Information("EF migrations completed successfully");
                        }
                        finally
                        {
                            await ReleaseAdvisoryLockAsync(db, AdvisoryLockKey, _log);
                        }
                    }
                }

                await EnsureCriticalTablesReadyAsync(db, autoRepairOnMissingTables, stoppingToken);

                // ✅ FIX: pass logger + ct (your CS7036)
                await VerifySeedPrerequisiteTablesAsync(db, _log, stoppingToken);

                _log.Information("Seeding Tiers and Missions (idempotent)…");
                var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();
                await seeder.SeedAsync(db, stoppingToken);
                _log.Information("Seeding completed successfully");

                _log.Information("Seeding catalog data from MinIO (idempotent)…");
                try
                {
                    var minioSeeder = scope.ServiceProvider.GetRequiredService<MinioSeeder>();
                    await minioSeeder.SeedAsync(db, stoppingToken);
                    _log.Information("MinIO catalog seeding completed");
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "MinIO catalog seeding failed (non-fatal). " +
                        "Ensure MinIO is running and seed files exist at seeds/*.json.");
                }

                // Seeders may leave catalog/question entities tracked; isolate mission reset
                // so its SaveChanges only flushes mission-claim state.
                db.ChangeTracker.Clear();

                _log.Information("Resetting Daily/Weekly mission claims (idempotent)…");
                var reset = scope.ServiceProvider.GetRequiredService<MissionResetService>();
                await reset.ResetAsync(db, stoppingToken);
                _log.Information("Mission claims reset completed successfully");

                _log.Information("Validating Django Operator Dashboard readiness…");
                var readiness = scope.ServiceProvider.GetRequiredService<DashboardReadinessValidator>();
                await readiness.ValidateAsync(db, stoppingToken);
            }
            else
            {
                _log.Information("Mode=RebuildElastic: skipping EF migrations + seeding + reset.");
            }

            // ---------------------------------------------
            // 3) Optional rebuild Elastic from Mongo rollups
            // ---------------------------------------------
            if (doRebuild)
            {
                var rebuilder = scope.ServiceProvider.GetService<IRollupRebuilder>();
                if (rebuilder is null)
                {
                    _log.Warning("IRollupRebuilder not registered. Ensure Mongo + Elastic are configured and DI is wired.");
                }
                else
                {
                    try
                    {
                        _log.Information("Rebuilding Elastic rollups from Mongo… from={FromUtcDate} to={ToUtcDate}",
                            fromUtcDate?.ToString("yyyy-MM-dd"), toUtcDate?.ToString("yyyy-MM-dd"));

                        await rebuilder.RebuildElasticFromMongoAsync(fromUtcDate, toUtcDate, stoppingToken);
                        _log.Information("Elastic rollup rebuild completed.");
                    }
                    catch (Exception ex)
                    {
                        _log.Warning(ex, "Failed to rebuild Elastic rollups. This is not critical.");
                    }
                }
            }
            else
            {
                _log.Information("Elastic rebuild disabled (MigrationService:RebuildElastic:Enabled=false and Mode does not request rebuild).");
            }

            _log.Information("✅ MigrationService completed successfully.");
        }
        catch (Exception ex)
        {
            _log.Error(ex, "MigrationService failed.");
            Environment.ExitCode = 1;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    private async Task<bool> TryBaselineOrRepairFromPreExistingSchemaAsync(
        AppDb db,
        bool autoRepairOnMissingTables,
        CancellationToken ct)
    {
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync(ct);
        if (appliedMigrations.Any())
            return false;

        // Sentinel check: if key legacy/app tables exist while EF history is empty,
        // initial migration is likely to fail with "relation already exists".
        //
        // Table names must match what exists in the database after migrations.
        // After the NormalizeSnakeCaseNaming migration, all tables use snake_case.
        var sentinelTables = new[] { "anti_cheat_flags", "users", "matches", "missions", "tiers" };
        var existingSentinels = new List<string>();

        foreach (var table in sentinelTables)
        {
            if (await TableExistsAsync(db, table, ct))
                existingSentinels.Add(table);
        }

        if (existingSentinels.Count == 0)
            return false;

        _log.Warning(
            "Detected pre-existing tables ({Tables}) while __EFMigrationsHistory is empty. " +
            "This indicates schema/history drift and can cause initial migration conflicts.",
            string.Join(", ", existingSentinels));

        var baselineTables = new[] { "anti_cheat_flags", "users", "matches", "missions", "tiers" };
        var canBaseline = true;
        foreach (var table in baselineTables)
        {
            if (!await TableExistsAsync(db, table, ct))
            {
                canBaseline = false;
                break;
            }
        }

        if (canBaseline)
        {
            var migrationsAssembly = db.GetService<IMigrationsAssembly>();
            var lastMigration = migrationsAssembly.Migrations.Keys
                .OrderBy(id => id, StringComparer.Ordinal)
                .LastOrDefault();

            if (!string.IsNullOrWhiteSpace(lastMigration))
            {
                _log.Warning(
                    "Baselining EF migration history at '{MigrationId}' because schema already exists.",
                    lastMigration);

                await EnsureHistoryTableExistsAsync(db, ct);
                await InsertMigrationHistoryRowIfMissingAsync(db, lastMigration!, ct);

                _log.Information("Baseline complete; skipping schema create migration for existing database.");
                return true;
            }
        }

        if (!autoRepairOnMissingTables)
        {
            throw new InvalidOperationException(
                "Database has existing tables but no EF migration history and cannot be safely baselined. " +
                "Enable MigrationService:AutoRepairOnMissingTables=true to auto-repair in dev/CI, " +
                "or reset the DB volume, then rerun MigrationService.");
        }

        _log.Warning("AutoRepairOnMissingTables=true. Rebuilding schema before applying migrations (EnsureDeleted + Migrate).");
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.MigrateAsync(ct);
        _log.Information("Schema drift recovered; EF migrations applied on a clean database.");
        return true;
    }

    private static async Task EnsureHistoryTableExistsAsync(AppDb db, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                openedHere = true;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" (" +
                "\"MigrationId\" character varying(150) NOT NULL," +
                "\"ProductVersion\" character varying(32) NOT NULL," +
                "CONSTRAINT \"PK___EFMigrationsHistory\" PRIMARY KEY (\"MigrationId\")" +
                ");";

            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync();
        }
    }

    private static async Task InsertMigrationHistoryRowIfMissingAsync(AppDb db, string migrationId, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                openedHere = true;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                "VALUES (@migrationId, @productVersion) ON CONFLICT (\"MigrationId\") DO NOTHING;";

            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@migrationId";
            p1.Value = migrationId;
            cmd.Parameters.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@productVersion";
            p2.Value = "9.0.11";
            cmd.Parameters.Add(p2);

            await cmd.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync();
        }
    }

    private async Task EnsureCriticalTablesReadyAsync(AppDb db, bool autoRepairOnMissingTables, CancellationToken ct)
    {
        var requiredTables = new[] { "tiers", "missions", "questions", "store_items", "skill_nodes", "season_reward_rules", "admin_email_acls" };

        var missingTables = new List<string>();
        foreach (var table in requiredTables)
        {
            if (!await TableExistsAsync(db, table, ct))
                missingTables.Add(table);
        }

        if (!missingTables.Contains("questions"))
            await RepairQuestionTaxonomyColumnsAsync(db, ct);

        var missingColumns = new List<string>();
        var requiredColumns = new (string Table, string Column)[]
        {
            ("questions", "status"),
            ("questions", "status_changed_at_utc"),
            ("questions", "canonical_category"),
            ("questions", "display_category"),
            ("questions", "subject"),
            ("questions", "topic"),
            ("questions", "subtopic"),
            ("questions", "grade_band"),
            ("questions", "age_group"),
            ("questions", "audience"),
            ("questions", "source_dataset"),
            ("questions", "source_question_id"),
            ("questions", "question_type"),
            ("questions", "media_type"),
            ("questions", "taxonomy_tags_json"),
            ("store_items", "sku"),
            ("skill_nodes", "key"),
            ("season_reward_rules", "max_tier_rank"),
            ("admin_email_acls", "normalized_email")
        };

        foreach (var (table, column) in requiredColumns)
        {
            if (missingTables.Contains(table))
                continue;

            if (!await ColumnExistsAsync(db, table, column, ct))
                missingColumns.Add($"{table}.{column}");
        }

        if (missingTables.Count == 0 && missingColumns.Count == 0)
            return;

        _log.Warning(
            "Schema mismatch detected after migration. Missing tables: {MissingTables}. Missing columns: {MissingColumns}",
            string.Join(", ", missingTables),
            string.Join(", ", missingColumns));

        if (!autoRepairOnMissingTables)
        {
            throw new InvalidOperationException(
                $"Schema mismatch after migration. Missing tables: {string.Join(", ", missingTables)}. " +
                $"Missing columns: {string.Join(", ", missingColumns)}. " +
                "Enable MigrationService:AutoRepairOnMissingTables=true or reset the DB volume and rerun migrations.");
        }

        _log.Warning("AutoRepairOnMissingTables=true. Rebuilding schema by EnsureDeleted + Migrate.");
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.MigrateAsync(ct);

        var stillMissing = new List<string>();
        foreach (var table in requiredTables)
        {
            if (!await TableExistsAsync(db, table, ct))
                stillMissing.Add(table);
        }

        foreach (var (table, column) in requiredColumns)
        {
            if (!await ColumnExistsAsync(db, table, column, ct))
                stillMissing.Add($"{table}.{column}");
        }

        if (stillMissing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Schema repair failed. Missing tables after repair: {string.Join(", ", stillMissing)}");
        }

        _log.Information("Schema repair completed; required tables now exist.");
    }

    private async Task RepairQuestionTaxonomyColumnsAsync(AppDb db, CancellationToken ct)
    {
        if (await ColumnExistsAsync(db, "questions", "age_group", ct))
            return;

        _log.Warning(
            "Detected questions table without taxonomy columns. Applying additive repair for AddQuestionTaxonomy drift.");

        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS canonical_category character varying(128) NOT NULL DEFAULT '';
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS display_category character varying(128) NOT NULL DEFAULT '';
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS subject character varying(128);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS topic character varying(128);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS subtopic character varying(128);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS grade_band character varying(64);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS age_group character varying(64);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS audience character varying(64);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS source_dataset character varying(256);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS source_question_id character varying(128);
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS question_type character varying(64) NOT NULL DEFAULT 'multiple_choice';
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS media_type character varying(64) NOT NULL DEFAULT 'text';
            ALTER TABLE questions ADD COLUMN IF NOT EXISTS taxonomy_tags_json jsonb NOT NULL DEFAULT '[]';

            UPDATE questions
            SET canonical_category = lower(regexp_replace(regexp_replace(category, '[^a-zA-Z0-9]+', '_', 'g'), '^_|_$', '', 'g'))
            WHERE canonical_category = '';

            UPDATE questions
            SET display_category = category
            WHERE display_category = '';

            UPDATE questions
            SET media_type = CASE WHEN media_key IS NULL OR btrim(media_key) = '' THEN 'text' ELSE 'image' END
            WHERE media_type = 'text';

            CREATE INDEX IF NOT EXISTS ix_questions_canonical_category ON questions (canonical_category);
            CREATE INDEX IF NOT EXISTS ix_questions_subject ON questions (subject);
            CREATE INDEX IF NOT EXISTS ix_questions_grade_band ON questions (grade_band);
            CREATE INDEX IF NOT EXISTS ix_questions_age_group ON questions (age_group);
            CREATE INDEX IF NOT EXISTS ix_questions_source_dataset ON questions (source_dataset);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_questions_source_dataset_source_question_id
                ON questions (source_dataset, source_question_id)
                WHERE source_dataset IS NOT NULL AND source_question_id IS NOT NULL;
            CREATE INDEX IF NOT EXISTS ix_questions_status_canonical_category_difficulty
                ON questions (status, canonical_category, difficulty);
            """,
            ct);

        if (await TableExistsAsync(db, "learning_modules", ct))
        {
            await db.Database.ExecuteSqlRawAsync(
                """
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS canonical_category character varying(128) NOT NULL DEFAULT '';
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS subject character varying(128);
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS topic character varying(128);
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS grade_band character varying(64);
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS age_group character varying(64);
                ALTER TABLE learning_modules ADD COLUMN IF NOT EXISTS audience character varying(64);

                UPDATE learning_modules
                SET canonical_category = lower(regexp_replace(regexp_replace(category, '[^a-zA-Z0-9]+', '_', 'g'), '^_|_$', '', 'g'))
                WHERE canonical_category = '';

                CREATE INDEX IF NOT EXISTS ix_learning_modules_canonical_category ON learning_modules (canonical_category);
                CREATE INDEX IF NOT EXISTS ix_learning_modules_subject ON learning_modules (subject);
                CREATE INDEX IF NOT EXISTS ix_learning_modules_grade_band ON learning_modules (grade_band);
                CREATE INDEX IF NOT EXISTS ix_learning_modules_age_group ON learning_modules (age_group);
                """,
                ct);
        }

        _log.Information("Question taxonomy additive schema repair completed.");
    }

    private static async Task<bool> TableExistsAsync(AppDb db, string tableName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                openedHere = true;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT EXISTS (" +
                "SELECT 1 FROM information_schema.tables " +
                "WHERE table_schema = current_schema() AND table_name = @tableName" +
                ")";

            var p = cmd.CreateParameter();
            p.ParameterName = "@tableName";
            p.Value = tableName;
            cmd.Parameters.Add(p);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result is bool b && b;
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync();
        }
    }

    private static async Task<bool> ColumnExistsAsync(AppDb db, string tableName, string columnName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                openedHere = true;
            }

            await using var cmd = conn.CreateCommand();
            cmd.CommandText =
                "SELECT EXISTS (" +
                "SELECT 1 FROM information_schema.columns " +
                "WHERE table_schema = current_schema() AND table_name = @tableName AND column_name = @columnName" +
                ")";

            var table = cmd.CreateParameter();
            table.ParameterName = "@tableName";
            table.Value = tableName;
            cmd.Parameters.Add(table);

            var column = cmd.CreateParameter();
            column.ParameterName = "@columnName";
            column.Value = columnName;
            cmd.Parameters.Add(column);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result is bool b && b;
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync();
        }
    }

    private async Task TryEnsureElasticTemplatesAsync(IServiceScope scope, CancellationToken ct)
    {
        // Probe first: if the client isn't registered, skip Elastic entirely.
        var client = scope.ServiceProvider.GetService<Elastic.Clients.Elasticsearch.ElasticsearchClient>();
        if (client is null)
        {
            _log.Information("ElasticsearchClient not registered. Skipping Elastic template creation.");
            return;
        }

        var admin = scope.ServiceProvider.GetService<ElasticAdmin>();
        if (admin is null)
        {
            _log.Information("ElasticAdmin not registered. Skipping template creation.");
            return;
        }

        try
        {
            _log.Information("Ensuring Elasticsearch templates…");
            await admin.EnsureTemplatesAsync(ct);
            _log.Information("Elasticsearch templates ensured.");
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to create Elasticsearch templates. Continuing with migrations anyway…");
            _log.Warning("To fix Elastic template failures, align client/server majors (recommended: Elastic.Clients.Elasticsearch 8.x with ES 8.x), upgrade ES, or disable Elastic.");
        }
    }

    private async Task TryEnsureElasticIndicesAsync(IServiceScope scope, CancellationToken ct)
    {
        var client = scope.ServiceProvider.GetService<Elastic.Clients.Elasticsearch.ElasticsearchClient>();
        if (client is null)
        {
            _log.Information("ElasticsearchClient not registered. Skipping Elastic index bootstrap.");
            return;
        }

        var bootstrapper = scope.ServiceProvider.GetService<ElasticIndexBootstrapper>();
        if (bootstrapper is null)
        {
            _log.Information("ElasticIndexBootstrapper not registered. Skipping index creation.");
            return;
        }

        try
        {
            _log.Information("Ensuring Elasticsearch indices exist…");
            await bootstrapper.EnsureCreatedAsync(ct);
            _log.Information("Elasticsearch indices ensured.");
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to create Elasticsearch indices. Continuing with migrations anyway…");
        }
    }

    private static DateOnly? TryParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var v = value.Trim();

        if (DateOnly.TryParseExact(v, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;

        if (DateOnly.TryParse(v, out var fallback))
            return fallback;

        return null;
    }

    private static async Task AcquireAdvisoryLockAsync(AppDb db, long lockKey, Serilog.ILogger log, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"SELECT pg_advisory_lock({lockKey})";
        await cmd.ExecuteNonQueryAsync(ct);
        log.Information("Acquired PostgreSQL advisory lock {LockKey}.", lockKey);
    }

    private static async Task ReleaseAdvisoryLockAsync(AppDb db, long lockKey, Serilog.ILogger log)
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) return;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT pg_advisory_unlock({lockKey})";
            await cmd.ExecuteNonQueryAsync();
            log.Information("Released PostgreSQL advisory lock {LockKey}.", lockKey);
        }
        catch (Exception ex)
        {
            log.Warning(ex, "Failed to release advisory lock {LockKey} — connection may have already closed.", lockKey);
        }
    }

    private async Task VerifySeedPrerequisiteTablesAsync(AppDb db, Serilog.ILogger log, CancellationToken ct)
    {
        var requiredTables = new[]
        {
        "tiers",
        "missions",
        };

        var conn = db.Database.GetDbConnection();
        var openedHere = false;

        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
            {
                await conn.OpenAsync(ct);
                openedHere = true;
            }

            foreach (var table in requiredTables)
            {
                if (!await TableExistsAsync(db, table, ct))
                {
                    log.Error("Schema check failed: required table {Table} does not exist.", table);
                    throw new InvalidOperationException($"Schema not present. Required table '{table}' does not exist.");
                }
            }
        }
        finally
        {
            if (openedHere)
                await conn.CloseAsync();
        }
    }
}
