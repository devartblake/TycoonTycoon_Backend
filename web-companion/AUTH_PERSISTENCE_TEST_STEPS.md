# Authentication Persistence - Test Procedure

**Date**: June 25, 2026  
**Changes**: Added auth state restoration to App.tsx and logout cleanup to authStore.ts  
**Status**: Ready for Testing

## Test Objectives

Verify that:
1. ✅ User stays logged in after page refresh
2. ✅ Auth token is properly stored in localStorage
3. ✅ Auth state is restored from localStorage on app init
4. ✅ Invalid/expired tokens are handled gracefully
5. ✅ Logout properly clears tokens

## Test Cases

### Test 1: Login → Refresh → Should Stay Logged In

**Steps**:
1. Clear browser cache and LocalStorage:
   - Open DevTools (F12)
   - Application → LocalStorage → Delete all entries
   - Close DevTools

2. Go to login page:
   - Navigate to `http://localhost:5173/login`
   - Form should be visible

3. Login with test credentials:
   - Email: `test@example.com`
   - Password: `TestPassword123`
   - Click "Sign In"

4. Verify on dashboard:
   - Should redirect to `http://localhost:5173/`
   - Dashboard should load with user profile visible
   - Check header: User name and currency should display

5. Check localStorage:
   - Open DevTools → Application → LocalStorage
   - Should see:
     - `auth_token`: JWT token value ✅
     - `refresh_token`: Refresh token value ✅
     - `device_id`: Device ID ✅

6. Refresh the page:
   - Press F5 or Ctrl+R
   - **Expected**: App loads and user stays logged in
   - **Expected**: Dashboard displays user profile (no redirect to login)

7. Check console logs:
   - Open DevTools → Console
   - Should see: `[Auth] Auth state restored from localStorage`
   - No error messages about auth failure

**Result**: ✅ PASS (if user stayed logged in) / ❌ FAIL (if redirected to login)

---

### Test 2: Logout → Should Clear Tokens

**Steps**:
1. From logged-in dashboard:
   - Click logout button in AppShell

2. Verify redirect:
   - Should redirect to `/login` page

3. Check localStorage:
   - Open DevTools → Application → LocalStorage
   - `auth_token` should be GONE ❌
   - `refresh_token` should be GONE ❌
   - `device_id` should still exist (it's device-specific, not auth-specific) ✅

4. Try to navigate to protected route directly:
   - Type in URL: `http://localhost:5173/dashboard`
   - Should redirect to `/login` (because isAuthenticated is false)

**Result**: ✅ PASS (if tokens cleared and redirect works) / ❌ FAIL (if tokens remain)

---

### Test 3: Expired Token → Should Handle Gracefully

**Steps**:
1. Login successfully (from Test 1)

2. Manually expire the token:
   - Open DevTools → Application → LocalStorage
   - Find `auth_token` value
   - Modify it to make it invalid (e.g., change a few characters at the end)
   - Save the change

3. Refresh page:
   - Press F5
   - Should redirect to `/login`

4. Check console:
   - Open DevTools → Console
   - Should see: `[Auth] Failed to restore auth state`
   - No 500 errors or unhandled rejections

5. Check localStorage:
   - `auth_token` should be cleared ✅
   - `refresh_token` should be cleared ✅

**Result**: ✅ PASS (if expired token handled gracefully) / ❌ FAIL (if error occurs)

---

### Test 4: No Token → Should Show Login

**Steps**:
1. Clear all localStorage:
   - DevTools → Application → LocalStorage → Delete all

2. Navigate to dashboard:
   - Go to `http://localhost:5173/`
   - Should redirect to `/login` immediately
   - No errors in console

3. Try protected routes:
   - Try `/play` → redirects to `/login`
   - Try `/leaderboard` → redirects to `/login`
   - Try `/skills` → redirects to `/login`

**Result**: ✅ PASS (if redirects work cleanly) / ❌ FAIL (if errors occur)

---

### Test 5: Multiple Sessions → Device ID Persists

**Steps**:
1. Login in Session A:
   - Browser window/tab 1
   - Note the `device_id` from localStorage

2. Login in Session B:
   - New browser window/tab
   - Login with same or different account
   - Check `device_id` → should be DIFFERENT

3. Go back to Session A:
   - Refresh page
   - Should still be logged in
   - `device_id` should be the SAME as before

**Result**: ✅ PASS (if device IDs are unique per session) / ❌ FAIL (if device IDs are shared)

---

## Quick Test Script

Run this in DevTools Console to test the auth flow programmatically:

```javascript
// Test auth persistence
(async () => {
  console.log('=== Auth Persistence Test ===');
  
  // 1. Check initial state
  console.log('Initial auth_token:', localStorage.getItem('auth_token') ? 'EXISTS' : 'MISSING');
  
  // 2. Make a request to get current user
  const token = localStorage.getItem('auth_token');
  if (token) {
    try {
      const response = await fetch('/api/v1/users/me', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      const user = await response.json();
      console.log('User fetch:', response.status === 200 ? 'SUCCESS' : 'FAILED');
      console.log('User data:', user);
    } catch (err) {
      console.error('Error:', err);
    }
  } else {
    console.log('No token in localStorage');
  }
})();
```

---

## Expected Files Changes

### Changed Files
| File | Change |
|------|--------|
| `src/app/App.tsx` | Added useEffect to restore auth on init |
| `src/stores/authStore.ts` | Enhanced logout to clear localStorage |

### How It Works (Flow)

```
App Initialize
  ↓
App.tsx useEffect runs
  ↓
Check for auth_token in localStorage
  ├─ Token not found → Done (user not logged in)
  └─ Token found → Verify it
      ↓
      Call /users/me with token
      ├─ Success (200) → Restore user to store
      └─ Failure (401/403/error) → Clear localStorage & logout
```

---

## Debugging Tips

If tests fail, check:

1. **Console Errors**: Open DevTools → Console, look for:
   - `[Auth] Auth state restored from localStorage` ✅ (good sign)
   - `[Auth] Failed to restore auth state` ❌ (token might be invalid)
   - CORS errors → API proxy might be down
   - 401/403 errors → Token is invalid or expired

2. **Network Tab**: Check requests:
   - `/api/v1/users/me` should show `200 OK` when logged in
   - Should show `Authorization: Bearer <token>` header
   - Response should contain user data (id, email, handle)

3. **Application Tab**: Check LocalStorage:
   - `auth_token` should persist across page reloads (when logged in)
   - Should be cleared immediately on logout

4. **Sources Tab**: Set breakpoint in `App.tsx`:
   - Line with `restoreAuthState()` call
   - Verify it's being called on every page load
   - Check if token is being found and verified

---

## Success Criteria

All tests should PASS:
- [ ] Test 1: Login → Refresh → Logged in
- [ ] Test 2: Logout → Tokens cleared
- [ ] Test 3: Expired token → Handled gracefully
- [ ] Test 4: No token → Shows login
- [ ] Test 5: Device IDs → Persisted correctly

**Overall Result**: 🟢 Authentication persistence working correctly
