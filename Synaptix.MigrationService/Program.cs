using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Synaptix.Backend.Infrastructure;
using Synaptix.MigrationService;
using Synaptix.MigrationService.Options;
using Synaptix.MigrationService.Seeding;
using Synaptix.Shared.Observability;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    await Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration);
            cfg.WriteTo.Console();
        })
        .ConfigureServices((ctx, services) =>
        {
            // Observability (tracing + metrics + Serilog integration)
            // IMPORTANT: This is the correct place because THIS is the host you actually run.
            services.AddObservability(ctx.Configuration, serviceName: "Synaptix.MigrationService");

            // Infrastructure (EF Core, Mongo, Elastic, Redis, clock, dispatcher, etc.)
            services.AddSingleton<Mediator.IPublisher, NoOpDomainEventPublisher>();
            services.AddInfrastructure(ctx.Configuration);

            // Bind MigrationServiceOptions from config section "MigrationService"
            services.AddOptions<MigrationServiceOptions>()
                .BindConfiguration("MigrationService");
            services.AddOptions<MinioSeedOptions>()
                .BindConfiguration("MinIO:Seeds");

            // Seeder + reset services
            services.AddTransient<AppSeeder>();
            services.AddTransient<MissionResetService>();
            services.AddTransient<MinioSeeder>();
            services.AddTransient<DashboardReadinessValidator>();

            // Worker
            services.AddHostedService<MigrationWorker>();
        })
        .RunConsoleAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MigrationService terminated unexpectedly.");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
