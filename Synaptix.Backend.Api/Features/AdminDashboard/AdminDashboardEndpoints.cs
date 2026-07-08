using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synaptix.Backend.Api.Features.AdminDashboard
{
    // Operator overview (#418). Aggregates the registered ASP.NET health checks
    // into the dashboard's service list.
    //
    // Deferred by design: rich system metrics and per-service time-series history
    // live in Prometheus/Grafana and are NOT queryable from this API. So `metrics`
    // is a zeroed placeholder, and the dashboard's service-history views stay empty
    // (handled client-side) until a metrics integration is added. Building those
    // time-series routes requires a Prometheus query path and is out of scope here.
    public static class AdminDashboardEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/dashboard").WithTags("Admin/Dashboard");

            g.MapGet("/stats", async (HealthCheckService health, CancellationToken ct) =>
            {
                var report = await health.CheckHealthAsync(ct);
                var now = DateTimeOffset.UtcNow;

                var services = report.Entries.Select(e => new
                {
                    id = e.Key,
                    name = e.Key,
                    displayName = e.Key,
                    status = MapStatus(e.Value.Status),
                    uptime = 0,
                    responseTime = e.Value.Duration.TotalMilliseconds,
                    lastCheckedAt = now,
                    nextCheckAt = now,
                    description = e.Value.Description ?? string.Empty,
                    endpoint = (string?)null
                }).ToList();

                return Results.Ok(new
                {
                    services,
                    metrics = new
                    {
                        apiGatewayRequests = 0,
                        activeConnections = 0,
                        cpuUsage = 0,
                        memoryUsage = 0,
                        diskUsage = 0,
                        avgResponseTime = 0,
                        errorRate = 0
                    },
                    lastUpdatedAt = now,
                    checksPerformed = services.Count,
                    alertsActive = services.Count(s => s.status != "healthy")
                });
            });
        }

        private static string MapStatus(HealthStatus status) => status switch
        {
            HealthStatus.Healthy => "healthy",
            HealthStatus.Degraded => "degraded",
            _ => "offline"
        };
    }
}
