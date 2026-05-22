# Frontend-Backend Auth Integration Analysis

## Critical Finding: THREE Duplicate Auth Systems

You have **three separate, overlapping authentication implementations** in your Flutter frontend:

### 1. **`lib/core/auth/`** — JWT Token Manager (Modern, Backend-Ready)
**Files:**
- `auth_manager.dart` - Token lifecycle, refresh logic
- `auth_api.dart` - REST API calls to `/auth/login`, `/auth/refresh`, `/auth/logout`
- `auth_tokens.dart` - Token model with expiry tracking
- `token_store.dart` - Persistence interface

**Strengths:**
- ✅ Automatic token refresh with concurrent request deduplication
- ✅ Expiry-aware (refreshes 20s before expiration)
- ✅ Clean separation: AuthManager (business logic) + AuthApi (HTTP) + TokenStore (persistence)
- ✅ **Ready to connect to your .NET backend**

**Missing:**
- ❌ No `deviceId` support (your backend requires it)
- ❌ Hardcoded to secure storage, no Hive option

---

### 2. **`lib/core/services/`** — Backend Session Manager (Hive-Based)
**Files:**
- `auth_service.dart` - High-level login/logout with device ID support
- `auth_api_client.dart` - REST client for `/auth/login`, `/auth/refresh`, `/auth/logout`
- `auth_token_store.dart` - Hive-based persistence (stores `AuthSession`)

**Strengths:**
- ✅ Already has `deviceId` support (required by your backend)
- ✅ Hive persistence (survives app restarts)
- ✅ Flexible JSON parsing (handles snake_case and camelCase)
- ✅ Stores `userId` alongside tokens

**Missing:**
- ❌ No automatic token refresh logic
- ❌ No concurrent request deduplication

---

### 3. **`lib/ui_components/login/providers/auth.dart`** — Legacy Local-Only Auth
**File:** `auth.dart` + `login_manager.dart`

**What it does:**
- ✅ UI state management (login mode vs signup mode)
- ✅ Conditional backend auth via `ConfigService.useBackendAuth` flag
- ✅ Integrates with `ApiService` for signup/login

**Problems:**
- ❌ Doesn't use either of the above auth systems consistently
- ❌ Stores auth state in `GeneralKeyValueStorageService` instead of token store
- ❌ No token refresh logic
- ❌ Mixes UI state with auth logic

---

## The Problem: Zero Integration

**None of these three systems talk to each other.** Your app has:
- A modern JWT manager (`lib/core/auth/`) that's never used
- A Hive-based backend client (`lib/core/services/`) that's never used
- A UI layer (`login_manager.dart`) that calls `ApiService.login()` but doesn't store tokens properly

**Result:** Your frontend can't actually authenticate with your backend because the systems are disconnected.

---

## Backend API Contract (What You Built)

Based on your `.NET AuthService`, your backend expects:

### **POST `/auth/login`**
```json
{
  "email": "user@example.com",
  "password": "SecurePassword123",
  "deviceId": "unique-device-id"
}
```
**Response 200:**
```json
{
  "accessToken": "eyJhbG...",
  "refreshToken": "dGhlIHJlZnJlc2g...",
  "expiresAtUtc": "2026-02-18T03:00:00Z",
  "userId": "guid-here"
}
```

### **POST `/auth/refresh`**
```json
{
  "refreshToken": "dGhlIHJlZnJlc2g...",
  "deviceId": "unique-device-id"
}
```
**Response 200:**
```json
{
  "accessToken": "new-access-token",
  "refreshToken": "new-refresh-token",
  "expiresAtUtc": "2026-02-18T04:00:00Z"
}
```

### **POST `/auth/logout`**
```json
{
  "deviceId": "unique-device-id"
}
```
**Headers:** `Authorization: Bearer <accessToken>`

---

## What's Missing in Your Backend

### 🔴 **Duplicate Route Error** (Blocking Swagger)
```
Conflicting method/path combination "GET users/me"
```

**You have TWO endpoints registered for `GET /users/me`.**

**Find and delete one:**
```bash
# Search your API project
grep -rn "users/me" Tycoon.Backend.Api/ --include="*.cs"
```

You'll find something like:
```csharp
// Duplicate #1
app.MapGet("/users/me", async (HttpContext ctx) => { ... });

// Duplicate #2
app.MapGet("/users/me", RetrieveCurrentUser);
```

**Delete one of them.** If both are identical, remove one. If different, pick the better implementation.

---

### 🟡 **Missing Signup Endpoint**

Your `LoginManager.signup()` calls:
```dart
await apiService.signup(email: email, password: password, extra: {...});
```

**But your backend has NO `/auth/signup` endpoint.**

You need to add this to your `Tycoon.Backend.Api`:

