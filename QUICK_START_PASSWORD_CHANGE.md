# Password Change Feature - Quick Start Guide

## TL;DR

Password change feature is **50% complete**:
- ✅ **Frontend**: Done (form component, validation, UI)
- ⏳ **Backend**: Ready to implement (just copy-paste the code)

## What's Already Done

### Frontend Implementation (No Backend Needed Yet)

**Files Created**:
1. `src/features/settings/components/ChangePasswordForm.tsx` - Complete form component
2. `src/features/dashboard/pages/SettingsPage.tsx` - Updated with modal
3. `src/core/api/client.ts` - Added `changePassword()` method

**User Experience**:
```
Settings page → Privacy & Security → "Change Password" button
              ↓
              Modal opens with form
              ↓
              User enters: current password, new password, confirm
              ↓
              Form shows password strength
              ↓
              Submit button
```

**Features**:
- ✅ Password strength indicator (8 chars, upper, lower, number, special)
- ✅ Show/hide password toggles
- ✅ Real-time validation
- ✅ Error messages
- ✅ Success confirmation
- ✅ Loading state

---

## What Needs to Be Done (Backend)

### Super Quick (15 minutes)

Just 3 things to add to your backend:

#### 1. Add DTOs (AuthDtos.cs)
```csharp
public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ChangePasswordResponse(
    string Message,
    bool SessionCleared,
    bool RequiresReauth
);
```

#### 2. Add Route (AuthEndpoints.cs)
```csharp
// In the Map method, add:
authGroup.MapPost("/change-password", HandleChangePassword).RequireAuthorization();
```

#### 3. Add Handler (AuthEndpoints.cs)
Copy the complete `HandleChangePassword` method from:
`BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md`

