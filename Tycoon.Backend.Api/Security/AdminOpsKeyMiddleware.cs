using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Tycoon.Backend.Api.Security
{
    /// <summary>
    /// Admin ops key gate for internal/privileged endpoints.
    /// Default behavior:
    /// - Applies to any request under /admin/analytics
    /// - Requires header X-Admin-Ops-Key to match AdminOps:Key
    ///
    /// Optional behavior (future-proof):
    /// - If an endpoint is decorated with RequireAdminOpsKeyAttribute metadata,
    ///   this middleware will enforce the ops key even outside /admin/analytics.
    /// </summary>
    public sealed class AdminOpsKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _cfg;

        public AdminOpsKeyMiddleware(RequestDelegate next, IConfiguration cfg)
        {
            _next = next;
            _cfg = cfg;
        }

        public async Task Invoke(HttpContext ctx)
        {
            // Enforce ops-key if:
            // 1) request is under the admin analytics prefix, OR
            // 2) endpoint metadata explicitly requests it.
            var endpoint = ctx.GetEndpoint();
            var metaEnforced = endpoint?.Metadata.GetMetadata<RequireAdminOpsKeyAttribute>() is not null;

            var pathEnforced = ctx.Request.Path.StartsWithSegments("/admin/analytics");

            if (!pathEnforced && !metaEnforced)
            {
                await _next(ctx);
                return;
            }

            var key = _cfg["AdminOps:Key"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await ctx.Response.WriteAsync("AdminOps key not configured.");
                return;
            }

            var headerName = _cfg["AdminOps:Header"];
            if (string.IsNullOrWhiteSpace(headerName))
                headerName = "X-Admin-Ops-Key";

            if (!ctx.Request.Headers.TryGetValue(headerName, out var provided) ||
                !string.Equals(provided.ToString(), key, StringComparison.Ordinal))
            {
                // 401 is more semantically correct than 403 here because authentication failed.
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Invalid admin ops key.");
                return;
            }

            await _next(ctx);
        }
    }

    /// <summary>
    /// Optional endpoint metadata flag. If used, AdminOpsKeyMiddleware will enforce ops-key
    /// even if the route is not under /admin/analytics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireAdminOpsKeyAttribute : Attribute { }
}
