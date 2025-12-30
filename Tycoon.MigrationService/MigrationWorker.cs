using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding;

namespace Tycoon.MigrationService
{
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
        private readonly Serilog.ILogger _log;
        private readonly IConfiguration _cfg;

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

                // -----------------------------
                // 1) Optional Elastic admin bits
                // -----------------------------
                var admin = scope.ServiceProvider.GetService<Tycoon.Backend.Infrastructure.Analytics.Elastic.ElasticAdmin>();
                if (admin is not null)
                {
                    _log.Information("Ensuring Elasticsearch templates...");
                    await admin.EnsureTemplatesAsync(stoppingToken);
                }
                else
                {
                    _log.Information("ElasticAdmin not registered (Elastic not configured). Skipping template creation.");
                }

                var bootstrapper = scope.ServiceProvider.GetService<Tycoon.Backend.Infrastructure.Analytics.Elastic.ElasticIndexBootstrapper>();
                if (bootstrapper is not null)
                {
                    _log.Information("Ensuring Elasticsearch indices exist...");
                    await bootstrapper.EnsureCreatedAsync(stoppingToken);
                }
                else
                {
                    _log.Information("ElasticIndexBootstrapper not registered (Elastic not configured). Skipping index creation.");
                }

                // -----------------------------
                // 2) Read mode flags
                // -----------------------------
                var mode = (_cfg["MigrationService:Mode"] ?? "MigrateAndSeed").Trim();

                var rebuildEnabled =
                    bool.TryParse(_cfg["MigrationService:RebuildElastic:Enabled"], out var enabled) && enabled;

                // If mode explicitly requests rebuild, treat as enabled.
                var modeRebuildOnly = mode.Equals("RebuildElastic", StringComparison.OrdinalIgnoreCase);
                var modeMigrateSeedAndRebuild = mode.Equals("MigrateSeedAndRebuildElastic", StringComparison.OrdinalIgnoreCase);

                var doRebuild = rebuildEnabled || modeRebuildOnly || modeMigrateSeedAndRebuild;

                DateOnly? fromUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:FromUtcDate"]);
                DateOnly? toUtcDate = TryParseDateOnly(_cfg["MigrationService:RebuildElastic:ToUtcDate"]);

                // -----------------------------
                // 3) Migrate + Seed + Reset
                // -----------------------------
                if (!modeRebuildOnly)
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDb>();

                    _log.Information("Applying EF migrations…");
                    await db.Database.MigrateAsync(stoppingToken);

                    _log.Information("Seeding Tiers and Missions (idempotent)…");
                    var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();
                    await seeder.SeedAsync(db);

                    _log.Information("Resetting Daily/Weekly mission claims (idempotent)…");
                    var reset = scope.ServiceProvider.GetRequiredService<MissionResetService>();
                    await reset.ResetAsync(db, stoppingToken);
                }
                else
                {
                    _log.Information("Mode=RebuildElastic: skipping EF migrations + seeding + reset.");
                }

                // -----------------------------
                // 4) Optional rebuild Elastic from Mongo rollups
                // -----------------------------
                if (doRebuild)
                {
                    var rebuilder = scope.ServiceProvider.GetService<IRollupRebuilder>();
                    if (rebuilder is null)
                    {
                        _log.Warning("IRollupRebuilder not registered. Ensure Mongo + Elastic are configured and Step 7 DI is wired.");
                    }
                    else
                    {
                        _log.Information("Rebuilding Elastic rollups from Mongo… from={FromUtcDate} to={ToUtcDate}",
                            fromUtcDate?.ToString("yyyy-MM-dd"), toUtcDate?.ToString("yyyy-MM-dd"));

                        await rebuilder.RebuildElasticFromMongoAsync(fromUtcDate, toUtcDate, stoppingToken);

                        _log.Information("Elastic rollup rebuild completed.");
                    }
                }
                else
                {
                    _log.Information("Elastic rebuild disabled (MigrationService:RebuildElastic:Enabled=false and Mode does not request rebuild).");
                }

                _log.Information("MigrationService completed successfully.");
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

        private static DateOnly? TryParseDateOnly(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Accept ISO yyyy-MM-dd (recommended)
            if (DateOnly.TryParse(value, out var d))
                return d;

            return null;
        }
    }
}
