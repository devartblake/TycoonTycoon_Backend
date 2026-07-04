using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.Profiling;

namespace Synaptix.Monitoring.Errors;

/// <summary>
/// Sentry integration for centralized error tracking and performance monitoring.
/// Captures exceptions, performance traces, and provides breadcrumb tracking.
/// </summary>
public static class SentryIntegration
{
    /// <summary>
    /// Add Sentry error tracking to the application.
    /// Must be called before building the app.
    /// </summary>
    public static WebApplicationBuilder AddSentryMonitoring(
        this WebApplicationBuilder builder,
        string? dsn = null,
        double? tracesSampleRate = null)
    {
        // Get DSN from parameter or configuration
        var sentryDsn = dsn ?? builder.Configuration["Sentry:Dsn"];

        if (string.IsNullOrWhiteSpace(sentryDsn))
        {
            Console.WriteLine("⚠️  Sentry DSN not configured - error tracking disabled");
            return builder;
        }

        var environment = builder.Environment.EnvironmentName.ToLower();
        var sampleRate = tracesSampleRate ?? GetDefaultSampleRate(environment);

        Console.WriteLine($"✅ Configuring Sentry (env: {environment}, sampling: {sampleRate * 100}%)");

        builder.WebHost.UseSentry(options =>
        {
            options.Dsn = sentryDsn;
            options.Environment = environment;
            options.TracesSampleRate = sampleRate;
            options.MaxBreadcrumbs = 200;

            // Enable performance monitoring
            options.EnableTracing = true;

            // Attach stacktrace to all messages
            options.AttachStacktrace = true;

            // Capture unhandled exceptions
            options.CaptureFailedRequests = true;

            // Track server-side HTTP 5xx errors
            options.FailedRequestStatusCodes.Add((500, 599));

            // Request body capture (be careful with PII)
            options.MaxRequestBodySize = RequestSize.Small;

            // Include local variables in stack traces (performance overhead)
            options.IncludeLocalVariables = false;

            // Custom fingerprinting for grouping
            options.BeforeSend = (sentryEvent, hint) =>
            {
                // Don't send 404s or 401s to Sentry
                if (sentryEvent.Request?.Url != null)
                {
                    if (sentryEvent.Request.Url.Contains("/health") ||
                        sentryEvent.Request.Url.Contains("/metrics") ||
                        sentryEvent.Request.Url.Contains("/alive"))
                    {
                        return null; // Discard health check events
                    }
                }

                return sentryEvent;
            };

            // Add custom tags for filtering
            options.SetTag("service", "backend-api");
            options.SetTag("framework", ".NET");
        });

        return builder;
    }

    /// <summary>
    /// Use Sentry middleware in the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseSentryMonitoring(this IApplicationBuilder app)
    {
        app.UseSentryTracing();
        return app;
    }

    /// <summary>
    /// Get default sampling rate based on environment.
    /// </summary>
    private static double GetDefaultSampleRate(string environment)
    {
        return environment switch
        {
            "development" => 1.0,    // 100% in development
            "staging" => 0.5,        // 50% in staging
            "production" => 0.1,     // 10% in production (cost control)
            _ => 0.1,
        };
    }

    /// <summary>
    /// Manually capture an exception with Sentry.
    /// </summary>
    public static void CaptureException(Exception exception, string? message = null, SentryLevel level = SentryLevel.Error)
    {
        var sentryEvent = new SentryEvent(exception)
        {
            Level = level,
            Message = message,
        };

        SentrySdk.CaptureEvent(sentryEvent);
    }

    /// <summary>
    /// Add a breadcrumb for debugging context.
    /// </summary>
    public static void AddBreadcrumb(string message, string category = "debug", BreadcrumbLevel level = BreadcrumbLevel.Info)
    {
        SentrySdk.AddBreadcrumb(message, category, level: level);
    }

    /// <summary>
    /// Set user context for error tracking.
    /// </summary>
    public static void SetUserContext(string userId, string? email = null, string? username = null)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = new SentryUser
            {
                Id = userId,
                Email = email,
                Username = username,
            };
        });
    }

    /// <summary>
    /// Clear user context on logout.
    /// </summary>
    public static void ClearUserContext()
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.User = null;
        });
    }

    /// <summary>
    /// Set custom tag for filtering in Sentry.
    /// </summary>
    public static void SetTag(string key, string value)
    {
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag(key, value);
        });
    }
}
