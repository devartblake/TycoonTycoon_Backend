using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Serilog;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Infrastructure.Analytics.Elastic;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding;

namespace Tycoon.MigrationService;

/// <summary>
/// One-shot worker:
/// - Optionally ensures Elastic templates + indices (if Elastic is configured)
/// - Applies EF migrations
/// - Seeds tiers/missions (idempotent)
/// - Resets mission claims (idempotent)
/// - Optionally rebuilds Elastic rollups from Mongo (idempotent)
///
/// Mode flags:
///   MigrationService:Mode = MigrateAndSeed | RebuildElastic | MigrateSeedAndRebuildElastic
///   MigrationService:RebuildElastic:Enabled = true|false
///   MigrationService:RebuildElastic:FromUtcDate = yyyy-MM-dd (optional)
///   MigrationService:RebuildElastic:ToUtcDate   = yyyy-MM-dd (optional)
/// </summary>
public sealed class MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _cfg;
    private readonly Serilog.ILogger _log;

    public MigrationWorker(
        IServiceProvider sp,
        IHostApplicationLifetime lifetime,
        IConfiguration cfg)
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

            var rebuildEnabled =
                bool.TryParse(_cfg["MigrationService:RebuildElastic:Enabled"], out var enabled) && enabled;

            var modeRebuildOnly = mode.Equals("RebuildElastic", StringComparison.OrdinalIgnoreCase);
            var modeMigrateSeedAndRebuild = mode.Equals("MigrateSeedAndRebuildElastic", StringComparison.OrdinalIgnoreCase);

            var doRebuild = rebuildEnabled || modeRebuildOnly || modeMigrateSeedAndRebuild;

            DateOnly? fromUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:FromUtcDate"]);
            DateOnly? toUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:ToUtcDate"]);

            // ---------------------------------------------
            // 1) Optional Elastic bootstrap (never blocks DB)
            // ---------------------------------------------
            // If you have a dedicated switch, prefer:
            //   Elastic:Enabled = true|false
            // Fallback: treat "registered in DI" as enabled.
            var elasticEnabled =
                !bool.TryParse(_cfg["Elastic:Enabled"], out var eEnabled) || eEnabled;

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

                // ---- Model guard: log offenders ONLY for migrations
                // (Do not throw; just log. Keeps behavior dev-friendly.)
                try
                {
                    db.LogEntitiesMissingPrimaryKeysForMigrations(_log);
                }
                catch (Exception ex)
                {
                    // Guard must never break migrations. If it breaks, log and continue.
                    _log.Warning(ex, "Model key guard threw unexpectedly; continuing.");
                }

                // ---- Detect "no migrations" early.
                // If there are zero migrations, EF will create __EFMigrationsHistory and do nothing,
                // then seeding will fail because your tables do not exist (e.g., \"Tiers\").
                var migrations = (await db.Database.GetMigrationsAsync(stoppingToken)).ToList();
                if (migrations.Count == 0)
                {
                    _log.Error(
                        "No migrations were found. Add an initial migration for the Infrastructure DbContext " +
                        "and ensure the migration assembly is correct. Seeding cannot run without schema.");
                    throw new InvalidOperationException(
                        "No EF migrations found. Create and apply migrations before seeding.");
                }

                _log.Information("Applying EF migrations…");
                await db.Database.MigrateAsync(stoppingToken);
                _log.Information("EF migrations completed successfully");

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
                    _log.Warning(
                        "IRollupRebuilder not registered. Ensure Mongo + Elastic are configured and DI is wired.");
                }
                else
                {
                    try
                    {
                        _log.Information(
                            "Rebuilding Elastic rollups from Mongo… from={FromUtcDate} to={ToUtcDate}",
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
                _log.Information(
                    "Elastic rebuild disabled (MigrationService:RebuildElastic:Enabled=false and Mode does not request rebuild).");
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

    private async Task TryEnsureElasticTemplatesAsync(IServiceScope scope, CancellationToken ct)
    {
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
            // Log + continue (Elastic is optional)
            _log.Warning(ex, "Failed to create Elasticsearch templates. Continuing with migrations anyway…");

            // Your log suggests a client/server major mismatch (client 9 vs server 8).
            _log.Warning("To fix Elastic template failures, either:");
            _log.Warning("  1) Align client major to server major (recommended: use Elastic.Clients.Elasticsearch 8.x with ES 8.x)");
            _log.Warning("  2) Upgrade the Elasticsearch server to match the client major");
            _log.Warning("  3) Disable Elasticsearch in configuration (Elastic:Enabled=false)");
        }
    }

    private async Task TryEnsureElasticIndicesAsync(IServiceScope scope, CancellationToken ct)
    {
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
            // Log + continue (Elastic is optional)
            _log.Warning(ex, "Failed to create Elasticsearch indices. Continuing with migrations anyway…");
        }
    }

    private static DateOnly? TryParseDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Accept ISO yyyy-MM-dd (recommended)
        if (DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;

        if (DateOnly.TryParse(value.Trim(), out var fallback))
            return fallback;

        return null;
    }
}
