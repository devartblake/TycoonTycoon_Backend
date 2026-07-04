using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Synaptix.Monitoring.Errors;

/// <summary>
/// Tracks HTTP error rates by endpoint for alerting and monitoring.
/// Maintains rolling counters for last 1min, 5min, 15min periods.
/// </summary>
public class ErrorRateTracker
{
    private static readonly ActivitySource ErrorActivitySource = new("Synaptix.Errors");

    private readonly ConcurrentDictionary<string, EndpointErrorMetrics> _endpointMetrics;
    private readonly ILogger<ErrorRateTracker> _logger;
    private readonly object _lockObj = new();

    // Configuration
    private const int WindowSizeSeconds = 60;
    private const double ErrorRateThreshold = 0.05; // 5% error rate triggers alert

    public ErrorRateTracker(ILogger<ErrorRateTracker> logger)
    {
        _logger = logger;
        _endpointMetrics = new ConcurrentDictionary<string, EndpointErrorMetrics>();
    }

    /// <summary>
    /// Record a request outcome (success or failure) for an endpoint.
    /// </summary>
    public void RecordRequest(string endpoint, int statusCode, TimeSpan duration)
    {
        var isError = statusCode >= 400;

        var metrics = _endpointMetrics.AddOrUpdate(
            endpoint,
            _ => new EndpointErrorMetrics(endpoint),
            (_, existing) => existing
        );

        lock (_lockObj)
        {
            metrics.TotalRequests++;
            if (isError)
            {
                metrics.ErrorCount++;
                metrics.LastErrorTime = DateTime.UtcNow;
                metrics.LastErrorStatus = statusCode;
            }

            metrics.TotalDuration += duration;

            // Clean old entries
            metrics.CleanOldEntries();
        }

        // Record OpenTelemetry activity for error tracking
        if (isError)
        {
            using var activity = ErrorActivitySource.StartActivity($"http.error", ActivityKind.Internal);
            if (activity != null)
            {
                activity.SetTag("http.endpoint", endpoint);
                activity.SetTag("http.status_code", statusCode);
                activity.SetTag("http.duration_ms", duration.TotalMilliseconds);
                activity.AddEvent(new ActivityEvent("http_error"));
            }

            _logger.LogWarning(
                "HTTP Error: {Endpoint} - Status: {StatusCode} - Duration: {Duration}ms",
                endpoint, statusCode, duration.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Get current error rate metrics for an endpoint.
    /// </summary>
    public ErrorRateMetrics? GetEndpointMetrics(string endpoint)
    {
        if (_endpointMetrics.TryGetValue(endpoint, out var metrics))
        {
            lock (_lockObj)
            {
                return metrics.GetCurrentMetrics();
            }
        }

        return null;
    }

    /// <summary>
    /// Get all endpoint metrics snapshots.
    /// </summary>
    public IEnumerable<ErrorRateMetrics> GetAllMetrics()
    {
        foreach (var metrics in _endpointMetrics.Values)
        {
            lock (_lockObj)
            {
                yield return metrics.GetCurrentMetrics();
            }
        }
    }

    /// <summary>
    /// Get endpoints with high error rates (above threshold).
    /// </summary>
    public IEnumerable<ErrorRateMetrics> GetHighErrorRateEndpoints()
    {
        return GetAllMetrics()
            .Where(m => m.ErrorRate > ErrorRateThreshold)
            .OrderByDescending(m => m.ErrorRate);
    }

    /// <summary>
    /// Get summary of all error metrics.
    /// </summary>
    public ErrorRateSummary GetSummary()
    {
        var allMetrics = GetAllMetrics().ToList();

        return new ErrorRateSummary
        {
            TotalEndpoints = allMetrics.Count,
            TotalRequests = allMetrics.Sum(m => m.TotalRequests),
            TotalErrors = allMetrics.Sum(m => m.ErrorCount),
            AverageErrorRate = allMetrics.Any() ? allMetrics.Average(m => m.ErrorRate) : 0,
            HighErrorRateEndpoints = allMetrics
                .Where(m => m.ErrorRate > ErrorRateThreshold)
                .Select(m => m.Endpoint)
                .ToList(),
            MaxErrorRate = allMetrics.Any() ? allMetrics.Max(m => m.ErrorRate) : 0,
            LastErrorTime = allMetrics.Max(m => (DateTime?)m.LastErrorTime),
        };
    }
}

/// <summary>
/// Tracks error metrics for a single endpoint.
/// </summary>
internal class EndpointErrorMetrics
{
    private readonly struct RequestEntry
    {
        public DateTime Timestamp { get; set; }
        public bool IsError { get; set; }
    }

    private readonly List<RequestEntry> _requests = new();
    public string Endpoint { get; }
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public int? LastErrorStatus { get; set; }

    public EndpointErrorMetrics(string endpoint)
    {
        Endpoint = endpoint;
    }

    public void CleanOldEntries()
    {
        var cutoffTime = DateTime.UtcNow.AddSeconds(-WindowSizeSeconds);
        _requests.RemoveAll(r => r.Timestamp < cutoffTime);
    }

    public ErrorRateMetrics GetCurrentMetrics()
    {
        var totalRequests = TotalRequests;
        var errorCount = ErrorCount;
        var errorRate = totalRequests > 0 ? (double)errorCount / totalRequests : 0;

        return new ErrorRateMetrics
        {
            Endpoint = Endpoint,
            TotalRequests = totalRequests,
            ErrorCount = errorCount,
            ErrorRate = errorRate,
            AverageDuration = totalRequests > 0 ? TotalDuration / totalRequests : TimeSpan.Zero,
            LastErrorTime = LastErrorTime,
            LastErrorStatus = LastErrorStatus,
            HighErrorRate = errorRate > 0.05,
        };
    }
}

/// <summary>
/// Error rate metrics for a single endpoint.
/// </summary>
public record ErrorRateMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public DateTime? LastErrorTime { get; set; }
    public int? LastErrorStatus { get; set; }
    public bool HighErrorRate { get; set; }
}

/// <summary>
/// Summary of error metrics across all endpoints.
/// </summary>
public record ErrorRateSummary
{
    public int TotalEndpoints { get; set; }
    public int TotalRequests { get; set; }
    public int TotalErrors { get; set; }
    public double AverageErrorRate { get; set; }
    public List<string> HighErrorRateEndpoints { get; set; } = new();
    public double MaxErrorRate { get; set; }
    public DateTime? LastErrorTime { get; set; }
}
