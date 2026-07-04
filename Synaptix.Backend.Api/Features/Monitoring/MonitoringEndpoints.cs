using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Synaptix.Monitoring.Errors;
using Synaptix.Monitoring.Jobs;

namespace Synaptix.Backend.Api.Features.Monitoring;

/// <summary>
/// Endpoints for exposing job and error monitoring metrics.
/// Used by monitoring dashboards and alert systems.
/// </summary>
public static class MonitoringEndpoints
{
    public static void MapMonitoringEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/monitoring")
            .AllowAnonymous()
            .WithTags("Monitoring");

        group.MapGet("/jobs/metrics", GetJobMetrics)
            .WithName("GetJobMetrics")
            .WithOpenApi()
            .WithSummary("Get current Hangfire job metrics");

        group.MapGet("/errors/summary", GetErrorSummary)
            .WithName("GetErrorSummary")
            .WithOpenApi()
            .WithSummary("Get error rate summary");

        group.MapGet("/errors/by-endpoint", GetErrorsByEndpoint)
            .WithName("GetErrorsByEndpoint")
            .WithOpenApi()
            .WithSummary("Get error metrics by endpoint");

        group.MapGet("/errors/high-rate", GetHighErrorRateEndpoints)
            .WithName("GetHighErrorRateEndpoints")
            .WithOpenApi()
            .WithSummary("Get endpoints with high error rates");
    }

    private static IResult GetJobMetrics(HangfireMetricsCollector collector)
    {
        var metrics = collector.GetCurrentMetrics();
        return Results.Ok(metrics);
    }

    private static IResult GetErrorSummary(ErrorRateTracker tracker)
    {
        var summary = tracker.GetSummary();
        return Results.Ok(summary);
    }

    private static IResult GetErrorsByEndpoint(ErrorRateTracker tracker)
    {
        var metrics = tracker.GetAllMetrics()
            .OrderByDescending(m => m.ErrorRate)
            .ToList();

        return Results.Ok(metrics);
    }

    private static IResult GetHighErrorRateEndpoints(ErrorRateTracker tracker)
    {
        var highErrorEndpoints = tracker.GetHighErrorRateEndpoints()
            .ToList();

        return Results.Ok(new
        {
            HighErrorRateEndpoints = highErrorEndpoints,
            Count = highErrorEndpoints.Count,
            Timestamp = DateTime.UtcNow,
        });
    }
}
