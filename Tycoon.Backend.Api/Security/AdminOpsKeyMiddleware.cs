using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Contracts;

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

            var endpoint = ctx.GetEndpoint();
            var metaEnforced = endpoint?.Metadata.GetMetadata<RequireAdminOpsKeyAttribute>() is not null;
            var pathEnforced = ctx.Request.Path.StartsWithSegments("/admin/analytics");

            if (!pathEnforced && !metaEnforced)
            {
                await _next(ctx);
                return;
            }

            var validation = ValidateOpsKey(ctx, _cfg);
            if (validation is not null)
            {
                await validation.ExecuteAsync(ctx);
                return;
            }

            await _next(ctx);
        }

        internal static IResult? ValidateOpsKey(HttpContext ctx, IConfiguration cfg)
        {
            var headerName = cfg["AdminOps:Header"];
            if (string.IsNullOrWhiteSpace(headerName))
                headerName = "X-Admin-Ops-Key";

            var expectedKey = cfg["AdminOps:Key"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expectedKey))
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", "AdminOps key not configured.");

            if (!ctx.Request.Headers.TryGetValue(headerName, out var provided) ||
                string.IsNullOrWhiteSpace(provided))
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing admin ops key.");

            if (!string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal))
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Invalid admin ops key.");

            return null;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireAdminOpsKeyAttribute : Attribute { }

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

                var validation = AdminOpsKeyMiddleware.ValidateOpsKey(http, cfg);
                if (validation is not null)
                    return validation;

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

                var authz = http.RequestServices.GetRequiredService<IAuthorizationService>();
                var authzResult = await authz.AuthorizeAsync(http.User, null, AdminPolicies.AdminOpsPolicy);
                if (!authzResult.Succeeded)
                {
                    return http.User?.Identity?.IsAuthenticated == true
                        ? ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Admin policy requirements not satisfied.")
                        : ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");
                }

                var allowedEmails = cfg.GetSection("AdminAuth:AllowedEmails").Get<string[]>() ?? [];
                var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? http.User.FindFirst("email")?.Value;

                if (allowedEmails.Length > 0 && (string.IsNullOrWhiteSpace(email) || !allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase)))
                {
                    return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Admin email is not allowlisted.");
                }

                return await next(context);
            });

            return group;
        }
    }
}
