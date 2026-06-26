# Password Change Feature - Implementation Guide

**Date**: June 25, 2026  
**Scope**: Complete implementation from backend to frontend  
**Complexity**: Medium (requires backend changes + frontend form + validation)

## Table of Contents

1. [User Flow](#user-flow)
2. [Ideal Architecture](#ideal-architecture)
3. [Security Best Practices](#security-best-practices)
4. [Implementation Plan](#implementation-plan)
5. [API Contract](#api-contract)
6. [Code Examples](#code-examples)

---

## User Flow

### Typical Password Change Flow

```
User in App
  ↓
1. Navigate to Settings page
  ↓
2. Find "Security" or "Change Password" section
  ↓
3. Click "Change Password" button/link
  ↓
4. Modal/Form appears with fields:
   - Current password (required)
   - New password (required, 8+ chars)
   - Confirm new password (required, must match)
  ↓
5. User enters current password
   - Validates: password entered
  ↓
6. User enters new password
   - Validates: 8+ chars, includes uppercase, number, special char (optional)
   - Shows password strength indicator
  ↓
7. User confirms new password
   - Validates: matches new password field
  ↓
8. User clicks "Change Password"
  ↓
9. Frontend sends: POST /auth/change-password
   {
     "currentPassword": "user's_current_password",
     "newPassword": "user's_new_password"
   }
  ↓
10. Backend validation:
    - User is authenticated (has valid token)
    - Current password matches user's stored hash
    - New password meets requirements
    - New password is different from current password
  ↓
11. Backend action (if all valid):
    - Hash new password with bcrypt
    - Update user's password hash in database
    - Clear all refresh tokens (force re-login on all devices)
    - Log password change event for audit
  ↓
12. Backend response: 200 OK
  ↓
13. Frontend shows success message
    - "Password changed successfully"
    - "You may need to login again on other devices"
  ↓
14. Optional: Auto-logout user (security best practice)
    - Or keep them logged in but require re-auth for sensitive ops
  ↓
15. User redirected to Settings page / Dashboard
```

---

## Ideal Architecture

### Layer 1: Database

**User Table** (already exists):
```sql
Users
  ├─ Id (Guid) ✅
  ├─ Email (string) ✅
  ├─ PasswordHash (string) ✅ -- bcrypt hash
  ├─ PasswordSalt (string) ✅ -- for bcrypt
  ├─ CreatedAt (DateTime) ✅
  └─ UpdatedAt (DateTime) ✅
```

### Layer 2: Backend API

**New Endpoint**:
```
POST /auth/change-password
Authorization: Bearer {token}
Content-Type: application/json

{
  "currentPassword": "user's_current_password",
  "newPassword": "user's_new_password"
}
```

**Authentication**: Required (JWT token)  
**Rate Limiting**: Yes, 5 attempts per hour  
**Response**: 200 OK or 400/401/409

### Layer 3: Frontend

**Components**:
1. `ChangePasswordForm.tsx` - Form component
2. Integration into `SettingsPage.tsx`
3. `apiClient.changePassword()` method

**Validation**:
- Current password: required, not empty
- New password: 8+ chars, strong password
- Confirm password: matches new password

**Error Handling**:
- 400 Bad Request: validation error
- 401 Unauthorized: current password incorrect
- 409 Conflict: password already used (optional)
- Network errors: offline handling

---

## Security Best Practices

### 1. Password Requirements

**Minimum**:
- [ ] At least 8 characters
- [ ] At least one uppercase letter (A-Z)
- [ ] At least one lowercase letter (a-z)
- [ ] At least one number (0-9)
- [ ] At least one special character (!@#$%^&*)

**Optional Enhancements**:
- [ ] Check against common password lists
- [ ] Check against user's email/username
- [ ] Check against previously used passwords

### 2. Backend Validation

```csharp
✅ ALWAYS validate on backend
- Never trust client-side validation alone
- Verify current password hash matches
- Verify new password meets requirements
- Verify new password ≠ current password
- Verify user is authenticated
```

### 3. Password Hashing

```csharp
// ❌ DON'T: Plain text or weak hashing
password = "plaintext123"  // ❌ NEVER
hash = MD5(password)       // ❌ NEVER

// ✅ DO: Use bcrypt
hash = BCrypt.HashPassword(password, workFactor: 12)
verify = BCrypt.Verify(userInput, hash)
```

### 4. Token Management

**After password change**:
```
✅ Option 1: Clear all sessions (most secure)
  - Invalidate all refresh tokens
  - Force user to re-login everywhere
  - Prevents compromised tokens from being used

✅ Option 2: Keep current session (better UX)
  - Keep current session active
  - Invalidate other sessions
  - User stays logged in on current device
  - But must login again on other devices

❌ Option 3: Keep all sessions (not secure)
  - Old tokens still valid
  - Compromised token can still be used
  - Not recommended
```

### 5. Audit Logging

```csharp
Log these events:
✅ Password change attempt (success/failure)
✅ Account associated with password change
✅ IP address of request
✅ Timestamp
✅ Whether sessions were cleared

// For suspicious activity detection:
- Multiple failed password change attempts
- Password changed at unusual time
- Multiple password changes in short time
```

### 6. Additional Security

- [ ] **HTTPS Only**: All password endpoints must use HTTPS
- [ ] **Rate Limiting**: Max 5 attempts per hour per user
- [ ] **CSRF Protection**: POST endpoints should be CSRF protected
- [ ] **Email Notification**: Notify user if password changed from new device
- [ ] **Two-Factor Auth**: Require 2FA for password change (optional enhancement)

---

## Implementation Plan

### Phase 1: Backend Implementation

**Files to Create/Modify**:

1. **Synaptix.Shared.Contracts/Dtos/AuthDtos.cs**
   - Add `ChangePasswordRequest`
   - Add `ChangePasswordResponse`

2. **Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs**
   - Add endpoint: `POST /auth/change-password`
   - Add validation logic
   - Add password hashing/verification

3. **Synaptix.Backend.Application/Auth/AuthService.cs**
   - Add method: `ChangePasswordAsync(userId, currentPassword, newPassword)`
   - Hash new password
   - Update database
   - Clear refresh tokens (optional)

### Phase 2: Frontend API Client

**File**: `src/core/api/client.ts`

Add method:
```typescript
async changePassword(currentPassword: string, newPassword: string) {
  const response = await this.post('/auth/change-password', {
    currentPassword,
    newPassword,
  });
  return response.data;
}
```

### Phase 3: Frontend Form Component

**File**: `src/features/settings/components/ChangePasswordForm.tsx`

Features:
- [ ] Current password field (password input)
- [ ] New password field (password input)
- [ ] Confirm password field (password input)
- [ ] Password strength indicator
- [ ] Real-time validation
- [ ] Error messages
- [ ] Loading state during submission
- [ ] Success message

### Phase 4: Integration

**File**: `src/features/dashboard/pages/SettingsPage.tsx`

- [ ] Import ChangePasswordForm
- [ ] Add section "Security"
- [ ] Add ChangePasswordForm component
- [ ] Handle success/error responses

---

## API Contract

### Request

```http
POST /api/v1/auth/change-password
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "currentPassword": "MyOldPassword123!",
  "newPassword": "MyNewPassword456!"
}
```

### Response: Success (200 OK)

```json
{
  "message": "Password changed successfully",
  "sessionCleared": true,
  "requiresReauth": false
}
```

### Response: Validation Error (400 Bad Request)

```json
{
  "error": "VALIDATION_ERROR",
  "message": "New password must be at least 8 characters",
  "field": "newPassword",
  "validationRules": {
    "minLength": 8,
    "requireUppercase": true,
    "requireLowercase": true,
    "requireNumbers": true,
    "requireSpecialChars": true
  }
}
```

### Response: Wrong Current Password (401 Unauthorized)

```json
{
  "error": "INVALID_CREDENTIALS",
  "message": "Current password is incorrect",
  "attemptsRemaining": 4
}
```

### Response: Rate Limited (429 Too Many Requests)

```json
{
  "error": "RATE_LIMITED",
  "message": "Too many password change attempts. Try again in 1 hour",
  "retryAfterSeconds": 3600
}
```

---

## Code Examples

### Backend: ChangePasswordRequest DTO

```csharp
namespace Synaptix.Shared.Contracts.Dtos
{
    public record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword
    );

    public record ChangePasswordResponse(
        string Message,
        bool SessionCleared,
        bool RequiresReauth
    );
}
```

### Backend: Endpoint Implementation

```csharp
private static async Task<IResult> HandleChangePassword(
    [FromBody] ChangePasswordRequest request,
    HttpContext httpContext,
    IAuthService authService,
    IAppDb database,
    CancellationToken cancellation)
{
    // 1. Get user ID from token
    if (!TryGetUserId(httpContext, out var userId))
        return Results.Unauthorized();

    // 2. Get user from database
    var user = await database.Users
        .FirstOrDefaultAsync(u => u.Id == userId, cancellation);
    
    if (user is null)
        return Results.NotFound();

    // 3. Verify current password
    if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
    {
        // Log failed attempt for security
        return Results.Unauthorized(new { 
            error = "INVALID_CREDENTIALS",
            message = "Current password is incorrect"
        });
    }

    // 4. Validate new password
    var validationError = ValidatePassword(request.NewPassword);
    if (validationError != null)
        return Results.BadRequest(validationError);

    // 5. Verify new password is different from current
    if (BCrypt.Net.BCrypt.Verify(request.NewPassword, user.PasswordHash))
    {
        return Results.BadRequest(new {
            error = "VALIDATION_ERROR",
            message = "New password must be different from current password"
        });
    }

    // 6. Hash new password
    var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

    // 7. Update password in database
    user.PasswordHash = newHash;
    user.UpdatedAt = DateTimeOffset.UtcNow;
    
    await database.SaveChangesAsync(cancellation);

    // 8. OPTIONAL: Clear all refresh tokens to force re-login
    // This is more secure but has worse UX
    // await authService.ClearAllRefreshTokensAsync(userId);

    return Results.Ok(new ChangePasswordResponse(
        Message: "Password changed successfully",
        SessionCleared: false,
        RequiresReauth: false
    ));
}
```

### Frontend: ChangePasswordForm Component

```typescript
import { useState } from 'react';
import { apiClient } from '@core/api/client';
import { Lock, Eye, EyeOff, CheckCircle, AlertCircle } from 'lucide-react';

export function ChangePasswordForm() {
  const [formData, setFormData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);
  const [showPasswords, setShowPasswords] = useState({
    current: false,
    new: false,
    confirm: false,
  });

  const validatePassword = (password: string) => {
    const requirements = {
      minLength: password.length >= 8,
      hasUppercase: /[A-Z]/.test(password),
      hasLowercase: /[a-z]/.test(password),
      hasNumber: /[0-9]/.test(password),
      hasSpecial: /[!@#$%^&*(),.?":{}|<>]/.test(password),
    };
    return requirements;
  };

  const passwordRequirements = validatePassword(formData.newPassword);
  const passwordValid = Object.values(passwordRequirements).every(Boolean);
  const passwordsMatch = formData.newPassword === formData.confirmPassword;
  const formValid = formData.currentPassword && passwordValid && passwordsMatch;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!formValid) {
      setError('Please fix all errors above');
      return;
    }

    setIsLoading(true);
    try {
      await apiClient.changePassword(
        formData.currentPassword,
        formData.newPassword
      );

      setSuccess(true);
      setFormData({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });

      // Show success message for 3 seconds
      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      const errorMessage =
        err.response?.data?.message ||
        err.message ||
        'Failed to change password';
      setError(errorMessage);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-4 max-w-md">
      <h3 className="text-lg font-semibold text-white">Change Password</h3>

      {error && (
        <div className="p-3 bg-red-900/20 border border-red-700 rounded-lg flex gap-2">
          <AlertCircle size={18} className="text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-red-300 text-sm">{error}</p>
        </div>
      )}

      {success && (
        <div className="p-3 bg-green-900/20 border border-green-700 rounded-lg flex gap-2">
          <CheckCircle size={18} className="text-green-400 flex-shrink-0 mt-0.5" />
          <p className="text-green-300 text-sm">Password changed successfully!</p>
        </div>
      )}

      {/* Current Password */}
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Current Password
        </label>
        <div className="relative">
          <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
          <input
            type={showPasswords.current ? 'text' : 'password'}
            value={formData.currentPassword}
            onChange={(e) =>
              setFormData({ ...formData, currentPassword: e.target.value })
            }
            className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white"
            required
            disabled={isLoading}
          />
          <button
            type="button"
            onClick={() =>
              setShowPasswords({
                ...showPasswords,
                current: !showPasswords.current,
              })
            }
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500"
          >
            {showPasswords.current ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>
      </div>

      {/* New Password */}
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          New Password
        </label>
        <div className="relative">
          <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
          <input
            type={showPasswords.new ? 'text' : 'password'}
            value={formData.newPassword}
            onChange={(e) =>
              setFormData({ ...formData, newPassword: e.target.value })
            }
            className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white"
            required
            disabled={isLoading}
          />
          <button
            type="button"
            onClick={() =>
              setShowPasswords({ ...showPasswords, new: !showPasswords.new })
            }
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500"
          >
            {showPasswords.new ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>

        {/* Password Strength */}
        {formData.newPassword && (
          <div className="mt-3 space-y-1 text-xs">
            {[
              { label: '8+ characters', valid: passwordRequirements.minLength },
              { label: 'Uppercase letter', valid: passwordRequirements.hasUppercase },
              { label: 'Lowercase letter', valid: passwordRequirements.hasLowercase },
              { label: 'Number', valid: passwordRequirements.hasNumber },
              { label: 'Special character (!@#$%...)', valid: passwordRequirements.hasSpecial },
            ].map(({ label, valid }) => (
              <div key={label} className="flex items-center gap-2">
                <div className={`w-4 h-4 rounded-full ${valid ? 'bg-green-500' : 'bg-gray-600'}`} />
                <span className={valid ? 'text-green-400' : 'text-gray-400'}>
                  {label}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Confirm Password */}
      <div>
        <label className="block text-sm font-medium text-gray-300 mb-2">
          Confirm Password
        </label>
        <div className="relative">
          <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-500" />
          <input
            type={showPasswords.confirm ? 'text' : 'password'}
            value={formData.confirmPassword}
            onChange={(e) =>
              setFormData({ ...formData, confirmPassword: e.target.value })
            }
            className="w-full pl-10 pr-10 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white"
            required
            disabled={isLoading}
          />
          <button
            type="button"
            onClick={() =>
              setShowPasswords({
                ...showPasswords,
                confirm: !showPasswords.confirm,
              })
            }
            className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-500"
          >
            {showPasswords.confirm ? <EyeOff size={18} /> : <Eye size={18} />}
          </button>
        </div>
        {formData.confirmPassword && !passwordsMatch && (
          <p className="text-xs text-red-400 mt-1">Passwords do not match</p>
        )}
      </div>

      {/* Submit Button */}
      <button
        type="submit"
        disabled={!formValid || isLoading}
        className="w-full py-2 px-4 bg-primary hover:bg-secondary text-white font-medium rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {isLoading ? 'Changing password...' : 'Change Password'}
      </button>
    </form>
  );
}

export default ChangePasswordForm;
```

---

## Implementation Checklist

### Backend
- [ ] Create ChangePasswordRequest DTO
- [ ] Create ChangePasswordResponse DTO
- [ ] Create POST /auth/change-password endpoint
- [ ] Add password validation function
- [ ] Add password hashing logic
- [ ] Add refresh token clearing (optional)
- [ ] Add audit logging
- [ ] Test with curl/Postman

### Frontend
- [ ] Create ChangePasswordForm component
- [ ] Add apiClient.changePassword() method
- [ ] Integrate form into SettingsPage
- [ ] Add password strength indicator
- [ ] Add real-time validation
- [ ] Add error handling
- [ ] Test form submission
- [ ] Test error scenarios

### Testing
- [ ] Test successful password change
- [ ] Test wrong current password
- [ ] Test weak new password
- [ ] Test passwords don't match
- [ ] Test API validation
- [ ] Test error messages
- [ ] Test loading states

### Security
- [ ] Verify HTTPS only (production)
- [ ] Add rate limiting
- [ ] Add CSRF protection
- [ ] Test password hashing
- [ ] Add audit logging
- [ ] Review error messages (no info leakage)

---

## Summary

The password change feature requires:

1. **Backend**: New endpoint with validation and hashing
2. **Frontend**: Form component with validation and UI
3. **Security**: Strong password requirements, rate limiting, audit logs
4. **UX**: Clear error messages, password strength indicator, success feedback

This is a critical security feature that should follow best practices for password management.
