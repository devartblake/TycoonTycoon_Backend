using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Serilog;
using System.Globalization;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Infrastructure.Analytics.Elastic;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding;

namespace Tycoon.MigrationService;

public sealed class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _cfg;
    private readonly Serilog.ILogger _log;

    public MigrationWorker(IServiceProvider sp, IHostApplicationLifetime lifetime, IConfiguration cfg)
    {
        _sp = sp;
        _lifetime = lifetime;
        _cfg = cfg;
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
            var mode = (_cfg["MigrationService:Mode"] ?? "MigrateAndSeed").Trim();
            var resetDatabase = bool.TryParse(_cfg["MigrationService:ResetDatabase"], out var resetDb) && resetDb;
            var allowEnsureCreated = bool.TryParse(_cfg["MigrationService:AllowEnsureCreated"], out var allowCreated) && allowCreated;
            var autoRepairOnMissingTables = !bool.TryParse(_cfg["MigrationService:AutoRepairOnMissingTables"], out var autoRepair) || autoRepair;

            var rebuildEnabled = bool.TryParse(_cfg["MigrationService:RebuildElastic:Enabled"], out var enabled) && enabled;

            var modeRebuildOnly = mode.Equals("RebuildElastic", StringComparison.OrdinalIgnoreCase);
            var modeMigrateSeedAndRebuild = mode.Equals("MigrateSeedAndRebuildElastic", StringComparison.OrdinalIgnoreCase);

            var doRebuild = rebuildEnabled || modeRebuildOnly || modeMigrateSeedAndRebuild;

            DateOnly? fromUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:FromUtcDate"]);
            DateOnly? toUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:ToUtcDate"]);

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

                // (Optional) your model guard — log only, never block migrations
                try
                {
                    db.LogEntitiesMissingPrimaryKeysForMigrations(_log);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Model key guard threw unexpectedly; continuing.");
                }

                // ---- Migration-less deploy guard (robust, no extension methods)
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
                            "Create an initial migration in Tycoon.Backend.Migrations and ensure UseNpgsql(...).MigrationsAssembly(\"Tycoon.Backend.Migrations\") is set.");

                        throw new InvalidOperationException(
                            "No EF migrations found. Create and apply migrations before seeding.");
                    }

                    if (resetDatabase)
                    {
                        _log.Warning("ResetDatabase enabled. Deleting database before EnsureCreated.");
                        await db.Database.EnsureDeletedAsync(stoppingToken);
                    }

                    _log.Warning("No EF migrations found. Running EnsureCreated for dev-only bootstrap.");
                    await db.Database.EnsureCreatedAsync(stoppingToken);
                }
                else
                {
                    _log.Information("Detected {MigrationsCount} migrations in assembly {MigrationsAssembly}.",
                        migrationsCount, migrationsAssembly.Assembly.GetName().Name);

                    if (resetDatabase)
                    {
                        _log.Warning("ResetDatabase enabled. Deleting database before applying migrations.");
                        await db.Database.EnsureDeletedAsync(stoppingToken);
                    }

                    _log.Information("Applying EF migrations…");
                    await db.Database.MigrateAsync(stoppingToken);
                    _log.Information("EF migrations completed successfully");
                }

                await EnsureCriticalTablesReadyAsync(db, autoRepairOnMissingTables, stoppingToken);

                await VerifySeedPrerequisiteTablesAsync(db, stoppingToken);

                _log.Information("Seeding Tiers and Missions (idempotent)…");
                var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();
                await seeder.SeedAsync(db);
                _log.Information("Seeding completed successfully");

                _log.Information("Resetting Daily/Weekly mission claims (idempotent)…");
                var reset = scope.ServiceProvider.GetRequiredService<MissionResetService>();
                await reset.ResetAsync(db, stoppingToken);
                _log.Information("Mission claims reset completed successfully");
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

    private void EnsureMigrationsExistOrFail(AppDb db)
    {
        // Deprecated: replaced by inline handling with AllowEnsureCreate

        var migrationsAssembly = db.GetService<IMigrationsAssembly>();
        var migrationsCount = migrationsAssembly.Migrations.Count;

        if (migrationsCount == 0)
        {
            _log.Error(
                "No EF migrations were found in the configured migrations assembly. " +
                "Create an initial migration in Tycoon.Backend.Migrations and ensure UseNpgsql(...).MigrationsAssembly(\"Tycoon.Backend.Migrations\") is set.");

            throw new InvalidOperationException(
                "No EF migrations found. Create and apply migrations before seeding.");
        }

        _log.Information("Detected {MigrationsCount} migrations in assembly {MigrationsAssembly}.",
            migrationsCount, migrationsAssembly.Assembly.GetName().Name);
    }

    private async Task EnsureCriticalTablesReadyAsync(AppDb db, bool autoRepairOnMissingTables, CancellationToken ct)
    {
        var requiredTables = new[] { "Tiers", "Missions" };

        var missingTables = new List<string>();
        foreach (var table in requiredTables)
        {
            if (!await TableExistsAsync(db, table, ct))
                missingTables.Add(table);
        }

        if (missingTables.Count == 0)
            return;

        _log.Warning("Schema mismatch detected after migration. Missing tables: {MissingTables}", string.Join(", ", missingTables));

        if (!autoRepairOnMissingTables)
        {
            throw new InvalidOperationException(
                $"Missing critical tables after migration ({string.Join(", ", missingTables)}). " +
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

        if (stillMissing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Schema repair failed. Missing tables after repair: {string.Join(", ", stillMissing)}");
        }

        _log.Information("Schema repair completed; required tables now exist.");
    }

    private static async Task<bool> TableExistsAsync(AppDb db, string tableName, CancellationToken ct)
    {
        var conn = db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(ct);

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

    private async Task VerifySeedPrerequisiteTablesAsync(AppDb db, CancellationToken ct)
    {
        var requiredTables = new[] { "Tiers", "Missions", "MissionClaims" };

        await using var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
        {
            await conn.OpenAsync(ct);
        }

        var missing = new List<string>();

        foreach (var table in requiredTables)
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT to_regclass(@tableName) IS NOT NULL;";
            cmd.Parameters.AddWithValue("tableName", $"\"{table}\"");

            var exists = (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
            if (!exists)
            {
                missing.Add(table);
            }
        }

        if (missing.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Required tables are missing after migrations: " + string.Join(", ", missing) + ". " +
            "This usually means an empty initial migration was already recorded in __EFMigrationsHistory. " +
            "Reset the database (MigrationService:ResetDatabase=true) and rerun migrations, " +
            "or manually remove the bad migration row before rerunning.");
    }
}