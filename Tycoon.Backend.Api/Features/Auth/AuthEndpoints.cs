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
            var authGroup = app.MapGroup("/auth").WithTags("Authentication");

            authGroup.MapPost("/register", HandleRegistration);
            authGroup.MapPost("/login", HandleLogin);
            authGroup.MapPost("/refresh", HandleTokenRefresh);
            authGroup.MapPost("/logout", HandleLogout).RequireAuthorization();
        }

        private static async Task<IResult> HandleRegistration(
            [FromBody] RegisterRequest request, 
            IAuthService authService, 
            CancellationToken cancellation)
        {
            try
            {
                var registeredUser = await authService.RegisterAsync(
                    request.Email, 
                    request.Password, 
                    request.Handle, 
                    request.Country);
                
                return Results.Created(
                    $"/users/{registeredUser.Id}", 
                    new { 
                        userId = registeredUser.Id, 
                        message = "Registration successful" 
                    });
            }
            catch (InvalidOperationException error)
            {
                return Results.BadRequest(new { 
                    error = "registration_failed", 
                    message = error.Message 
                });
            }
        }

        private static async Task<IResult> HandleLogin(
            [FromBody] LoginRequest request, 
            IAuthService authService, 
            CancellationToken cancellation)
        {
            try
            {
                var authData = await authService.LoginAsync(
                    request.Email, 
                    request.Password, 
                    request.DeviceId);
                
                var loginResult = new LoginResponse(
                    authData.AccessToken,
                    authData.RefreshToken,
                    authData.ExpiresIn,
                    authData.User
                );
                
                return Results.Ok(loginResult);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }

        private static async Task<IResult> HandleTokenRefresh(
            [FromBody] RefreshRequest request, 
            IAuthService authService, 
            CancellationToken cancellation)
        {
            try
            {
                var authData = await authService.RefreshAsync(request.RefreshToken);
                
                var refreshResult = new LoginResponse(
                    authData.AccessToken,
                    authData.RefreshToken,
                    authData.ExpiresIn,
                    authData.User
                );
                
                return Results.Ok(refreshResult);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }

        private static async Task<IResult> HandleLogout(
            [FromBody] LogoutRequest request, 
            HttpContext httpContext, 
            IAuthService authService, 
            CancellationToken cancellation)
        {
            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            
            if (userIdClaim?.Value == null) 
                return Results.Unauthorized();

            var parsedUserId = Guid.Parse(userIdClaim.Value);
            await authService.LogoutAsync(request.DeviceId, parsedUserId);
            
            return Results.NoContent();
        }
    }
}
