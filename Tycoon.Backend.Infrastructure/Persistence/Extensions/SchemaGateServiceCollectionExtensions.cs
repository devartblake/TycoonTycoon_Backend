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
                    // Default policy:
                    // - Production: enabled
                    // - Non-production: disabled
                    opt.Enabled = environment.IsProduction();
                    opt.StartupGateEnabled = opt.Enabled;
                    opt.HealthCheckEnabled = opt.Enabled;
                }

                if (opt.TimeoutSeconds <= 0) opt.TimeoutSeconds = 10;
                if (string.IsNullOrWhiteSpace(opt.Schema)) opt.Schema = "public";
                opt.CriticalTables ??= Array.Empty<string>();
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
