using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Synaptix.Backend.Api.Features.AdminDashboard
{
    public sealed record HealthSample(DateTimeOffset Timestamp, HealthStatus Status, double ResponseTimeMs);

    // In-memory time series of health-check results for the operator dashboard.
    // Bounded per service; resets on process restart (durable metrics remain in
    // Prometheus/Grafana — this only backs the dashboard's recent-history view).
    public sealed class HealthHistoryStore
    {
        private const int MaxSamplesPerService = 1440; // 24h at one sample/minute

        private readonly ConcurrentDictionary<string, ConcurrentQueue<HealthSample>> _series = new();

        public void Record(string serviceId, HealthSample sample)
        {
            var queue = _series.GetOrAdd(serviceId, _ => new ConcurrentQueue<HealthSample>());
            queue.Enqueue(sample);
            while (queue.Count > MaxSamplesPerService && queue.TryDequeue(out _)) { }
        }

        public IReadOnlyList<HealthSample> Get(string serviceId, TimeSpan window)
        {
            var cutoff = DateTimeOffset.UtcNow - window;
            return _series.TryGetValue(serviceId, out var queue)
                ? queue.Where(s => s.Timestamp >= cutoff).ToList()
                : Array.Empty<HealthSample>();
        }

        public IReadOnlyList<string> ServiceIds => _series.Keys.ToList();
    }

    public sealed class HealthHistorySampler(
        HealthCheckService health,
        HealthHistoryStore store,
        IConfiguration cfg,
        ILogger<HealthHistorySampler> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(
                Math.Max(15, cfg.GetValue("Dashboard:HealthSampleIntervalSeconds", 60)));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var report = await health.CheckHealthAsync(stoppingToken);
                    var now = DateTimeOffset.UtcNow;
                    foreach (var entry in report.Entries)
                        store.Record(entry.Key, new HealthSample(now, entry.Value.Status, entry.Value.Duration.TotalMilliseconds));
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Health history sampling failed; will retry next interval.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
