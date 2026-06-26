# Authentication Persistence Issue - Test Results

**Date**: June 25, 2026  
**Issue**: User logout on page refresh (auth state not restored from localStorage)  
**Status**: 🔴 CONFIRMED BUG

## The Problem

When a user logs in and refreshes the page, they are logged out. The token is saved in localStorage, but the auth state is lost.

### Root Cause

The app initializes auth store with default values and never restores from localStorage:

```typescript
// In authStore.ts
export const useAuthStore = create<AuthState>((set) => ({
  user: null,                    // ← Always null on init
  isLoading: false,
  error: null,
  isAuthenticated: false,        // ← Always false on init
  // ... actions
}));

// In ProtectedRoute.tsx
const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
if (!isAuthenticated) {
  return <Navigate to="/login" replace />;  // ← Redirects because false!
}
```

### Flow of the Bug

```
1. User logs in successfully
   ↓
2. LoginPage stores token: localStorage.setItem('auth_token', token)
3. LoginPage calls: setUser(user) → auth store updated ✅
4. User redirected to dashboard ✅
   ↓
5. User refreshes page (F5 / Ctrl+R)
   ↓
6. App re-initializes
7. Auth store initialized to: { user: null, isAuthenticated: false } ❌
8. ProtectedRoute checks isAuthenticated → false
9. User redirected to /login ❌
   ↓
10. Token still in localStorage, but state lost
11. User appears logged out
```

## Data Verification

### What's Stored (Present)
- ✅ `auth_token` in localStorage
- ✅ `refresh_token` in localStorage (if provided)
- ✅ `device_id` in localStorage

### What's Lost (Problem)
- ❌ `user` object in auth store
- ❌ `isAuthenticated` flag in auth store

## Expected Behavior

The app should:
1. On initialization, check for `auth_token` in localStorage
2. If token exists, verify it's valid by calling `/users/me`
3. Restore `user` object and set `isAuthenticated: true`
4. If token is invalid/expired, clear localStorage and show login

## Solution

Need to add auth state restoration in `App.tsx`:

```typescript
import { useEffect } from 'react';
import { useAuthStore } from '@stores';
import { apiClient } from '@core/api/client';

function App() {
  useEffect(() => {
    const restoreAuthState = async () => {
      const token = localStorage.getItem('auth_token');
      if (!token) return; // No token, user is not logged in
      
      try {
        // Try to fetch current user to verify token is valid
        const userData = await apiClient.getCurrentUser();
        
        // Restore user to auth store
        const user = {
          id: userData.id,
          email: userData.email,
          displayName: userData.handle || 'User',
          avatar: userData.avatarUrl || undefined,
          role: (userData.userRoles?.[0] || 'user').toLowerCase() as 'user' | 'admin',
          createdAt: new Date().toISOString(),
        };
        useAuthStore.getState().setUser(user);
      } catch (err) {
        console.error('Auth restore failed:', err);
        // Token is invalid, clear it
        localStorage.removeItem('auth_token');
        localStorage.removeItem('refresh_token');
        useAuthStore.getState().logout();
      }
    };
    
    restoreAuthState();
  }, []);
  
  // ... rest of App component
}
```

## Test Steps to Verify Bug

1. **Start clean**:
   - Open DevTools → Application → LocalStorage
   - Clear all local storage

2. **Login**:
   - Go to http://localhost:5173/login
   - Enter credentials: test@example.com / TestPassword123
   - Click Sign In
   - Should see dashboard with profile loaded

3. **Verify token is stored**:
   - Open DevTools → Application → LocalStorage
   - Check: `auth_token` is present ✅
   - Check: `device_id` is present ✅

4. **Refresh page**:
   - Press F5 or Ctrl+R
   - **BUG**: Redirected to /login (even though token is in localStorage)

5. **Verify token still exists**:
   - Open DevTools → Application → LocalStorage
   - Token is STILL there, but user was logged out ❌

## Files That Need Changes

| File | Change | Priority |
|------|--------|----------|
| `src/app/App.tsx` | Add useEffect to restore auth state on init | CRITICAL |
| `src/stores/authStore.ts` | Optional: Add initialization from localStorage | MEDIUM |

## Related Issues

- This affects all protected routes
- User must login again after every page refresh
- Breaks user experience
- Makes app feel unstable

## Next Steps

1. ✅ Identify root cause (auth state not restored on init)
2. ⏳ Implement auth state restoration in App.tsx
3. ⏳ Test persistence works across page refreshes
4. ⏳ Test token expiration handling
5. ⏳ Test logout properly clears localStorage
