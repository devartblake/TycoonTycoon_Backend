using System.Diagnostics;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Synaptix.Monitoring.Jobs;

/// <summary>
/// Collects metrics from Hangfire job queue for Prometheus monitoring.
/// Tracks job success/failure rates, execution times, and queue depth.
/// Note: This collector provides a foundation for monitoring. Actual metrics
/// collection should be implemented through Prometheus exporters or periodic polling.
/// </summary>
public class HangfireMetricsCollector
{
    private static readonly ActivitySource JobActivitySource = new("Synaptix.Hangfire.Jobs");
    private readonly ILogger<HangfireMetricsCollector> _logger;

    public HangfireMetricsCollector(ILogger<HangfireMetricsCollector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Collect current Hangfire job queue metrics.
    /// Called periodically by monitoring system.
    /// </summary>
    public JobMetricsSnapshot GetCurrentMetrics()
    {
        try
        {
            // Placeholder metrics - in production, integrate with Hangfire's storage directly
            // or use Prometheus Hangfire exporter library
            return new JobMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                EnqueuedCount = 0,
                ScheduledCount = 0,
                ProcessingCount = 0,
                SucceededCount = 0,
                FailedCount = 0,
                DeletedCount = 0,
                ReccurringJobCount = 0,
                ServerCount = 1,
                QueueDepth = 0,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Hangfire metrics");
            return new JobMetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                ErrorOccurred = true,
                ErrorMessage = ex.Message,
            };
        }
    }

    /// <summary>
    /// Get detailed metrics for a specific queue.
    /// </summary>
    public QueueMetrics GetQueueMetrics(string queueName)
    {
        try
        {
            return new QueueMetrics
            {
                QueueName = queueName,
                EnqueuedCount = 0,
                ProcessingCount = 0,
                FailedCount = 0,
                QueueHealthy = true,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect queue metrics for queue: {QueueName}", queueName);
            return new QueueMetrics
            {
                QueueName = queueName,
                ErrorOccurred = true,
            };
        }
    }

    /// <summary>
    /// Record a job execution event (start, success, failure).
    /// Creates an OpenTelemetry activity for tracing.
    /// </summary>
    public void RecordJobExecution(string jobId, string jobType, JobExecutionStatus status, TimeSpan duration, string? errorMessage = null)
    {
        using var activity = JobActivitySource.StartActivity($"hangfire.job.execute", ActivityKind.Internal);

        if (activity != null)
        {
            activity.SetTag("hangfire.job.id", jobId);
            activity.SetTag("hangfire.job.type", jobType);
            activity.SetTag("hangfire.job.status", status.ToString());
            activity.SetTag("hangfire.job.duration_ms", duration.TotalMilliseconds);

            if (status == JobExecutionStatus.Failed && !string.IsNullOrEmpty(errorMessage))
            {
                activity.SetTag("hangfire.job.error", errorMessage);
                activity.AddEvent(new ActivityEvent("job_failed"));
            }
        }

        _logger.LogInformation(
            "Job executed: {JobType} (ID: {JobId}) - Status: {Status} - Duration: {Duration}ms",
            jobType, jobId, status, duration.TotalMilliseconds);
    }
}

/// <summary>
/// Snapshot of current Hangfire job queue state.
/// </summary>
public record JobMetricsSnapshot
{
    public DateTime Timestamp { get; set; }
    public int EnqueuedCount { get; set; }
    public int ScheduledCount { get; set; }
    public int ProcessingCount { get; set; }
    public int SucceededCount { get; set; }
    public int FailedCount { get; set; }
    public int DeletedCount { get; set; }
    public int ReccurringJobCount { get; set; }
    public int ServerCount { get; set; }
    public int QueueDepth { get; set; }
    public bool ErrorOccurred { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Metrics for a specific job queue.
/// </summary>
public record QueueMetrics
{
    public string QueueName { get; set; } = string.Empty;
    public int EnqueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int FailedCount { get; set; }
    public bool QueueHealthy { get; set; } = true;
    public bool ErrorOccurred { get; set; }
}

/// <summary>
/// Hangfire job execution outcome.
/// </summary>
public enum JobExecutionStatus
{
    Enqueued,
    Processing,
    Succeeded,
    Failed,
    Deleted,
}
