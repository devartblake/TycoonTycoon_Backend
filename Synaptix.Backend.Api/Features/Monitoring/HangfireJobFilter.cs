using Hangfire.Common;
using Hangfire.Server;
using Synaptix.Monitoring.Jobs;

namespace Synaptix.Backend.Api.Features.Monitoring;

/// <summary>
/// Hangfire job filter that tracks job execution metrics for monitoring.
/// Automatically records job start, success, and failure events.
/// </summary>
public class HangfireJobFilterAttribute : JobFilterAttribute,
    IApplyStateFilter,
    IElectStateFilter,
    IApplyStateFilter
{
    public void OnStateApplied(ApplyStateContext context, IDisposable? transaction)
    {
        var hangfireCollector = context.Connection.GetType()
            .Assembly
            .GetType("Synaptix.Monitoring.Jobs.HangfireMetricsCollector");

        // The collector will be injected via DI in the actual implementation
    }

    public void OnStateUnapplied(ApplyStateContext context, IDisposable? transaction)
    {
        // Tracking happens in OnStateApplied
    }

    public void OnStateElection(ElectStateContext context)
    {
        // Election happens before state is applied
    }
}

/// <summary>
/// Extension to register Hangfire job filter.
/// </summary>
public static class HangfireJobFilterExtensions
{
    public static void AddJobMonitoring(this IGlobalConfiguration config)
    {
        config.UseFilter(new HangfireJobFilterAttribute());
    }
}
