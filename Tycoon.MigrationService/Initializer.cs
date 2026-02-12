using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using System.Diagnostics;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding;

namespace Tycoon.MigrationService
{
    /// <summary>
    /// Runs EF migrations and idempotent seeding, then stops the host.
    /// This is the only "entry behavior" for the MigrationService.
    /// </summary>
    public sealed class Initializer : BackgroundService
    {
        public const string ActivitySourceName = "Tycoon.Migrations";
        private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _lifetime;
        private readonly ILogger<Initializer> _logger;

        public Initializer(
            IServiceProvider serviceProvider,
            IHostApplicationLifetime lifetime,
            ILogger<Initializer> logger)
        {
            _serviceProvider = serviceProvider;
            _lifetime = lifetime;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var activity = ActivitySource.StartActivity(
                "MigrationService.Run",
                ActivityKind.Client);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var services = scope.ServiceProvider;

                _logger.LogInformation("Starting MigrationService…");

                var db = services.GetRequiredService<AppDb>();

                _logger.LogInformation("Applying EF migrations…");
                await db.Database.MigrateAsync(stoppingToken);

                var seeder = services.GetRequiredService<AppSeeder>();

                _logger.LogInformation("Seeding Tiers and Missions (idempotent)…");
                await seeder.SeedAsync(db, stoppingToken);

                _logger.LogInformation("MigrationService completed successfully.");
            }
            catch (Exception ex)
            {
                activity?.RecordException(ex);
                _logger.LogError(ex, "MigrationService failed.");
                Environment.ExitCode = 1;
            }
            finally
            {
                // Exit the service cleanly (Aspire will see it as completed)
                _lifetime.StopApplication();
            }
        }
    }
}
