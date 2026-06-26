# Backend Password Change Implementation

**Date**: June 25, 2026  
**Scope**: Add password change endpoint to .NET backend  
**Complexity**: Medium (new endpoint, validation, hashing)

## Overview

The backend needs to implement a password change endpoint that:
1. Authenticates the user (requires valid JWT token)
2. Verifies the current password
3. Validates the new password
4. Updates the password in database
5. Returns appropriate error messages

## Step-by-Step Implementation

### Step 1: Add DTOs to AuthDtos.cs

**File**: `Synaptix.Shared.Contracts/Dtos/AuthDtos.cs`

Add these records at the end of the file:

```csharp
/// <summary>
/// Request to change the authenticated user's password
/// </summary>
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

/// <summary>
/// Response from password change endpoint
/// </summary>
public record ChangePasswordResponse(
    string Message,
    bool SessionCleared,
    bool RequiresReauth
);
```

### Step 2: Add Password Change Endpoint

**File**: `Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs`

1. Add the route mapping in the `Map` method (after logout):
```csharp
authGroup.MapPost("/change-password", HandleChangePassword).RequireAuthorization();
```

2. Add the handler method at the end of the class:
```csharp
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
        // Use your password verification method
        // This depends on how passwords are currently hashed
        // For BCrypt:
        isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
            request.CurrentPassword, 
            user.PasswordHash);
    }
    catch (Exception ex)
    {
        // Log the error
        Console.WriteLine($"Password verification error: {ex.Message}");
        return Results.BadRequest(new { 
            error = "VERIFICATION_ERROR",
            message = "Failed to verify password"
        });
    }

    if (!isCurrentPasswordValid)
    {
        // Don't reveal whether email exists or password was wrong
        return Results.Unauthorized(new { 
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
        Console.WriteLine($"Password hashing error: {ex.Message}");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    // 8. Update password in database
    user.PasswordHash = newPasswordHash;
    user.UpdatedAtUtc = DateTimeOffset.UtcNow;
    
    try
    {
        await database.SaveChangesAsync(cancellation);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database update error: {ex.Message}");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }

    // 9. Optional: Clear all refresh tokens to force re-login
    // This is more secure but has worse UX
    // For now, we'll keep the current session active

    // 10. Log the password change event (for audit)
    // TODO: Implement audit logging
    Console.WriteLine($"[AUDIT] User {user.Id} changed their password at {DateTimeOffset.UtcNow}");

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

    // Check against common patterns (optional)
    var commonPasswords = new[] {
        "password", "12345678", "qwerty", "123456789", "123123123",
        "abc123", "password123", "admin", "letmein", "welcome"
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
```

### Step 3: Add Required Imports

Make sure these using statements are in AuthEndpoints.cs:

```csharp
using Microsoft.AspNetCore.Mvc;
using Synaptix.Shared.Contracts.Dtos;
using BCrypt.Net; // May need NuGet: BCrypt.Net-Core or BCrypt.Net-Next
```

### Step 4: Verify Database Schema

Ensure the User table has these columns:
- `PasswordHash` (string/nvarchar)
- `UpdatedAtUtc` (DateTimeOffset)

Most likely already exist, but verify in your database.

---

## Testing the Endpoint

### Option 1: Using curl

```bash
# 1. Login first to get a token
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "OldPassword123!",
    "deviceId": "test-device-001"
  }'

# Note: Copy the accessToken from response

# 2. Use token to change password
curl -X POST http://localhost:5000/api/v1/auth/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <paste-token-here>" \
  -d '{
    "currentPassword": "OldPassword123!",
    "newPassword": "NewPassword456!"
  }'

# Expected response (200 OK):
# {
#   "message": "Password changed successfully",
#   "sessionCleared": false,
#   "requiresReauth": false
# }
```

### Option 2: Using Postman

