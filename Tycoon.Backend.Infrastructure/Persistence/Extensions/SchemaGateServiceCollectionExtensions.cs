using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Infrastructure.Persistence.HealthChecks;
using Tycoon.Backend.Infrastructure.Persistence.Options;
using Tycoon.Backend.Infrastructure.Persistence.Startup;

namespace Tycoon.Backend.Infrastructure.Persistence.Extensions;

/// <summary>
/// Registers schema startup gating + readiness health check.
/// Call from API Program.cs (recommended).
/// </summary>
public static class SchemaGateServiceCollectionExtensions
{
    public static IServiceCollection AddSchemaGate(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var section = configuration.GetSection(SchemaGateOptions.SectionName);

        // Bind options and apply environment defaults ONLY when the section is missing.
        services.AddOptions<SchemaGateOptions>()
            .Bind(section)
            .PostConfigure(opt =>
            {
                var hasSection = section.Exists();

                if (!hasSection)
                {
                    // BUG FIX: Previously the gate was fully disabled for non-Production when
                    // no "SchemaGate:" config section was present. This caused the API to start
                    // immediately in Docker/dev before MigrationService had applied any migrations,
                    // resulting in 42P01 "relation does not exist" errors on the first request.
                    //
                    // New default policy: schema validation is ALWAYS enabled in every environment.
                    // Only the failure mode varies — Production hard-crashes, non-Production logs
                    // the error and allows the host to continue (so devs aren't blocked by a missing
                    // config section, but the gate still runs and warns loudly).
                    opt.Enabled = true;
                    opt.StartupGateEnabled = true;
                    opt.HealthCheckEnabled = true;
                    opt.FailStartupIfInvalid = environment.IsProduction();
                    opt.LogOnly = !environment.IsProduction();
                }

                if (opt.TimeoutSeconds <= 0) opt.TimeoutSeconds = 30;
                if (string.IsNullOrWhiteSpace(opt.Schema)) opt.Schema = "public";
                opt.RequiredTables ??= Array.Empty<string>();
                if (string.IsNullOrWhiteSpace(opt.MigrationsHistoryTable)) opt.MigrationsHistoryTable = "__EFMigrationsHistory";
            });

        // Core services
        services.TryAddScoped<SchemaStartupGate>();
        services.TryAddScoped<SchemaHealthCheck>();

        // Hosted wrapper to run the startup gate once.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SchemaStartupGateHostedService>());

        // Health check registration (returns Healthy if disabled via options).
        services.AddHealthChecks()
            .AddCheck<SchemaHealthCheck>(
                name: "schema",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "ready" });

        return services;
    }

    private sealed class SchemaStartupGateHostedService : IHostedService
    {
        private readonly IServiceProvider _root;
        private readonly IOptions<SchemaGateOptions> _options;

        public SchemaStartupGateHostedService(IServiceProvider root, IOptions<SchemaGateOptions> options)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var opt = _options.Value;
            if (!opt.Enabled || !opt.StartupGateEnabled)
                return;

            using var scope = _root.CreateScope();
            var gate = scope.ServiceProvider.GetRequiredService<SchemaStartupGate>();
            await gate.ValidateAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}