```csharp
app.MapPost("/auth/signup", async (
    [FromBody] SignupRequest req,
    [FromServices] IAuthService auth,
    CancellationToken ct) =>
{
    // Validate email/password
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest("Email and password required");

    // Create user account
    var user = await auth.CreateUserAsync(
        email: req.Email,
        password: req.Password,
        username: req.Username,
        ct: ct);

    // Generate tokens
    var tokens = await auth.CreateTokensForDeviceAsync(
        userId: user.Id,
        deviceId: req.DeviceId,
        ct: ct);

    return Results.Ok(new
    {
        accessToken = tokens.AccessToken,
        refreshToken = tokens.RefreshToken,
        expiresAtUtc = tokens.ExpiresAtUtc,
        userId = user.Id.ToString()
    });
});

record SignupRequest(string Email, string Password, string DeviceId, string? Username);
```

**Missing method in `IAuthService`:**
```csharp
Task<User> CreateUserAsync(string email, string password, string? username, CancellationToken ct);
```

You'll need to implement this in `AuthService.cs` — hash the password with BCrypt, insert into `Users` table.

---

## Recommended Solution: Consolidate to ONE System

### **Option A: Use `lib/core/services/` (Recommended)**

**Why:** Already has `deviceId`, Hive persistence, and userId support. Just needs refresh logic added.

**Steps:**

1. **Add auto-refresh to `AuthService`** (copy from `AuthManager._refreshTokens`)
2. **Delete `lib/core/auth/`** entirely (it's unused)
3. **Replace `LoginManager` to use `AuthService`** directly instead of `ApiService`
4. **Wire up `AuthApiClient` in your DI**

---

### **Option B: Enhance `lib/core/auth/` and Use It Everywhere**

**Why:** More modern, better concurrency handling.

**Steps:**

1. **Add `deviceId` parameter** to `AuthApi.login()`, `AuthApi.refresh()`, `AuthApi.logout()`
2. **Add `userId` to `AuthTokens` model**
3. **Replace `TokenStore` interface with Hive implementation** (copy from `AuthTokenStore`)
4. **Delete `lib/core/services/auth_*`** (redundant)
5. **Replace `LoginManager` to use `AuthManager`**

---

## Immediate Action Plan

### 1. Fix Swagger (5 minutes)
```bash
cd Tycoon.Backend.Api
grep -rn "users/me" . --include="*.cs"
# Delete one of the duplicate MapGet("/users/me") lines
docker compose restart backend-api
```

### 2. Add Signup Endpoint (30 minutes)
- Add `POST /auth/signup` handler to your API
- Implement `CreateUserAsync` in `AuthService`
- Test with Postman

### 3. Pick ONE Auth System (1 hour)
- **Recommended:** Keep `lib/core/services/` and delete `lib/core/auth/`
- Add automatic token refresh logic to `AuthService`
- Update `LoginManager` to use it consistently

### 4. Wire Everything Together (1 hour)
- Ensure `AuthService` is injected into `LoginManager`
- Test login → token storage → auto-refresh → logout flow
- Remove `ConfigService.useBackendAuth` flag (always use backend)

---

## Testing Checklist

Once integrated, verify:

- [ ] **Login:** Tokens stored in Hive, API returns 200
- [ ] **Token Refresh:** Access token refreshes automatically before expiry
- [ ] **Logout:** Tokens cleared locally + backend revokes refresh token
- [ ] **Signup:** Creates user account + returns tokens
- [ ] **401 Handling:** Expired access token triggers refresh, not re-login
- [ ] **Device ID:** Same across app restarts (use UUID + secure storage)
- [ ] **Concurrent Requests:** Multiple simultaneous API calls don't trigger duplicate refreshes

---

## Files to Delete (After Consolidation)

**If using Option A (keep `lib/core/services/`):**
```
lib/core/auth/auth_manager.dart
lib/core/auth/auth_api.dart
lib/core/auth/auth_tokens.dart
lib/core/auth/token_store.dart
```

**If using Option B (keep `lib/core/auth/`):**
```
lib/core/services/auth_service.dart
lib/core/services/auth_api_client.dart
lib/core/services/auth_token_store.dart
```

**Always delete:**
```
lib/ui_components/login/providers/auth.dart → Keep only UI state, remove AuthService class
```

Move the `AuthService` class out of UI components into `lib/core/services/` properly.

---

## Summary

**Current State:** 🔴 Broken  
- 3 duplicate auth systems, zero integration
- Backend missing signup endpoint
- Swagger broken by duplicate route
- Frontend can't actually authenticate

**Target State:** ✅ Working  
- 1 unified auth system
- Backend has login/signup/refresh/logout
- Tokens auto-refresh
- Frontend properly stores/retrieves tokens
- All API calls use `Authorization: Bearer <token>`

**Effort:** ~4 hours total (1hr backend + 3hr frontend consolidation)

---

Let me know which option you prefer (A or B) and I'll provide the exact code changes needed.
