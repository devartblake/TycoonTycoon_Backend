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
            var authGroup = app.MapGroup("/auth").WithTags("Authentication").WithOpenApi();

            authGroup.MapPost("/register", HandleRegistration);
            authGroup.MapPost("/signup", HandleSignup);
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

        /// <summary>
        /// Registers a new user account and immediately logs them in, returning auth tokens.
        /// This is the preferred endpoint for mobile apps that want to register + login in one call.
        /// </summary>
        private static async Task<IResult> HandleSignup(
            [FromBody] SignupRequest request,
            IAuthService authService,
            CancellationToken cancellation)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required" });

            if (string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Password is required" });

            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required" });

            if (request.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters" });

            try
            {
                // Extract handle (username) from request
                // Flutter sends "username" but backend uses "handle"
                var handle = request.Username
                    ?? request.Handle
                    ?? request.Email.Split('@')[0]; // Fallback to email prefix

                // Step 1: Register the user
                var registeredUser = await authService.RegisterAsync(
                    email: request.Email,
                    password: request.Password,
                    handle: handle,
                    country: request.Country);

                // Step 2: Immediately log them in to get tokens
                var authData = await authService.LoginAsync(
                    email: request.Email,
                    password: request.Password,
                    deviceId: request.DeviceId);

                // Step 3: Return tokens + user info (same format as login)
                var signupResult = new SignupResponse(
                    AccessToken: authData.AccessToken,
                    RefreshToken: authData.RefreshToken,
                    ExpiresIn: authData.ExpiresIn,
                    UserId: authData.User.Id.ToString(),
                    User: authData.User
                );

                return Results.Ok(signupResult);
            }
            catch (InvalidOperationException error) when (error.Message.Contains("email is already in use"))
            {
                return Results.Conflict(new
                {
                    error = "email_already_exists",
                    message = "This email is already registered"
                });
            }
            catch (InvalidOperationException error) when (error.Message.Contains("handle is not available"))
            {
                return Results.Conflict(new
                {
                    error = "username_taken",
                    message = "This username is already taken"
                });
            }
            catch (InvalidOperationException error)
            {
                return Results.BadRequest(new
                {
                    error = "signup_failed",
                    message = error.Message
                });
            }
            catch (UnauthorizedAccessException)
            {
                // This shouldn't happen since we just created the account,
                // but handle it gracefully just in case
                return Results.Problem(
                    detail: "Account created but auto-login failed",
                    statusCode: 500);
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
