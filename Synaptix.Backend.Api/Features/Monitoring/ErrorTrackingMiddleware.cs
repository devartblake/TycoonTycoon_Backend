using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Synaptix.Monitoring.Errors;

namespace Synaptix.Backend.Api.Features.Monitoring;

/// <summary>
/// Middleware that automatically tracks HTTP request errors for monitoring.
/// Integrates with ErrorRateTracker to collect metrics on error rates per endpoint.
/// </summary>
public class ErrorTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ErrorRateTracker errorTracker)
    {
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;

            // Track the request in error rate tracker
            errorTracker.RecordRequest(endpoint, statusCode, stopwatch.Elapsed);
        }
    }
}

/// <summary>
/// Extension methods for error tracking middleware.
/// </summary>
public static class ErrorTrackingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorTracking(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ErrorTrackingMiddleware>();
    }
}
