using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Api.Contracts;
using Synaptix.Backend.Api.Security;
using Synaptix.Backend.Api.Services;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Features.Auth
{
    public static class AuthEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var authGroup = app.MapGroup("/auth").WithTags("Authentication");

            authGroup.MapPost("/register", HandleRegistration);
            authGroup.MapPost("/signup", HandleSignup);
            authGroup.MapPost("/login", HandleLogin);
            authGroup.MapPost("/refresh", HandleTokenRefresh).RequireSecureChannel();
            authGroup.MapPost("/logout", HandleLogout).RequireAuthorization();

            // Device-first guest funnel: play immediately, register later.
            authGroup.MapPost("/device/bootstrap", HandleDeviceBootstrap);
            authGroup.MapPost("/account/upgrade", HandleAccountUpgrade).RequireAuthorization();

            // Password management
            authGroup.MapPost("/change-password", HandleChangePassword).RequireAuthorization();

            // Password reset (OTP-based)
            authGroup.MapPost("/forgot-password", HandleForgotPassword);
            authGroup.MapPost("/verify-otp", HandleVerifyOtp);
            authGroup.MapPost("/reset-password", HandleResetPassword);

            // Native game-platform and OAuth sign-in. These require provider
            // credentials and server-side signature/token verification that is not
            // configured yet; they fail closed (501) rather than 404 so the client
            // surfaces a clear "not available" state instead of a silent dead-end,
            // and so no unverified identity is ever accepted (no auth bypass).
            authGroup.MapPost("/mobile-game-login", HandleGamePlatformNotConfigured);
            authGroup.MapPost("/link-game-account", HandleGamePlatformNotConfigured).RequireAuthorization();
            authGroup.MapGet("/oauth/{provider}", HandleOAuthNotConfigured);
        }

        private static async Task<IResult> HandleDeviceBootstrap(
            [FromBody] DeviceBootstrapRequest request,
            IAuthService authService,
            CancellationToken cancellation)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required" });

            try
            {
                var authData = await authService.BootstrapDeviceAsync(request.DeviceId, request.DisplayName);
                return Results.Ok(new LoginResponse(
                    authData.AccessToken,
                    authData.RefreshToken,
                    authData.ExpiresIn,
                    authData.User));
            }
            catch (InvalidOperationException error)
            {
                return Results.BadRequest(new { error = "bootstrap_failed", message = error.Message });
            }
        }

        private static async Task<IResult> HandleAccountUpgrade(
            [FromBody] AccountUpgradeRequest request,
            HttpContext httpContext,
            IAuthService authService,
            CancellationToken cancellation)
        {
            var subject = httpContext.User.FindFirst("sub")?.Value
                          ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (subject is null || !Guid.TryParse(subject, out var userId))
                return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required" });
            if (string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Password is required" });
            if (request.Password.Length < 8)
                return Results.BadRequest(new { error = "Password must be at least 8 characters" });
            if (string.IsNullOrWhiteSpace(request.DeviceId))
                return Results.BadRequest(new { error = "DeviceId is required" });

            try
            {
                var handle = request.Username ?? request.Handle;
                var authData = await authService.UpgradeAccountAsync(
                    userId, request.Email, request.Password, request.DeviceId, handle, request.Country);

                return Results.Ok(new LoginResponse(
                    authData.AccessToken,
                    authData.RefreshToken,
                    authData.ExpiresIn,
                    authData.User));
            }
            catch (InvalidOperationException error) when (error.Message.Contains("already in use"))
            {
                return Results.Conflict(new { error = "email_already_exists", message = error.Message });
            }
            catch (InvalidOperationException error) when (error.Message.Contains("not available"))
            {
                return Results.Conflict(new { error = "username_taken", message = error.Message });
            }
            catch (InvalidOperationException error) when (error.Message.Contains("already registered"))
            {
                return Results.Conflict(new { error = "already_registered", message = error.Message });
            }
            catch (InvalidOperationException error)
            {
                return Results.BadRequest(new { error = "upgrade_failed", message = error.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        }

        private static IResult HandleGamePlatformNotConfigured()
            => ApiResponses.Error(
                StatusCodes.Status501NotImplemented,
                "not_implemented",
                "Native game-platform sign-in is not configured on this server. " +
                "It requires Apple Game Center / Google Play Games verification to be enabled.");

        private static IResult HandleOAuthNotConfigured(string provider)
            => ApiResponses.Error(
                StatusCodes.Status501NotImplemented,
                "not_implemented",
                $"OAuth sign-in for '{provider}' is not configured on this server.");

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

        private static async Task<IResult> HandleChangePassword(
            [FromBody] ChangePasswordRequest request,
            HttpContext httpContext,
            IAppDb database,
            CancellationToken cancellation)
        {
            // 1. Get user ID from JWT token
            if (!TryGetUserId(httpContext, out var userId))
                return Results.Unauthorized();

            // 2. Get user from database
            var user = await database.Users
                .FirstOrDefaultAsync(u => u.Id == userId, cancellation);

            if (user is null)
                return Results.NotFound(new { error = "USER_NOT_FOUND", message = "User not found" });

            // 3. Validate inputs
            if (string.IsNullOrWhiteSpace(request.CurrentPassword))
                return Results.BadRequest(new {
                    error = "VALIDATION_ERROR",
                    message = "Current password is required",
                    field = "currentPassword"
                });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new {
                    error = "VALIDATION_ERROR",
                    message = "New password is required",
                    field = "newPassword"
                });

            // 4. Verify current password
            bool isCurrentPasswordValid = false;
            try
            {
                isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                    request.CurrentPassword,
                    user.PasswordHash);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Password verification error: {ex.Message}");
                return Results.BadRequest(new {
                    error = "VERIFICATION_ERROR",
                    message = "Failed to verify password"
                });
            }

            if (!isCurrentPasswordValid)
            {
                return Results.BadRequest(new {
                    error = "INVALID_CREDENTIALS",
                    message = "Current password is incorrect"
                });
            }

            // 5. Validate new password requirements
            var passwordValidationError = ValidateNewPassword(request.NewPassword, user.Email);
            if (passwordValidationError != null)
                return Results.BadRequest(passwordValidationError);

            // 6. Verify new password is different from current
            bool isSameAsOld = false;
            try
            {
                isSameAsOld = BCrypt.Net.BCrypt.Verify(
                    request.NewPassword,
                    user.PasswordHash);
            }
            catch { /* Password is different */ }

            if (isSameAsOld)
            {
                return Results.BadRequest(new {
                    error = "VALIDATION_ERROR",
                    message = "New password must be different from your current password",
                    field = "newPassword"
                });
            }

            // 7. Hash new password
            string newPasswordHash;
            try
            {
                newPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    request.NewPassword,
                    workFactor: 12);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Password hashing error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            // 8. Update password in database
            user.ChangePassword(newPasswordHash);

            try
            {
                await database.SaveChangesAsync(cancellation);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Database update error: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            // 9. Log the password change event (audit)
            Console.WriteLine($"[AUDIT] User {user.Id} ({user.Email}) changed their password at {DateTimeOffset.UtcNow:O}");

            return Results.Ok(new ChangePasswordResponse(
                Message: "Password changed successfully",
                SessionCleared: false,
                RequiresReauth: false
            ));
        }

        private static object? ValidateNewPassword(string password, string userEmail)
        {
            // Minimum requirements
            if (string.IsNullOrWhiteSpace(password))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password cannot be empty",
                    field = "newPassword"
                };

            if (password.Length < 8)
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password must be at least 8 characters",
                    field = "newPassword",
                    requirement = "minLength"
                };

            if (!password.Any(char.IsUpper))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password must contain at least one uppercase letter",
                    field = "newPassword",
                    requirement = "uppercase"
                };

            if (!password.Any(char.IsLower))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password must contain at least one lowercase letter",
                    field = "newPassword",
                    requirement = "lowercase"
                };

            if (!password.Any(char.IsDigit))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password must contain at least one number",
                    field = "newPassword",
                    requirement = "number"
                };

            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password must contain at least one special character",
                    field = "newPassword",
                    requirement = "special"
                };

            // Check against common passwords
            var commonPasswords = new[] {
                "password", "12345678", "qwerty", "123456789", "123123123",
                "abc123", "password123", "admin", "letmein", "welcome", "passw0rd"
            };

            if (commonPasswords.Contains(password.ToLower()))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "This password is too common. Please choose a stronger password",
                    field = "newPassword"
                };

            // Check password doesn't contain email
            if (!string.IsNullOrEmpty(userEmail) && password.Contains(userEmail, StringComparison.OrdinalIgnoreCase))
                return new {
                    error = "VALIDATION_ERROR",
                    message = "Password cannot contain your email address",
                    field = "newPassword"
                };

            return null; // Valid
        }

        private static bool TryGetUserId(HttpContext httpContext, out Guid userId)
        {
            userId = Guid.Empty;
            var claim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)
                        ?? httpContext.User.FindFirst("sub");
            return claim is not null && Guid.TryParse(claim.Value, out userId) && userId != Guid.Empty;
        }

        /// <summary>
        /// Handles password reset request (step 1: send OTP)
        /// POST /auth/forgot-password
        /// </summary>
        private static async Task<IResult> HandleForgotPassword(
            [FromBody] RequestPasswordResetRequest request,
            IAppDb database,
            OtpService otpService,
            EmailService emailService,
            CancellationToken cancellation)
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required" });

            var email = request.Email.ToLowerInvariant();

            // Uniform response to avoid revealing whether the email is registered.
            var accepted = Results.Ok(new RequestPasswordResetResponse(
                Message: "If the email is registered, an OTP has been sent",
                Method: request.Method ?? "email",
                Hint: $"Sent to {email.Substring(0, 3)}***{email.Substring(email.LastIndexOf('@'))}",
                ExpiresIn: 600  // 10 minutes
            ));

            // Check if user exists — but never disclose the result to the caller.
            var user = await database.Users
                .FirstOrDefaultAsync(u => u.Email == email, cancellation);

            if (user is null)
                return accepted;

            // Check rate limiting (still return the uniform response, not a 429 that leaks existence)
            if (await otpService.IsRateLimitedAsync(email, cancellation))
                return accepted;

            try
            {
                // Generate OTP
                var otp = otpService.GenerateOtp();
                var otpHash = otpService.HashOtp(otp);

                // Store OTP in database
                var stored = await otpService.StoreOtpAsync(email, otpHash, cancellation);
                if (!stored)
                    return Results.StatusCode(StatusCodes.Status500InternalServerError);

                // Send email. A send failure is logged but not surfaced, so the response
                // stays uniform and the flow does not hard-fail when email is unconfigured.
                var sent = await emailService.SendPasswordResetEmailAsync(
                    email,
                    user.Handle ?? email.Split('@')[0],
                    otp);

                if (!sent)
                    Console.WriteLine($"[ERROR] Failed to send OTP email to {email}");
                else
                    Console.WriteLine($"[AUDIT] OTP sent to {email} at {DateTimeOffset.UtcNow:O}");

                return accepted;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to send OTP: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles OTP verification (step 2: verify OTP and get reset token)
        /// POST /auth/verify-otp
        /// </summary>
        private static async Task<IResult> HandleVerifyOtp(
            [FromBody] VerifyOtpRequest request,
            IAppDb database,
            OtpService otpService,
            CancellationToken cancellation)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required" });

            if (string.IsNullOrWhiteSpace(request.Otp))
                return Results.BadRequest(new { error = "OTP is required" });

            var email = request.Email.ToLowerInvariant();

            try
            {
                // Verify OTP
                var isValid = await otpService.VerifyOtpAsync(email, request.Otp, cancellation);

                if (!isValid)
                {
                    var attemptsRemaining = await otpService.GetRemainingAttemptsAsync(email, cancellation);
                    return ApiResponses.Error(
                        StatusCodes.Status401Unauthorized,
                        "invalid_otp",
                        $"Invalid or expired OTP. {attemptsRemaining} attempts remaining");
                }

                // Resolve the user the OTP belongs to so the reset token can be bound to it.
                var user = await database.Users
                    .FirstOrDefaultAsync(u => u.Email == email, cancellation);

                if (user is null)
                    return ApiResponses.Error(
                        StatusCodes.Status401Unauthorized,
                        "invalid_otp",
                        "Invalid or expired OTP.");

                // Revoke any outstanding unused reset tokens for this user.
                var outstanding = await database.PasswordResetTokens
                    .Where(t => t.UserId == user.Id && !t.Used)
                    .ToListAsync(cancellation);
                foreach (var existing in outstanding)
                    existing.MarkAsUsed();

                // Generate and persist a single-use reset token (valid for 5 minutes),
                // bound to the user with an expiry — validated on reset by DB lookup.
                var resetTokenValue = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
                var resetToken = new PasswordResetToken(
                    user.Id,
                    resetTokenValue,
                    DateTimeOffset.UtcNow.AddMinutes(5));
                database.PasswordResetTokens.Add(resetToken);
                await database.SaveChangesAsync(cancellation);

                // Log
                Console.WriteLine($"[AUDIT] OTP verified for {email} at {DateTimeOffset.UtcNow:O}");

                return Results.Ok(new VerifyOtpResponse(
                    Message: "OTP verified successfully",
                    ResetToken: $"{email}:{resetTokenValue}",  // Email:Token format; token portion is validated against the DB
                    ExpiresIn: 300  // 5 minutes
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] OTP verification failed: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Handles password reset (step 3: reset password with token)
        /// POST /auth/reset-password
        /// </summary>
        private static async Task<IResult> HandleResetPassword(
            [FromBody] ResetPasswordRequest request,
            IAppDb database,
            OtpService otpService,
            EmailService emailService,
            CancellationToken cancellation)
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(request.Email))
                return Results.BadRequest(new { error = "Email is required" });

            if (string.IsNullOrWhiteSpace(request.Token))
                return Results.BadRequest(new { error = "Reset token is required" });

            if (string.IsNullOrWhiteSpace(request.NewPassword))
                return Results.BadRequest(new { error = "New password is required" });

            var email = request.Email.ToLowerInvariant();

            // Reset token is returned in "email:token" format; the token portion is the
            // opaque value persisted at OTP-verification time.
            var separatorIndex = request.Token.IndexOf(':');
            if (separatorIndex <= 0
                || !request.Token.AsSpan(0, separatorIndex).SequenceEqual(email))
                return ApiResponses.Error(
                    StatusCodes.Status401Unauthorized,
                    "invalid_token",
                    "Invalid reset token");

            var tokenValue = request.Token[(separatorIndex + 1)..];

            try
            {
                // Get user
                var user = await database.Users
                    .FirstOrDefaultAsync(u => u.Email == email, cancellation);

                // Do not reveal whether the email exists — a bad email yields the same
                // response as a bad/expired token.
                if (user is null)
                    return ApiResponses.Error(
                        StatusCodes.Status401Unauthorized,
                        "invalid_token",
                        "Invalid or expired reset token");

                // Validate the reset token: must exist, belong to this user, be unused and unexpired.
                var resetToken = await database.PasswordResetTokens
                    .FirstOrDefaultAsync(
                        t => t.Token == tokenValue && t.UserId == user.Id && !t.Used,
                        cancellation);

                if (resetToken is null || !resetToken.IsValid())
                    return ApiResponses.Error(
                        StatusCodes.Status401Unauthorized,
                        "invalid_token",
                        "Invalid or expired reset token");

                // Validate new password
                var passwordValidationError = ValidateNewPassword(request.NewPassword, email);
                if (passwordValidationError != null)
                    return Results.BadRequest(passwordValidationError);

                // Hash new password
                var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(
                    request.NewPassword,
                    workFactor: 12);

                // Update password and consume the reset token (single use).
                user.ChangePassword(newPasswordHash);
                resetToken.MarkAsUsed();
                await database.SaveChangesAsync(cancellation);

                // Clear OTP tokens for this email
                await otpService.ClearOtpAsync(email, cancellation);

                // Send confirmation email
                await emailService.SendPasswordResetConfirmationEmailAsync(
                    email,
                    user.Handle ?? email.Split('@')[0]);

                // Log
                Console.WriteLine($"[AUDIT] Password reset for {email} at {DateTimeOffset.UtcNow:O}");

                return Results.Ok(new ResetPasswordResponse(
                    Message: "Password reset successfully",
                    Action: "redirect_to_login"
                ));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Password reset failed: {ex.Message}");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
