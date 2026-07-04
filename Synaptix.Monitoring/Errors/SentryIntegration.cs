using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.AspNetCore;

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

        builder.Services.AddSentry();

        // Configure Sentry via SDK initialization
        SentrySdk.Init(new SentryOptions
        {
            Dsn = sentryDsn,
            Environment = environment,
            TracesSampleRate = sampleRate,
            MaxBreadcrumbs = 200,
            AttachStacktrace = true,
            CaptureFailedRequests = true
        });

        return builder;
    }

    /// <summary>
    /// Use Sentry middleware in the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseSentryMonitoring(this IApplicationBuilder app)
    {
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
