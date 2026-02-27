using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tycoon.Backend.Api.Security
{
    /// <summary>
    /// Admin ops key gate for internal/privileged endpoints.
    ///
    /// Default behavior:
    /// - Applies to any request under /admin/analytics (legacy compatibility).
    /// - Also applies to any endpoint decorated with RequireAdminOpsKeyAttribute metadata.
    ///
    /// Recommended usage going forward:
    /// - Apply ops-key to all /admin/* routes using the RouteGroupBuilder extension:
    ///     var admin = app.MapGroup("/admin").RequireAdminOpsKey();
    ///
    /// Config:
    /// - AdminOps:Enabled (bool, default true)
    /// - AdminOps:Key (string)
    /// - AdminOps:Header (string, default "X-Admin-Ops-Key")
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
            var enabled = _cfg.GetValue("AdminOps:Enabled", true);
            if (!enabled)
            {
                await _next(ctx);
                return;
            }

            // Enforce ops-key if:
            // 1) request is under the admin analytics prefix (legacy default), OR
            // 2) endpoint metadata explicitly requests it.
            var endpoint = ctx.GetEndpoint();
            var metaEnforced = endpoint?.Metadata.GetMetadata<RequireAdminOpsKeyAttribute>() is not null;

            var pathEnforced = ctx.Request.Path.StartsWithSegments("/admin/analytics");

            if (!pathEnforced && !metaEnforced)
            {
                await _next(ctx);
                return;
            }

            var headerName = _cfg["AdminOps:Header"];
            if (string.IsNullOrWhiteSpace(headerName))
                headerName = "X-Admin-Ops-Key";

            var expectedKey = _cfg["AdminOps:Key"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                ctx.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await ctx.Response.WriteAsync("AdminOps key not configured.");
                return;
            }

            if (!ctx.Request.Headers.TryGetValue(headerName, out var provided) ||
                string.IsNullOrWhiteSpace(provided))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await ctx.Response.WriteAsync("Missing admin ops key.");
                return;
            }

            if (!string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
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

    /// <summary>
    /// Minimal API group wrapper. Prefer this for /admin/* route groups.
    /// This uses an endpoint filter so it scopes neatly to the group and does not require global middleware.
    /// </summary>
    public static class AdminOpsKeyRouteGroupExtensions
    {
        public static RouteGroupBuilder RequireAdminOpsKey(this RouteGroupBuilder group)
        {
            group.AddEndpointFilter(async (context, next) =>
            {
                var http = context.HttpContext;
                var cfg = http.RequestServices.GetRequiredService<IConfiguration>();

                var enabled = cfg.GetValue("AdminOps:Enabled", true);
                if (!enabled) return await next(context);

                var headerName = cfg["AdminOps:Header"];
                if (string.IsNullOrWhiteSpace(headerName))
                    headerName = "X-Admin-Ops-Key";

                var expectedKey = cfg["AdminOps:Key"] ?? string.Empty;
                if (string.IsNullOrWhiteSpace(expectedKey))
                    return Results.Problem("AdminOps key not configured.", statusCode: 503);

                if (!http.Request.Headers.TryGetValue(headerName, out var provided) ||
                    string.IsNullOrWhiteSpace(provided))
                    return Results.Unauthorized();

                if (!string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal))
                    return Results.StatusCode(StatusCodes.Status403Forbidden);

                return await next(context);
            });

            return group;
        }

        public static RouteGroupBuilder RequireAdminRoleClaims(this RouteGroupBuilder group)
        {
            group.AddEndpointFilter(async (context, next) =>
            {
                var http = context.HttpContext;
                var cfg = http.RequestServices.GetRequiredService<IConfiguration>();
                var isTesting = cfg.GetValue("Testing:UseInMemoryDb", false);

                if (isTesting)
                {
                    return await next(context);
                }

                if (http.User?.Identity?.IsAuthenticated != true)
                {
                    return Results.Unauthorized();
                }

                var role = http.User.FindFirst("role")?.Value
                           ?? http.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var audience = http.User.FindFirst("aud")?.Value;
                if (!string.Equals(audience, "admin-app", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var scope = http.User.FindFirst("scope")?.Value ?? string.Empty;
                var scopes = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (!scopes.Contains("users:read", StringComparer.OrdinalIgnoreCase))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var allowedEmails = cfg.GetSection("AdminAuth:AllowedEmails").Get<string[]>() ?? [];
                var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? http.User.FindFirst("email")?.Value;

                if (allowedEmails.Length > 0 && (string.IsNullOrWhiteSpace(email) || !allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase)))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return await next(context);
            });

            return group;
        }

    }
}
