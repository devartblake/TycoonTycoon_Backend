# Authentication Issues Analysis & Fixes

**Date**: June 25, 2026  
**Status**: ✅ FIXED  
**Issues Found**: 5 major problems

## Issues Discovered

### 1. ❌ Login Using Mock Implementation (CRITICAL)
**File**: `src/features/auth/pages/LoginPage.tsx`  
**Problem**: Login was hardcoded to use fake credentials instead of calling the API
- User credentials were ignored
- Token was generated locally with `'mock_token_' + Date.now()`
- No actual backend authentication attempted

**Fix**: Implemented real API call via `apiClient.login(email, password)`

### 2. ❌ Signup Using Mock Implementation (CRITICAL)
**File**: `src/features/auth/pages/SignupPage.tsx`  
**Problem**: Signup was hardcoded to use fake credentials instead of calling the API
- User data wasn't validated against backend
- Random user ID generated locally instead of from server
- No email verification or account creation on backend

**Fix**: Implemented real API call via `apiClient.signup(email, password, displayName)`

### 3. ❌ Missing Auth Endpoints in API Client (MAJOR)
**File**: `src/core/api/client.ts`  
**Problem**: API client didn't have login/signup methods
- Only had `getCurrentUser()` and other user endpoints
- No way to authenticate users
- Token refresh was private (internal use only)

**Fix**: Added public authentication methods:
```typescript
async login(email: string, password: string)
async signup(email: string, password: string, username?: string)
async logout()
```

### 4. ⚠️ CORS Proxy Issues (NETWORK)
**File**: `vite.config.ts`  
**Problem**: Vite proxy was configured for `/api` but API calls were using full absolute URLs
- Proxy couldn't intercept requests to `https://api.synaptixplay.com`
- Production server doesn't have CORS headers for localhost

**Fix**: Updated `src/core/env.ts` to use relative paths in development:
```typescript
const apiV1Url = import.meta.env.DEV ? '/api/v1' : `${apiBaseUrl}/api/v1`;
```

### 5. ⚠️ No Error Handling for Network Failures (MEDIUM)
**File**: `src/features/dashboard/pages/DashboardPage.tsx`  
**Problem**: CORS and network errors weren't differentiated from auth errors
- Users got confusing error messages
- No guidance on what went wrong

**Fix**: Added error type detection:
```typescript
const isCorsError = err?.message?.includes('Network Error');
const isAuthError = err?.response?.status === 401 || 403;
```

## Root Cause Analysis

### Why Login Wasn't Working

The authentication system had **3 layers of failure**:

1. **Frontend (LoginPage)**: Used mock implementation → never called backend
2. **API Client**: Missing login/signup methods → couldn't make auth requests even if frontend tried
3. **Network (CORS)**: Even if requests were made, they'd be blocked by CORS

### Why No Server Connection

The development flow was:
```
User enters credentials
        ↓
Frontend mock implementation
        ↓
localStorage.setItem('mock_token')
        ↓
navigate to dashboard
        ↓
Dashboard tries to fetch /users/me
        ↓
CORS Error (server doesn't have CORS headers)
```

## Comparison: Flutter vs Web Implementation

### Flutter (Correct)
```dart
// lib/core/services/auth_api_client.dart
Future<AuthSession> login({
  required String email,
  required String password,
}) async {
  final payload = {
    'email': email,
    'password': password,
    ...deviceIdentity,
  };
  
  final response = await _http.post(
    _u(loginPath),  // /auth/login
    body: jsonEncode(payload),
  );
  
  if (response.statusCode == 200) {
    final session = _parseSession(response.body);
    return session;
  }
}
```

### Web (Before Fix - Incorrect)
```typescript
// LoginPage.tsx
const handleSubmit = async (e) => {
  // ... commented out actual API call
  
  // Mock implementation for now
  const mockUser = { id: '1', email, displayName: email.split('@')[0] };
  setUser(mockUser);
  localStorage.setItem('auth_token', 'mock_token_' + Date.now());
};
```

### Web (After Fix - Correct)
```typescript
// LoginPage.tsx
const handleSubmit = async (e) => {
  const response = await apiClient.login(email, password);
  const { user, token, refreshToken } = response;
  
  localStorage.setItem('auth_token', token);
  if (refreshToken) {
    localStorage.setItem('refresh_token', refreshToken);
  }
  setUser(user);
};
```

## API Endpoints Used

### Authentication Endpoints (From Flutter Reference)
```
POST   /auth/login        → { email, password } → { user, token, refreshToken }
POST   /auth/signup       → { email, password, displayName } → { user, token, refreshToken }
POST   /auth/refresh      → { refreshToken } → { token, refreshToken }
POST   /auth/logout       → {} → { success }
```

### Expected Response Format (from Flutter parsing)
```json
{
  "user": {
    "id": "user-123",
    "email": "user@example.com",
    "displayName": "User Name",
    "role": "user",
    "createdAt": "2026-06-25T10:00:00Z"
  },
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-value"
}
```

## Files Modified

| File | Changes |
|------|---------|
| `src/features/auth/pages/LoginPage.tsx` | ✅ Real API call, token storage, error handling |
| `src/features/auth/pages/SignupPage.tsx` | ✅ Real API call, token storage, error handling |
| `src/core/api/client.ts` | ✅ Added login(), signup(), logout() methods |
| `src/core/env.ts` | ✅ Relative paths in dev, full URLs in prod |
| `vite.config.ts` | ✅ API proxy configuration |
| `src/features/dashboard/pages/DashboardPage.tsx` | ✅ Better error messages |

## Testing the Fix

### To Test Login:
1. Go to `http://localhost:5173/login`
2. Enter credentials (backend will validate)
3. Check browser console for request to `/api/v1/auth/login`
4. Should receive `user`, `token`, `refreshToken` in response
5. Should store token in localStorage
6. Should redirect to `/` (dashboard)

### To Test Signup:
1. Go to `http://localhost:5173/signup`
2. Fill in email, password, display name
3. Submit form
4. Check console for request to `/api/v1/auth/signup`
5. Should authenticate and redirect to dashboard

### Network Debugging
Open DevTools → Network tab
- Should see `auth/login` or `auth/signup` requests
- Should be `POST` to `http://localhost:5173/api/v1/auth/*`
- Status should be `200` (success) or `401/409` (user errors)

## Next Steps

1. ✅ API authentication endpoints implemented
2. ✅ Login/signup pages use real API
3. ⏳ Test with actual backend credentials
4. ⏳ Verify token refresh works
5. ⏳ Test logout flow

## Known Limitations

- Backend must be running and accessible at `https://api.synaptixplay.com`
- Backend must return tokens in expected format
- CORS headers must be configured on backend for production URLs
- Dev proxy only works for `/api` prefix

---

**Summary**: The web app was trying to authenticate via a mock system that bypassed the API entirely. Now it properly calls `/auth/login` and `/auth/signup` endpoints, matches the Flutter client's authentication pattern, and handles errors appropriately.
