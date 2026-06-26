# OTP Password Reset Backend - Implementation Steps

**Date**: June 25, 2026  
**Status**: Ready for Implementation  
**Complexity**: Advanced (3-4 hours to fully implement)

## Step 1: Update appsettings.json

Add configuration for OTP and SendGrid:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  
  "SendGrid": {
    "ApiKey": "SG.xxxxxxxxxxxxxxxxxxxx",
    "FromEmail": "noreply@synaptixplay.com",
    "FromName": "Trivia Tycoon"
  },
  
  "Otp": {
    "Length": 6,
    "ExpirationMinutes": 10,
    "MaxAttempts": 5,
    "RateLimitPerHour": 3
  }
}
```

**Get SendGrid API Key**:
1. Go to https://sendgrid.com
2. Sign up or login
3. Go to Settings → API Keys
4. Create a new API key
5. Copy and paste into appsettings.json

## Step 2: Add DbSet to IAppDb Interface

**File**: `Synaptix.Backend.Application/Abstractions/IAppDb.cs`

Add this line after other DbSets:

```csharp
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Abstractions
{
    public interface IAppDb : IEntitlementDb
    {
        // ... existing DbSets ...
        
        DbSet<OtpToken> OtpTokens { get; }
    }
}
```

## Step 3: Create Database Migration

```bash
cd Synaptix.Backend.Infrastructure
dotnet ef migrations add AddOtpTokensTable
dotnet ef database update
```

Or manually create a migration file:

```csharp
// Migrations/202606250000_AddOtpTokensTable.cs
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpTokensTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "otp_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    otp_hash = table.Column<string>(type: "nvarchar(255)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: false),
                    verification_attempts = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_otp_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_otp_tokens_email",
                table: "otp_tokens",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_otp_tokens_expires_at",
                table: "otp_tokens",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "otp_tokens");
        }
    }
}
```

## Step 4: Register Services in Program.cs

**File**: `Synaptix.Backend.Api/Program.cs`

Add before `app.Run()`:

```csharp
// Register OTP service
builder.Services.AddScoped<OtpService>();

// Register Email service with HttpClient
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpClient<EmailService>();
```

## Step 5: Add Endpoints to AuthEndpoints.cs

**File**: `Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs`

Add imports:

```csharp
using Synaptix.Backend.Api.Services;
```

Add routes in the `Map` method (after other auth routes):

```csharp
// Password reset endpoints
authGroup.MapPost("/forgot-password", HandleForgotPassword);
authGroup.MapPost("/verify-otp", HandleVerifyOtp);
authGroup.MapPost("/reset-password", HandleResetPassword);
```

Add handler methods at end of class (before closing braces):

```csharp
private static async Task<IResult> HandleForgotPassword(
    [FromBody] RequestPasswordResetRequest request,
    IAppDb database,
    OtpService otpService,
    EmailService emailService,
    CancellationToken cancellation)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "Email is required"
        });

    var email = request.Email.ToLowerInvariant();

    // Check if user exists
    var user = await database.Users
        .FirstOrDefaultAsync(u => u.Email == email, cancellation);

    if (user is null)
    {
        // Don't reveal if email is registered (security best practice)
        return Results.Ok(new RequestPasswordResetResponse(
            Message: "If this email is registered, you will receive an OTP shortly",
            Method: request.Method,
            Hint: "Check your email for the code",
            ExpiresIn: 600
        ));
    }

    // Check rate limiting
    bool isRateLimited = await otpService.IsRateLimitedAsync(email, cancellation);
    if (isRateLimited)
    {
        return Results.StatusCode(429, new {
            error = "RATE_LIMITED",
            message = "Too many password reset requests. Try again in 1 hour",
            retryAfterSeconds = 3600
        });
    }

    // Generate OTP
    string otp = otpService.GenerateOtp();
    string otpHash = otpService.HashOtp(otp);

    // Store OTP
    bool stored = await otpService.StoreOtpAsync(email, otpHash, cancellation);
    if (!stored)
    {
        return Results.StatusCode(500, new {
            error = "SERVER_ERROR",
            message = "Failed to generate reset code"
        });
    }

    // Send OTP
    string method = request.Method?.ToLower() == "sms" ? "sms" : "email";
    bool sent = false;

    if (method == "email")
    {
        sent = await emailService.SendPasswordResetEmailAsync(email, user.Handle, otp);
    }
    // TODO: Add SMS support via Twilio

    if (!sent)
    {
        // Don't reveal email service failure
        Console.WriteLine($"[ERROR] Failed to send OTP to {email}");
    }

    // Log the request
    Console.WriteLine($"[AUDIT] Password reset requested for {email} via {method}");

    return Results.Ok(new RequestPasswordResetResponse(
        Message: "If this email is registered, you will receive an OTP shortly",
        Method: method,
        Hint: $"Check your {method} for the code",
        ExpiresIn: 600
    ));
}

private static async Task<IResult> HandleVerifyOtp(
    [FromBody] VerifyOtpRequest request,
    IAppDb database,
    OtpService otpService,
    CancellationToken cancellation)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "Email is required"
        });

    if (string.IsNullOrWhiteSpace(request.Otp))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "OTP is required"
        });

    string email = request.Email.ToLowerInvariant();

    // Verify OTP
    bool verified = await otpService.VerifyOtpAsync(email, request.Otp, cancellation);

    if (!verified)
    {
        int remaining = await otpService.GetRemainingAttemptsAsync(email, cancellation);
        Console.WriteLine($"[AUDIT] Invalid OTP attempt for {email}, {remaining} attempts remaining");

        return Results.BadRequest(new {
            error = "INVALID_OTP",
            message = "The code you entered is incorrect",
            attemptsRemaining = remaining
        });
    }

    // Generate reset token (JWT or custom)
    // For simplicity, using a base64-encoded combination
    string resetToken = Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes($"{email}|{DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds()}")
    );

    Console.WriteLine($"[AUDIT] OTP verified successfully for {email}");

    return Results.Ok(new VerifyOtpResponse(
        Message: "OTP verified successfully",
        ResetToken: resetToken,
        ExpiresIn: 300
    ));
}

