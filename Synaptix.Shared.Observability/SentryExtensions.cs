// NOTE: Sentry integration has been moved to Synaptix.Monitoring.SentryIntegration
// This file is kept for reference but is not actively used
#if FALSE
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
#endif

namespace Synaptix.Shared.Observability
{
    /// <summary>
    /// Sentry integration for centralized error tracking and performance monitoring.
    /// Captures unhandled exceptions, performance data, and breadcrumbs.
    /// </summary>
    public static class SentryExtensions
    {
        public static WebApplicationBuilder AddSentry(this WebApplicationBuilder builder)
        {
            var dsn = builder.Configuration["Sentry:Dsn"];

            if (string.IsNullOrWhiteSpace(dsn))
            {
                // Sentry not configured - logging will still work
                return builder;
            }

            var environment = builder.Environment.EnvironmentName.ToLower();
            var serviceName = builder.Configuration["Observability:ServiceName"] ?? "synaptix-backend";

            builder.WebHost.UseSentry(options =>
            {
                options.Dsn = dsn;
                options.Environment = environment;
                options.Release = builder.Configuration["App:Version"] ?? "unknown";

                // Only send errors to Sentry in production/staging, but always in dev for testing
                options.IsGlobalModeEnabled = true;
                options.TracesSampleRate = builder.Environment.IsDevelopment() ? 1.0 : 0.1;

                // Capture breadcrumbs for better context
                options.MaxBreadcrumbs = 200;
                options.CaptureFailedRequests = true;
                options.FailedRequestTargetStatusCodes.Add(new HttpStatusCodeRange(400, 599));

                // Add custom tags
                options.Tags["service"] = serviceName;
                options.Tags["cluster"] = builder.Configuration["Observability:Cluster"] ?? "unknown";

                // Track user info if authenticated
                options.DefaultUserFactory = new SentryUserFactory(builder.Configuration);

                // Exclude health check endpoints
                options.ShouldLogUrl = url =>
                {
                    return !url.Contains("/health")
                        && !url.Contains("/metrics")
                        && !url.Contains("/alive")
                        && !url.Contains("/ready");
                };
            });

            return builder;
        }

        public static WebApplication UseSentry(this WebApplication app)
        {
            app.UseSentryTracing();
            return app;
        }
    }

    /// <summary>
    /// Custom user factory to extract authenticated user info for Sentry
    /// </summary>
    internal class SentryUserFactory : ISentryUserFactory
    {
        private readonly IConfiguration _configuration;

        public SentryUserFactory(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SentryUser CreateUser(IHttpContext httpContext)
        {
            var user = new SentryUser();

            // Extract user from JWT claims if authenticated
            if (httpContext?.Request.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                var principal = httpContext.Request.HttpContext.User;

                user.Id = principal.FindFirst("sub")?.Value ?? principal.FindFirst("id")?.Value;
                user.Email = principal.FindFirst("email")?.Value;
                user.Username = principal.FindFirst("email")?.Value;

                // Add custom user segment if available
                var role = principal.FindFirst("role")?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    user.Other = new Dictionary<string, string> { { "role", role } };
                }
            }

            return user;
        }
    }
}
#endif
