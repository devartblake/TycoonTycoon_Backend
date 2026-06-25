# Authentication Backend Integration Fix

**Date**: June 25, 2026  
**Issue**: 400 Bad Request errors on login/signup  
**Root Cause**: Missing required `DeviceId` field  
**Status**: ✅ FIXED

## The Problem

The web app was sending incomplete authentication requests:

```
❌ Before:
{
  "email": "user@example.com",
  "password": "password123"
}

✅ After:
{
  "email": "user@example.com",
  "password": "password123",
  "deviceId": "web_550e8400-e29b-41d4-a716-446655440000"
}
```

### Backend Requirements (from AuthEndpoints.cs)

**LoginRequest** (line 5 of AuthDtos.cs):
```csharp
public record LoginRequest(string Email, string Password, string DeviceId);
```

**SignupRequest** (lines 81-88 of AuthDtos.cs):
```csharp
public record SignupRequest(
    string Email,
    string Password,
    string DeviceId,  // ← REQUIRED
    string? Username = null,
    string? Handle = null,
    string? Country = null
);
```

**Backend validation** (AuthEndpoints.cs lines 168-178):
```csharp
if (string.IsNullOrWhiteSpace(request.Email))
    return Results.BadRequest(new { error = "Email is required" });

if (string.IsNullOrWhiteSpace(request.Password))
    return Results.BadRequest(new { error = "Password is required" });

if (string.IsNullOrWhiteSpace(request.DeviceId))
    return Results.BadRequest(new { error = "DeviceId is required" });  // ← This was failing!

if (request.Password.Length < 8)
    return Results.BadRequest(new { error = "Password must be at least 8 characters" });
```

## The Fix

### 1. Enhanced DeviceId Generation
**File**: `src/core/api/client.ts`

Improved device ID generation from simple random ID to UUID-based:

```typescript
private getDeviceId(): string {
  let deviceId = localStorage.getItem('device_id');
  if (!deviceId) {
    // Generate unique device ID: web_UUID_timestamp
    const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = (Math.random() * 16) | 0;
      const v = c === 'x' ? r : (r & 0x3) | 0x8;
      return v.toString(16);
    });
    deviceId = `web_${uuid}`;
    localStorage.setItem('device_id', deviceId);
  }
  return deviceId;
}
```

### 2. Updated Login Method
```typescript
async login(email: string, password: string) {
  const response = await this.post('/auth/login', {
    email,
    password,
    deviceId: this.getDeviceId(),  // ← Added!
  });
  return response.data;
}
```

### 3. Updated Signup Method
```typescript
async signup(email: string, password: string, username?: string) {
  const response = await this.post('/auth/signup', {
    email,
    password,
    deviceId: this.getDeviceId(),  // ← Added!
    ...(username && { username }),
  });
  return response.data;
}
```

## Expected Response Format

**Successful Signup/Login** (200 OK):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-value",
  "expiresIn": 3600,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "handle": "username",
    "email": "user@example.com",
    "country": null,
    "avatarUrl": null,
    "tier": null,
    "mmr": 0,
    "userRoles": null
  }
}
```

**Error Response** (400 Bad Request):
```json
{
  "error": "Email is required"
}
```

## Backend Behavior

### Signup Flow (from AuthEndpoints.cs line 162-244)
1. Validate email, password, deviceId are provided
2. Validate password is at least 8 characters
3. Register the user (`AuthService.RegisterAsync`)
4. Immediately log them in (`AuthService.LoginAsync`)
5. Return tokens + user info (same as login response)

### Login Flow (from AuthEndpoints.cs line 246-271)
1. Call `AuthService.LoginAsync(email, password, deviceId)`
2. Return `AccessToken`, `RefreshToken`, `ExpiresIn`, `User`

## Device ID Storage

The device ID is:
- Generated on first login/signup
- Stored in `localStorage` under key `device_id`
- Reused for all subsequent requests
- Format: `web_[UUID-v4]`
- Example: `web_550e8400-e29b-41d4-a716-446655440000`

## Files Changed

| File | Change |
|------|--------|
| `src/core/api/client.ts` | Enhanced DeviceId generation + Added deviceId to login/signup |

## How to Test

1. Open DevTools → Network tab
2. Go to http://localhost:5173/signup
3. Fill form: email, password (8+ chars), display name
4. Submit
5. Should see `POST /api/v1/auth/signup` with:
   - Status: 200 (success) or 409 (email exists)
   - Body includes `accessToken`, `refreshToken`, `user`
6. Redirect to dashboard
7. Token saved in localStorage

## Comparison with Flutter

The Flutter app also sends `deviceId` in its device identity payload:

```dart
// From trivia_tycoon Flutter app
final payload = {
  'email': email,
  'password': password,
  ...deviceIdentity,  // ← includes deviceId, platform, etc.
};
```

The web app now follows the same pattern! ✅

---

**Summary**: The backend requires `DeviceId` for audit/multi-device tracking. The web app now generates a unique device ID on first use and sends it with all auth requests, matching the Flutter client's behavior.