1. **GET** `/auth/login` first
2. Copy the `accessToken` value
3. Click **Authorization** tab
4. Select "Bearer Token"
5. Paste the token
6. **POST** `/auth/change-password`
7. Body (JSON):
   ```json
   {
     "currentPassword": "OldPassword123!",
     "newPassword": "NewPassword456!"
   }
   ```

## Error Scenarios to Test

| Scenario | Expected Response | Status |
|----------|-------------------|--------|
| No auth token | `"UNAUTHORIZED"` | 401 |
| Invalid auth token | `"UNAUTHORIZED"` | 401 |
| Wrong current password | `"INVALID_CREDENTIALS"` | 401 |
| New password too short | `"VALIDATION_ERROR"` with `minLength` | 400 |
| New password no uppercase | `"VALIDATION_ERROR"` with `uppercase` | 400 |
| New password = current password | `"VALIDATION_ERROR"` | 400 |
| Success | `"Password changed successfully"` | 200 |

---

## Security Checklist

- [ ] Password hashing uses bcrypt with workFactor: 12
- [ ] Current password is verified before allowing change
- [ ] New password validation checks all requirements
- [ ] New password cannot be same as current
- [ ] Error messages don't leak information (no "email not found")
- [ ] Endpoint requires authentication
- [ ] Rate limiting is applied (recommend 5 attempts/hour)
- [ ] Password change events are logged
- [ ] HTTPS is enforced in production

---

## Optional Enhancements

### 1. Clear All Sessions (More Secure)

After successful password change, invalidate all refresh tokens:

```csharp
// After SaveChangesAsync, before returning success
await database.RefreshTokens
    .Where(rt => rt.UserId == userId)
    .ExecuteDeleteAsync(cancellation);

// Then return:
return Results.Ok(new ChangePasswordResponse(
    Message: "Password changed successfully. You may need to login again on other devices.",
    SessionCleared: true,
    RequiresReauth: true
));
```

### 2. Email Notification

Send notification email after password change:

```csharp
// After successful password update
await emailService.SendPasswordChangedEmailAsync(user.Email, user.Handle);
```

### 3. Password History

Prevent reusing old passwords:

```csharp
// Store previous password hashes
var passwordHistory = await database.PasswordHistories
    .Where(ph => ph.UserId == userId && ph.CreatedAt > DateTime.Now.AddYears(-1))
    .ToListAsync();

foreach (var oldHash in passwordHistory.Select(ph => ph.PasswordHash))
{
    if (BCrypt.Net.BCrypt.Verify(request.NewPassword, oldHash))
        return Results.BadRequest(new { 
            error = "VALIDATION_ERROR",
            message = "You cannot reuse a password from the last year"
        });
}
```

### 4. Two-Factor Auth Verification

Require 2FA for password change (if user has it enabled):

```csharp
// Check if user has 2FA enabled
if (user.TwoFactorEnabled)
{
    // Require 2FA verification before allowing password change
    // This would be a separate request/response flow
}
```

---

## Deployment Considerations

### Development
- Set workFactor to 12 (default, balanced)
- Log password changes to console

### Production
- Set workFactor to 14+ (slower, more secure)
- Log password changes to secure audit log
- Enable rate limiting (5 attempts/hour)
- Monitor for suspicious password change patterns
- Send email notifications

---

## Related Files

- Frontend: `src/features/settings/components/ChangePasswordForm.tsx`
- Frontend API: `src/core/api/client.ts`
- Frontend Integration: `src/features/dashboard/pages/SettingsPage.tsx`
- This guide: `BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md`

---

## Verification Steps

After implementing:

1. ✅ Create new DTOs in AuthDtos.cs
2. ✅ Add endpoint route in AuthEndpoints.cs
3. ✅ Add handler method in AuthEndpoints.cs
4. ✅ Add validation method
5. ✅ Test with curl/Postman
6. ✅ Test error scenarios
7. ✅ Test successful password change
8. ✅ Verify new password works for login
9. ✅ Check audit logs
10. ✅ Deploy to staging

