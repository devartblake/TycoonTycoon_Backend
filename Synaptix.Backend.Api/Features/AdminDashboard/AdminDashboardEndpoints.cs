using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Synaptix.Backend.Api.Features.AdminDashboard
{
    // Operator overview (#418). Aggregates the registered ASP.NET health checks
    // into the dashboard's service list, serves recent per-service history from
    // the in-memory HealthHistoryStore sampler, and — when Dashboard:PrometheusUrl
    // is configured — fills the system-metrics tiles from Prometheus instant
    // queries. Everything degrades gracefully: no Prometheus => metrics are 0.
    public static class AdminDashboardEndpoints
    {
        // Default PromQL for each metric tile, overridable via
        // Dashboard:MetricsQueries:<Name>. Metric names follow the OpenTelemetry
        // Prometheus exporter's convention and may need tuning per exporter
        // version. memoryUsage/diskUsage need infra-specific series (node-exporter
        // or cgroup) that aren't in the default scrape set, so they default to
        // empty (=> 0) until the operator supplies a query.
        private static readonly (string Key, string Default)[] MetricDefaults =
        {
            ("apiGatewayRequests", "sum(rate(http_server_request_duration_seconds_count[1m]))"),
            ("avgResponseTime", "sum(rate(http_server_request_duration_seconds_sum[1m]))/sum(rate(http_server_request_duration_seconds_count[1m]))*1000"),
            ("errorRate", "sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~\"5..\"}[1m]))/clamp_min(sum(rate(http_server_request_duration_seconds_count[1m])),1)*100"),
            ("activeConnections", "sum(http_server_active_requests)"),
            ("cpuUsage", "avg(process_cpu_utilization)*100"),
            ("memoryUsage", ""),
            ("diskUsage", ""),
        };


        public static void Map(RouteGroupBuilder admin)
        {
            var g = admin.MapGroup("/dashboard").WithTags("Admin/Dashboard");

            g.MapGet("/services/history", (HealthHistoryStore store, [FromQuery] int? hours) =>
            {
                var window = HistoryWindow(hours);
                var histories = store.ServiceIds
                    .Select(id => ToServiceHistory(id, store.Get(id, window)))
                    .ToList();
                return Results.Ok(histories);
            });

            g.MapGet("/services/{id}/history", (string id, HealthHistoryStore store, [FromQuery] int? hours) =>
            {
                return Results.Ok(ToServiceHistory(id, store.Get(id, HistoryWindow(hours))));
            });

            g.MapGet("/stats", async (
                HealthCheckService health,
                IPrometheusQueryClient prometheus,
                IConfiguration cfg,
                CancellationToken ct) =>
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

                var metrics = await QueryMetricsAsync(prometheus, cfg, ct);

                return Results.Ok(new
                {
                    services,
                    metrics,
                    lastUpdatedAt = now,
                    checksPerformed = services.Count,
                    alertsActive = services.Count(s => s.status != "healthy")
                });
            });
        }

        // Runs the (configurable) PromQL for each metric tile in parallel and
        // shapes the result into the dashboard's SystemMetrics contract. Unset
        // Prometheus or any query failure yields 0 for that tile.
        private static async Task<object> QueryMetricsAsync(IPrometheusQueryClient prometheus, IConfiguration cfg, CancellationToken ct)
        {
            var values = new Dictionary<string, double>(StringComparer.Ordinal);
            var tasks = MetricDefaults.Select(async m =>
            {
                var query = cfg[$"Dashboard:MetricsQueries:{m.Key}"] ?? m.Default;
                var value = await prometheus.QueryScalarAsync(query, ct);
                lock (values) values[m.Key] = value;
            });
            await Task.WhenAll(tasks);

            double V(string k) => values.TryGetValue(k, out var v) ? v : 0d;
            return new
            {
                apiGatewayRequests = V("apiGatewayRequests"),
                activeConnections = V("activeConnections"),
                cpuUsage = V("cpuUsage"),
                memoryUsage = V("memoryUsage"),
                diskUsage = V("diskUsage"),
                avgResponseTime = V("avgResponseTime"),
                errorRate = V("errorRate"),
            };
        }

        private static string MapStatus(HealthStatus status) => status switch
        {
            HealthStatus.Healthy => "healthy",
            HealthStatus.Degraded => "degraded",
            _ => "offline"
        };

        // Optional query param: omitted/invalid falls back to the full 24h window.
        private static TimeSpan HistoryWindow(int? hours) =>
            TimeSpan.FromHours(Math.Clamp(hours is null or <= 0 ? 24 : hours.Value, 1, 24));

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
