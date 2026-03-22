using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

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
        private readonly ILogger<AdminOpsKeyMiddleware> _logger;

        public AdminOpsKeyMiddleware(RequestDelegate next, IConfiguration cfg, ILogger<AdminOpsKeyMiddleware> logger)
        {
            _next = next;
            _cfg = cfg;
            _logger = logger;
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

            var validation = ValidateOpsKey(ctx, _cfg, _logger);
            if (validation is not null)
            {
                await validation.ExecuteAsync(ctx);
                return;
            }

            await _next(ctx);
        }

        internal static IResult? ValidateOpsKey(HttpContext ctx, IConfiguration cfg, ILogger? logger = null)
        {
            var headerName = cfg["AdminOps:Header"];
            if (string.IsNullOrWhiteSpace(headerName))
                headerName = "X-Admin-Ops-Key";

            var expectedKey = cfg["AdminOps:Key"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expectedKey))
            {
                logger?.LogWarning("AdminOpsKey: AdminOps:Key is not configured in settings");
                return ApiResponses.Error(StatusCodes.Status503ServiceUnavailable, "SERVICE_UNAVAILABLE", "AdminOps key not configured.");
            }

            if (!ctx.Request.Headers.TryGetValue(headerName, out var provided) ||
                string.IsNullOrWhiteSpace(provided))
            {
                logger?.LogWarning("AdminOpsKey: {HeaderName} header missing or empty on {Method} {Path}",
                    headerName, ctx.Request.Method, ctx.Request.Path);
                return ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing admin ops key.");
            }

            if (!string.Equals(provided.ToString(), expectedKey, StringComparison.Ordinal))
            {
                var providedStr = provided.ToString();
                var suffix = providedStr.Length >= 4 ? providedStr[^4..] : "****";
                logger?.LogWarning("AdminOpsKey: provided key does not match expected key on {Method} {Path} (provided ends with ...{KeySuffix})",
                    ctx.Request.Method, ctx.Request.Path, suffix);
                return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Invalid admin ops key.");
            }

            logger?.LogDebug("AdminOpsKey: validated successfully for {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
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

                var logger = http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AdminOpsKeyFilter");
                var validation = AdminOpsKeyMiddleware.ValidateOpsKey(http, cfg, logger);
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

                var logger = http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AdminRoleClaims");

                var user = http.User;
                var isAuthenticated = user?.Identity?.IsAuthenticated == true;
                var roles = user?.FindAll(System.Security.Claims.ClaimTypes.Role)
                    .Concat(user.FindAll("role"))
                    .Select(c => c.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? [];
                var hasAdminRole = roles.Contains("admin", StringComparer.OrdinalIgnoreCase);
                var audClaims = user?.FindAll("aud").Select(c => c.Value).ToList() ?? [];
                var hasAdminAud = audClaims.Contains("admin-app", StringComparer.Ordinal);
                var scopeClaim = user?.FindFirst("scope")?.Value;
                var hasUsersReadScope = scopeClaim?.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Contains("users:read", StringComparer.OrdinalIgnoreCase) == true;

                logger.LogDebug(
                    "AdminRoleClaims check for {Method} {Path}: Authenticated={IsAuthenticated}, Roles=[{Roles}], HasAdminRole={HasAdminRole}, Aud=[{Aud}], HasAdminAppAud={HasAdminAud}, Scope={Scope}, HasUsersReadScope={HasUsersReadScope}",
                    http.Request.Method, http.Request.Path, isAuthenticated, string.Join(",", roles), hasAdminRole,
                    string.Join(",", audClaims), hasAdminAud, scopeClaim ?? "(none)", hasUsersReadScope);

                var authz = http.RequestServices.GetRequiredService<IAuthorizationService>();
                var authzResult = await authz.AuthorizeAsync(http.User, null, AdminPolicies.AdminOpsPolicy);
                if (!authzResult.Succeeded)
                {
                    var reason = !isAuthenticated ? "user is not authenticated"
                        : !hasAdminRole ? $"JWT missing role=admin (found roles: [{string.Join(",", roles)}])"
                        : !hasAdminAud ? $"JWT missing aud=admin-app (found aud: [{string.Join(",", audClaims)}])"
                        : !hasUsersReadScope ? $"JWT missing scope users:read (found scope: {scopeClaim ?? "(none)"})"
                        : "unknown policy failure (check AuthorizationFailure.FailureReasons)";

                    logger.LogWarning("AdminRoleClaims FAILED for {Method} {Path}: {Reason}",
                        http.Request.Method, http.Request.Path, reason);

                    return isAuthenticated
                        ? ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Admin policy requirements not satisfied.")
                        : ApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Authentication required.");
                }

                var email = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                            ?? http.User.FindFirst("email")?.Value;
                var normalizedEmail = email?.Trim().ToLowerInvariant();

                // Check DB-backed email ACL. Blocklist takes precedence.
                try
                {
                    var db = http.RequestServices.GetRequiredService<IAppDb>();
                    var aclEntry = string.IsNullOrWhiteSpace(normalizedEmail)
                        ? null
                        : await db.AdminEmailAcls.AsNoTracking()
                            .FirstOrDefaultAsync(e => e.NormalizedEmail == normalizedEmail, http.RequestAborted);

                    if (aclEntry is not null && aclEntry.ListType == AdminAclListType.Block)
                    {
                        logger.LogWarning("AdminRoleClaims email BLOCKED for {Method} {Path}: email={Email}",
                            http.Request.Method, http.Request.Path, normalizedEmail);
                        return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Admin email is blocked.");
                    }

                    // If ACL entries exist, require the email to be on the allowlist
                    var hasAclEntries = await db.AdminEmailAcls.AsNoTracking()
                        .AnyAsync(e => e.ListType == AdminAclListType.Allow, http.RequestAborted);

                    if (hasAclEntries && (aclEntry is null || aclEntry.ListType != AdminAclListType.Allow))
                    {
                        logger.LogWarning("AdminRoleClaims email allowlist FAILED for {Method} {Path}: email={Email}",
                            http.Request.Method, http.Request.Path, normalizedEmail ?? "(no email claim)");
                        return ApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Admin email is not allowlisted.");
                    }

                    logger.LogDebug("AdminRoleClaims PASSED for {Method} {Path}, email={Email}, role={AclRole}",
                        http.Request.Method, http.Request.Path, normalizedEmail ?? "(none)", aclEntry?.Role.ToString() ?? "default");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "AdminEmailAcls query failed in RequireAdminRoleClaims (table may not exist yet). Allowing access by default.");
                }

                return await next(context);
            });

            return group;
        }
    }
}