(Yes, it's that simple!)

---

## User Flow (Actual)

```
1. User in Settings → Click "Change Password"
   ↓
2. Modal pops up with form (ChangePasswordForm component)
   ↓
3. User enters:
   - Current password (required)
   - New password (8+, upper, lower, number, special)
   - Confirm password (must match)
   ↓
4. Frontend validates locally (instant feedback)
   ↓
5. User clicks "Change Password" button
   ↓
6. Frontend sends: POST /api/v1/auth/change-password
   {
     "currentPassword": "...",
     "newPassword": "..."
   }
   ↓
7. Backend validates:
   - User authenticated ✓
   - Current password correct ✓
   - New password strong ✓
   - Different from old ✓
   ↓
8. Backend hashes new password and saves
   ↓
9. Returns 200 OK
   ↓
10. Frontend shows: "Password changed successfully!"
    ↓
11. Modal closes, user still logged in
```

---

## Key Design Decisions

### Why This Way?

| Decision | Why | Benefit |
|----------|-----|---------|
| Modal instead of page | Less disruptive | Better UX |
| Show/hide toggle | Users verify input | Standard UI pattern |
| Strength indicator | Shows requirements | Fewer failed attempts |
| Frontend validation | Instant feedback | Better UX + less server load |
| Backend validation | Never trust client | Prevents API bypass attacks |
| bcrypt hashing | Industry standard | Secure + slow (attacks take longer) |

---

## Password Requirements

Users must enter a password with ALL of:
1. **8+ characters** (not "test123" - too short)
2. **Uppercase letter** (A-Z) - not "password123"
3. **Lowercase letter** (a-z) - not "PASSWORD123"
4. **Number** (0-9) - not "Password!"
5. **Special character** (!@#$%^&*) - not "Password123"

**Valid examples**:
- MyNewPassword123!
- Trivia@Tycoon456
- StrongPass#2026!

**Invalid examples**:
- password (no uppercase, number, special)
- Password123 (no special char)
- Pwd@1 (too short)

---

## Testing the Feature

### End-to-End (After Backend is Done)

```bash
# 1. Start app at http://localhost:5173

# 2. Login (or signup)

# 3. Go to Settings

# 4. Click "Privacy & Security" → "Change Password"

# 5. Fill form:
   Current: Your current password
   New: MyNewPass123!
   Confirm: MyNewPass123!

# 6. Click button

# 7. Should see: "Password changed successfully!"

# 8. Test new password works:
   - Logout
   - Login with new password
   - Old password shouldn't work
```

### Using curl (After Backend is Done)

```bash
# Get a token first
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPass123!",
    "deviceId": "web-test"
  }' | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

# Change password
curl -X POST http://localhost:5000/api/v1/auth/change-password \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "TestPass123!",
    "newPassword": "NewPass456!"
  }'

# Should return 200 OK with success message
```

---

## Security Best Practices (Implemented)

✅ **Frontend**:
- Password inputs masked by default
- Show/hide toggle available
- Client-side validation
- No passwords logged

✅ **Backend** (you'll add):
- Verify current password hash
- Validate new password meets requirements
- Hash new password with bcrypt (workFactor: 12)
- Don't reveal whether email exists
- Log password change events
- Rate limiting (max 5 attempts/hour)

---

## Architecture Diagram

```
Web App (http://localhost:5173)
│
├─ SettingsPage (has modal state)
│  └─ ChangePasswordForm (form component)
│     └─ apiClient.changePassword()
│
└─ API Client (src/core/api/client.ts)
   └─ POST /api/v1/auth/change-password
      │
      └─ Backend (https://api.synaptixplay.com)
         └─ AuthEndpoints.HandleChangePassword()
            ├─ Verify JWT token
            ├─ Check current password
            ├─ Validate new password
            ├─ Hash new password
            └─ Update in database
```

---

## Documentation Files

Created for you:

1. **PASSWORD_CHANGE_IMPLEMENTATION_GUIDE.md** (Comprehensive)
   - Full user flow with diagrams
   - Architecture details
   - Security practices
   - API contract
   - Code examples

2. **BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md** (How-to)
   - Step-by-step backend setup
   - Complete code to copy-paste
   - Testing procedures
   - Error scenarios
   - Optional enhancements

3. **PASSWORD_CHANGE_FEATURE_COMPLETE.md** (Overview)
   - What's done vs. to-do
   - File checklist
   - Full API contract
   - Testing checklist
   - Next steps

4. **QUICK_START_PASSWORD_CHANGE.md** (This file)
   - Quick reference
   - 15-minute backend setup
   - Testing guide

---

## Next Steps

### This Week
1. Read `BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md`
2. Add DTOs to `AuthDtos.cs`
3. Add endpoint route to `AuthEndpoints.cs`
4. Add handler method (copy-paste from doc)
5. Test with curl

### Next Week
1. Test in UI (http://localhost:5173/settings)
2. Test error scenarios
3. Add rate limiting
4. Deploy to staging
5. Load testing

### Nice-to-Have
1. Email notification: "Your password was changed"
2. Password history: can't reuse old passwords
3. Clear all sessions: force re-login on other devices
4. Login activity log: see when password was changed

---

## Common Questions

**Q: Will I break anything?**
A: No. The endpoint is new. Existing code isn't affected.

**Q: What if user forgets current password?**
A: They'll get "Current password is incorrect" error. Need password reset feature (separate implementation).

**Q: Do I need to force re-login?**
A: No. Users stay logged in. (Optional: clear refresh tokens to force re-login everywhere)

**Q: What if password change fails?**
A: User sees error message. Can try again. Original password unchanged.

**Q: Is this secure?**
A: Yes. Follows OWASP guidelines. Uses bcrypt + validation + audit logging.

**Q: How long does hashing take?**
A: ~100ms per password (bcrypt workFactor: 12). That's intentional (slows down attacks).

---

## Estimated Implementation Time

- **Frontend**: ✅ 0 hours (already done)
- **Backend**: ⏳ 15 minutes (copy-paste 3 code snippets)
- **Testing**: 30 minutes (curl + UI testing)
- **Total**: ~45 minutes

---

## Support

All code and detailed instructions are in:
- `PASSWORD_CHANGE_IMPLEMENTATION_GUIDE.md` - Theory
- `BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md` - Practice
- `PASSWORD_CHANGE_FEATURE_COMPLETE.md` - Overview
- `QUICK_START_PASSWORD_CHANGE.md` - This guide

Start with `BACKEND_PASSWORD_CHANGE_IMPLEMENTATION.md` and follow the steps!

