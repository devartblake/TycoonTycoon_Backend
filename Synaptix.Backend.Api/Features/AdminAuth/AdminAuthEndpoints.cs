using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Observability;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.AdminAuth;

public static class AdminAuthEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/auth").WithTags("Admin/Auth");

        g.MapPost("/login", Login).RequireRateLimiting("admin-auth-login").RequireSecureChannel().AllowTrustedBffPlainJson();
        g.MapPost("/refresh", Refresh).RequireRateLimiting("admin-auth-refresh").RequireSecureChannel().AllowTrustedBffPlainJson();
        g.MapGet("/me", Me).RequireAuthorization(AdminPolicies.AdminOpsPolicy);
    }

    private static string GetClientIp(HttpContext http)
    {
        var forwarded = http.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
            return forwarded.Split(',')[0].Trim();
        return http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string GetUserAgent(HttpContext http) =>
        http.Request.Headers["User-Agent"].ToString() is { Length: > 0 } ua ? ua : "unknown";

    private static async Task<IResult> Login(
        [FromBody] AdminLoginRequest request,
        HttpContext http,
        IAuthService authService,
        IAppDb db,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp(http);
        var ua = GetUserAgent(http);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            await AdminSecurityAudit.WriteAsync(db, "admin_auth_login", "validation_error", new { ip, userAgent = ua, reason = "missing_email_or_password" }, ct);
            AdminSecurityMetrics.RecordAuth("login", "validation_error", sw);
            return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "Email and password are required.");
        }

        try
        {
            var auth = await authService.AdminLoginAsync(request.Email, request.Password, deviceId: "admin-web");

            var aclLogger = loggerFactory.CreateLogger("AdminEmailAcl");
            var adminRole = await GetAllowedAdminRoleAsync(request.Email, db, aclLogger, ct);
            if (adminRole is null)
            {
                await AdminSecurityAudit.WriteAsync(db, "admin_auth_login", "forbidden", new { actor = request.Email, ip, userAgent = ua, reason = "email_not_allowlisted" }, ct);
                AdminSecurityMetrics.RecordAuth("login", "forbidden", sw);
                return AdminApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Authenticated user is not an admin.");
            }

            var permissions = AdminPermissionProfiles.ForRole(adminRole.Value);
            var profile = new AdminProfileResponse(
                Id: $"adm_{auth.User.Id:N}",
                Email: auth.User.Email,
                DisplayName: auth.User.Handle,
                Roles: permissions.Roles,
                Permissions: permissions.Permissions
            );

            await AdminSecurityAudit.WriteAsync(db, "admin_auth_login", "success", new { actor = request.Email, ip, userAgent = ua }, ct);
            AdminSecurityMetrics.RecordAuth("login", "success", sw);

            return Results.Ok(new AdminLoginResponse(
                AccessToken: auth.AccessToken,
                RefreshToken: auth.RefreshToken,
                ExpiresIn: auth.ExpiresIn,
                TokenType: "Bearer",
                Admin: profile
            ));
        }
        catch (UnauthorizedAccessException)
        {
            await AdminSecurityAudit.WriteAsync(db, "admin_auth_login", "unauthorized", new { actor = request.Email, ip, userAgent = ua }, ct);
            AdminSecurityMetrics.RecordAuth("login", "unauthorized", sw);
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Invalid credentials.");
        }
    }

    private static async Task<IResult> Refresh([FromBody] RefreshRequest request, HttpContext http, IAuthService authService, IAppDb db, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp(http);
        var ua = GetUserAgent(http);
        var actor = http.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;

        try
        {
            var auth = await authService.AdminRefreshAsync(request.RefreshToken);
            await AdminSecurityAudit.WriteAsync(db, "admin_auth_refresh", "success", new { actor, ip, userAgent = ua }, ct);
            AdminSecurityMetrics.RecordAuth("refresh", "success", sw);
            return Results.Ok(new AdminRefreshResponse(auth.AccessToken, auth.ExpiresIn, "Bearer"));
        }
        catch (UnauthorizedAccessException)
        {
            await AdminSecurityAudit.WriteAsync(db, "admin_auth_refresh", "unauthorized", new { actor, ip, userAgent = ua }, ct);
            AdminSecurityMetrics.RecordAuth("refresh", "unauthorized", sw);
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Refresh token is invalid or expired.");
        }
    }

    private static async Task<IResult> Me(HttpContext httpContext, IAppDb db, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var ip = GetClientIp(httpContext);
        var ua = GetUserAgent(httpContext);

        var sub = httpContext.User.FindFirst("sub")?.Value
                  ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var name = httpContext.User.Identity?.Name ?? email;

        if (string.IsNullOrWhiteSpace(sub))
        {
            await AdminSecurityAudit.WriteAsync(db, "admin_auth_me", "unauthorized", new { ip, userAgent = ua, reason = "missing_sub" }, ct);
            AdminSecurityMetrics.RecordAuth("me", "unauthorized", sw);
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing authenticated subject.");
        }

        await AdminSecurityAudit.WriteAsync(db, "admin_auth_me", "success", new { actor = sub, ip, userAgent = ua }, ct);
        AdminSecurityMetrics.RecordAuth("me", "success", sw);

        var roleClaim = httpContext.User.FindFirst("admin_role")?.Value;
        var role = Enum.TryParse<AdminRole>(roleClaim, ignoreCase: true, out var parsed)
            ? parsed
            : httpContext.User.FindFirst("scope")?.Value?.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains("acl:write", StringComparer.OrdinalIgnoreCase) == true
                ? AdminRole.SuperAdmin
                : AdminRole.Admin;
        var permissions = AdminPermissionProfiles.ForRole(role);

        return Results.Ok(new AdminProfileResponse(
            Id: $"adm_{sub}",
            Email: email,
            DisplayName: name,
            Roles: permissions.Roles,
            Permissions: permissions.Permissions
        ));
    }

    private static async Task<AdminRole?> GetAllowedAdminRoleAsync(string email, IAppDb db, ILogger logger, CancellationToken ct)
    {
        try
        {
            var normalized = email.Trim().ToLowerInvariant();

            // Blocklist always wins
            var isBlocked = await db.AdminEmailAcls.AsNoTracking()
                .AnyAsync(e => e.NormalizedEmail == normalized && e.ListType == AdminAclListType.Block, ct);
            if (isBlocked) return null;

            // If no allowlist entries exist, permit all (open access)
            var hasAllowEntries = await db.AdminEmailAcls.AsNoTracking()
                .AnyAsync(e => e.ListType == AdminAclListType.Allow, ct);
            if (!hasAllowEntries) return AdminRole.SuperAdmin;

            // Email must be on the allowlist
            var entry = await db.AdminEmailAcls.AsNoTracking()
                .FirstOrDefaultAsync(e => e.NormalizedEmail == normalized && e.ListType == AdminAclListType.Allow, ct);
            return entry?.Role;
        }
        catch (Exception ex)
        {
            // Table may not exist yet; fall back to open access (same as empty allowlist).
            logger.LogWarning(ex, "AdminEmailAcls query failed (table may not exist yet). Falling back to open access.");
            return AdminRole.SuperAdmin;
        }
    }
}