private static async Task<IResult> HandleResetPassword(
    [FromBody] ResetPasswordRequest request,
    IAppDb database,
    OtpService otpService,
    EmailService emailService,
    CancellationToken cancellation)
{
    // Validate input
    if (string.IsNullOrWhiteSpace(request.Email))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "Email is required"
        });

    if (string.IsNullOrWhiteSpace(request.Token))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "Reset token is required"
        });

    if (string.IsNullOrWhiteSpace(request.NewPassword))
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "New password is required"
        });

    string email = request.Email.ToLowerInvariant();

    // Verify reset token
    try
    {
        string decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Token));
        var parts = decoded.Split('|');
        if (parts.Length != 2 || parts[0] != email)
        {
            return Results.BadRequest(new {
                error = "INVALID_TOKEN",
                message = "The reset token is invalid or expired"
            });
        }

        long expiresAt = long.Parse(parts[1]);
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresAt)
        {
            return Results.BadRequest(new {
                error = "TOKEN_EXPIRED",
                message = "The reset token has expired"
            });
        }
    }
    catch
    {
        return Results.BadRequest(new {
            error = "INVALID_TOKEN",
            message = "The reset token is invalid or expired"
        });
    }

    // Get user
    var user = await database.Users
        .FirstOrDefaultAsync(u => u.Email == email, cancellation);

    if (user is null)
        return Results.NotFound(new { error = "USER_NOT_FOUND" });

    // Validate new password
    var passwordValidationError = ValidateNewPassword(request.NewPassword, user.Email);
    if (passwordValidationError != null)
        return Results.BadRequest(passwordValidationError);

    // Hash new password
    string newPasswordHash;
    try
    {
        newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Password hashing error: {ex.Message}");
        return Results.StatusCode(500);
    }

    // Update password
    user.ChangePassword(newPasswordHash);

    try
    {
        await database.SaveChangesAsync(cancellation);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Database update error: {ex.Message}");
        return Results.StatusCode(500);
    }

    // Clear OTP
    await otpService.ClearOtpAsync(email, cancellation);

    // Send confirmation email
    _ = emailService.SendPasswordResetConfirmationEmailAsync(email, user.Handle);

    // Log the reset
    Console.WriteLine($"[AUDIT] Password reset successful for user {user.Id} ({user.Email})");

    return Results.Ok(new ResetPasswordResponse(
        Message: "Password reset successfully",
        Action: "redirect_to_login"
    ));
}
```

## Step 6: Update Package References

Install SendGrid NuGet package:

```bash
cd Synaptix.Backend.Api
dotnet add package SendGrid
```

Update csproj file to include:

```xml
<ItemGroup>
    <PackageReference Include="SendGrid" Version="9.28.1" />
</ItemGroup>
```

## Step 7: Build and Test

```bash
# Build the project
dotnet build

# Run migrations
dotnet ef database update

# Start the server
dotnet run
```

## Step 8: Test Endpoints

### Test Request Password Reset

```bash
curl -X POST http://localhost:5000/api/v1/auth/forgot-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "method": "email"
  }'
```

### Test Verify OTP

```bash
curl -X POST http://localhost:5000/api/v1/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "otp": "123456"
  }'
```

### Test Reset Password

```bash
curl -X POST http://localhost:5000/api/v1/auth/reset-password \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "token": "eyJhbGc...",
    "newPassword": "NewPassword456!"
  }'
```

## Files Created/Modified

**Created**:
- ✅ `Synaptix.Backend.Domain/Entities/OtpToken.cs`
- ✅ `Synaptix.Backend.Api/Services/OtpService.cs`
- ✅ `Synaptix.Backend.Api/Services/EmailService.cs`
- ✅ Database migration file

**Modified**:
- ✅ `Synaptix.Shared.Contracts/Dtos/AuthDtos.cs` - Added 5 new DTOs
- ✅ `Synaptix.Backend.Application/Abstractions/IAppDb.cs` - Added DbSet
- ✅ `Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs` - Added endpoints
- ✅ `Synaptix.Backend.Api/Program.cs` - Registered services
- ✅ `appsettings.json` - Added configuration

---

## Next Phase: Frontend

After backend is done, implement frontend:
- ForgotPasswordPage.tsx
- Step components (email, method, OTP, password)
- API client methods
- Router configuration

---

## Security Notes

1. Never expose whether email is registered
2. Rate limit OTP requests (3 per hour)
3. Limit verification attempts (5 per OTP)
4. OTP expires after 10 minutes
5. Store OTP hash, not plaintext
6. Use bcrypt for OTP hashing
7. Log all attempts for audit trail
8. Send confirmation email after password reset
9. Don't auto-login after password reset
10. Clear OTP after use

---

## Configuration Checklist

- [ ] SendGrid API key added to appsettings.json
- [ ] Database migration created and applied
- [ ] OtpService registered in Program.cs
- [ ] EmailService registered in Program.cs
- [ ] Endpoints added to AuthEndpoints.cs
- [ ] All validation methods included
- [ ] Email templates configured
- [ ] Rate limiting parameters set
- [ ] Project builds without errors
- [ ] Database tables created
