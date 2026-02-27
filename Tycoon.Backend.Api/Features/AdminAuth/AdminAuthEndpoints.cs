using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Api.Contracts;
using Tycoon.Backend.Application.Auth;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminAuth;

public static class AdminAuthEndpoints
{
    private static readonly string[] DefaultPermissions =
    [
        "users:read",
        "users:write",
        "questions:read",
        "questions:write",
        "events:read",
        "events:write"
    ];

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/auth").WithTags("Admin/Auth").WithOpenApi();

        g.MapPost("/login", Login).RequireRateLimiting("admin-auth-login");
        g.MapPost("/refresh", Refresh).RequireRateLimiting("admin-auth-refresh");
        g.MapGet("/me", Me).RequireAuthorization();
    }

    private static async Task<IResult> Login(
        [FromBody] AdminLoginRequest request,
        IAuthService authService,
        IConfiguration configuration,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AdminApiResponses.Error(StatusCodes.Status422UnprocessableEntity, "VALIDATION_ERROR", "Email and password are required.");
        }

        try
        {
            var auth = await authService.AdminLoginAsync(request.Email, request.Password, deviceId: "admin-web");

            if (!IsAdminEmail(request.Email, configuration))
            {
                return AdminApiResponses.Error(StatusCodes.Status403Forbidden, "FORBIDDEN", "Authenticated user is not an admin.");
            }

            var profile = new AdminProfileResponse(
                Id: $"adm_{auth.User.Id:N}",
                Email: auth.User.Email,
                DisplayName: auth.User.Handle,
                Roles: ["admin"],
                Permissions: DefaultPermissions
            );

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
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Invalid credentials.");
        }
    }

    private static async Task<IResult> Refresh([FromBody] RefreshRequest request, IAuthService authService)
    {
        try
        {
            var auth = await authService.AdminRefreshAsync(request.RefreshToken);
            return Results.Ok(new AdminRefreshResponse(auth.AccessToken, auth.ExpiresIn, "Bearer"));
        }
        catch (UnauthorizedAccessException)
        {
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Refresh token is invalid or expired.");
        }
    }

    private static IResult Me(HttpContext httpContext)
    {
        var sub = httpContext.User.FindFirst("sub")?.Value
                  ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var email = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var name = httpContext.User.Identity?.Name ?? email;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return AdminApiResponses.Error(StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Missing authenticated subject.");
        }

        return Results.Ok(new AdminProfileResponse(
            Id: $"adm_{sub}",
            Email: email,
            DisplayName: name,
            Roles: ["admin"],
            Permissions: DefaultPermissions
        ));
    }

    private static bool IsAdminEmail(string email, IConfiguration configuration)
    {
        var allowedEmails = configuration.GetSection("AdminAuth:AllowedEmails").Get<string[]>() ?? [];
        if (allowedEmails.Length == 0)
        {
            return true;
        }

        return allowedEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
    }
}
