namespace Synaptix.Backend.Api.Features.Monitoring;

/// <summary>
/// Hangfire job filter placeholder for monitoring job execution metrics.
/// Job metrics are collected via the HangfireMetricsCollector service instead.
/// </summary>
public static class HangfireJobFilterExtensions
{
    /// <summary>
    /// Extension method for registering job monitoring (currently a placeholder).
    /// Job metrics are collected via polling the HangfireMetricsCollector service.
    /// </summary>
    public static void AddJobMonitoring(this object config)
    {
        // Job monitoring is handled by HangfireMetricsCollector polling
        // which queries the Hangfire job storage directly
    }
}
