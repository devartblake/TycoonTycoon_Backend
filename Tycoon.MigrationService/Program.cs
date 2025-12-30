using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Tycoon.Backend.Infrastructure;
using Tycoon.MigrationService;
using Tycoon.MigrationService.Seeding;
using Tycoon.Shared.Observability;

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
            // Step 9: Observability (tracing + metrics + Serilog integration)
            // IMPORTANT: This is the correct place because THIS is the host you actually run.
            services.AddObservability(ctx.Configuration, serviceName: "Tycoon.MigrationService");

            // Infrastructure (EF Core, Mongo, Elastic, Redis, clock, dispatcher, etc.)
            services.AddInfrastructure(ctx.Configuration);

            // Seeder + reset services
            services.AddTransient<AppSeeder>();
            services.AddTransient<MissionResetService>();

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
