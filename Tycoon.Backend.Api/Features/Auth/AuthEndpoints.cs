using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Auth;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.Auth
{
    public static class AuthEndpoints
    {
        public static void Map(WebApplication app)
        {
            var g = app.MapGroup("/auth").WithTags("Authentication");

            g.MapPost("/register", async ([FromBody] RegisterRequest req, IAuthService auth, CancellationToken ct) =>
            {
                try
                {
                    var user = await auth.RegisterAsync(req, ct);
                    return Results.Ok(user);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Conflict(new { error = ex.Message });
                }
            });

            g.MapPost("/login", async ([FromBody] LoginRequest req, IAuthService auth, CancellationToken ct) =>
            {
                try
                {
                    var result = await auth.LoginAsync(req.Email, req.Password, req.DeviceId, ct);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException ex)
                {
                    return Results.Problem(statusCode: 401, detail: ex.Message);
                }
            });

            g.MapPost("/refresh", async ([FromBody] RefreshRequest req, IAuthService auth, CancellationToken ct) =>
            {
                try
                {
                    var result = await auth.RefreshAsync(req.RefreshToken, ct);
                    return Results.Ok(result);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            });

            g.MapPost("/logout", async ([FromBody] LogoutRequest req, IAuthService auth, HttpContext ctx, CancellationToken ct) =>
            {
                var userIdClaim = ctx.User.FindFirst("sub");
                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    await auth.LogoutAsync(userId, req.DeviceId, ct);
                }
                return Results.NoContent();
            }).RequireAuthorization();
        }
    }
}
