using Microsoft.Extensions.DependencyInjection;
using Synaptix.Monitoring.Errors;
using Synaptix.Monitoring.Jobs;

namespace Synaptix.Monitoring;

/// <summary>
/// Dependency injection extensions for monitoring services.
/// Registers Hangfire metrics collection and error rate tracking.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Add all monitoring services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<ErrorRateTracker>();
        services.AddSingleton<HangfireMetricsCollector>();

        return services;
    }
}
