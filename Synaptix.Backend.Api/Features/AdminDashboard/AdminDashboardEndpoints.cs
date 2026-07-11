using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synaptix.Backend.Api.Features.AdminDashboard
{
    // Operator overview (#418). Aggregates the registered ASP.NET health checks
    // into the dashboard's service list, and serves recent per-service history
    // from the in-memory HealthHistoryStore sampler (resets on restart).
    //
    // Still deferred by design: rich system metrics (CPU/memory/error-rate) live
    // in Prometheus/Grafana and are NOT queryable from this API, so `metrics`
    // remains a zeroed placeholder.
    public static class AdminDashboardEndpoints
    {
        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/dashboard").WithTags("Admin/Dashboard");

            g.MapGet("/services/history", (HealthHistoryStore store, [FromQuery] int hours) =>
            {
                var window = TimeSpan.FromHours(Math.Clamp(hours <= 0 ? 24 : hours, 1, 24));
                var histories = store.ServiceIds
                    .Select(id => ToServiceHistory(id, store.Get(id, window)))
                    .ToList();
                return Results.Ok(histories);
            });

            g.MapGet("/services/{id}/history", (string id, HealthHistoryStore store, [FromQuery] int hours) =>
            {
                var window = TimeSpan.FromHours(Math.Clamp(hours <= 0 ? 24 : hours, 1, 24));
                return Results.Ok(ToServiceHistory(id, store.Get(id, window)));
            });

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

        // Matches the dashboard's ServiceHistory shape: value is the check's
        // response time in ms (0 when the check reported unhealthy).
        private static object ToServiceHistory(string serviceId, IReadOnlyList<HealthSample> samples) => new
        {
            serviceId,
            metrics = samples.Select(s => new
            {
                timestamp = s.Timestamp,
                value = s.Status == HealthStatus.Unhealthy ? 0d : s.ResponseTimeMs
            }).ToList()
        };
    }
}
