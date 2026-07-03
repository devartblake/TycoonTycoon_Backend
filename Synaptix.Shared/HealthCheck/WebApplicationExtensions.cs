using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Synaptix.Shared.HealthCheck
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MapCustomHealthChecks(this WebApplication app)
        {
            // Health checks enabled in all environments. Security is handled by:
            // - Reverse proxy (Traefik) enforces TLS and network policies
            // - Endpoints expose no sensitive data (status only)
            // - Can be disabled via DISABLE_HEALTH_CHECKS=true if needed
            var disableHealthChecks = !string.IsNullOrWhiteSpace(app.Configuration["DISABLE_HEALTH_CHECKS"]);

            if (!disableHealthChecks)
            {
                var healthChecks = app.MapGroup("");

                healthChecks.CacheOutput("HealthChecks").WithRequestTimeout("HealthChecks");

                // All health checks must pass for app to be considered ready to accept traffic after starting
                healthChecks.MapHealthChecks("/health");

                // Only health checks tagged with the "live" tag must pass for app to be considered alive
                healthChecks.MapHealthChecks("/alive", new() { Predicate = static r => r.Tags.Contains("live") });

                // Readiness checks (all checks tagged "ready")
                healthChecks.MapHealthChecks("/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

                // Export health metrics in `/health/metrics` endpoint for Prometheus scraping
                app.UseHealthChecksPrometheusExporter(
                    "/health/metrics",
                    options =>
                    {
                        options.ResultStatusCodes[HealthStatus.Unhealthy] = 200;
                    }
                );
            }

            return app;
        }
    }
}
