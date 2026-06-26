# Profile & Currency Display Fix

**Date**: June 25, 2026  
**Status**: ✅ FIXED  
**Issue**: User profile and in-game currency (coins/diamonds) were not displaying on dashboard and menu

## Problem

The app had the UI components ready to display user profile and wallet (coins/diamonds), but the data wasn't being loaded properly:

1. **AppShell** was set up to display `profile.coins` and `profile.diamonds` but profile was always `null`
2. **DashboardPage** was only fetching user data (`/users/me`) which doesn't include wallet info
3. **Login/Signup** responses were not being properly transformed from backend DTOs to frontend types
4. Wallet data (Credits and SynapseShards) wasn't being fetched separately

## Root Cause Analysis

### 1. Backend API Structure

The backend returns wallet data in a different endpoint:
- **User endpoint** (`GET /users/me`): Returns `UserDto` with basic user info (id, handle, email, avatar, tier, mmr)
- **Wallet endpoint** (`GET /users/me/wallet`): Returns `PlayerWalletDto` with currency data

```csharp
// UserDto from /users/me
public record UserDto(
    Guid Id,
    string Handle,
    string Email,
    string? Country,
    string? AvatarUrl,
    string? Tier,
    int Mmr,
    IReadOnlyList<string>? UserRoles = null
);

// PlayerWalletDto from /users/me/wallet
public sealed record PlayerWalletDto(
    Guid PlayerId,
    int Credits,           // ← In-game coins
    int NeuralXp,
    int SynapseShards,     // ← Premium currency (diamonds)
    DateTimeOffset UpdatedAtUtc);
```

### 2. Frontend Data Mapping Issues

The frontend auth store expected different field names than what the backend returns:

```typescript
// Frontend User interface (auth store)
export interface User {
  id: string;
  email: string;
  displayName: string;  // ← Backend sends 'handle'
  avatar?: string;      // ← Backend sends 'avatarUrl'
  role: 'user' | 'admin';
  createdAt: string;
}

// Frontend PlayerProfile interface (profile store)
export interface PlayerProfile {
  coins: number;        // ← Backend sends 'credits'
  diamonds: number;     // ← Backend sends 'synapseShards'
  // ... other fields
}
```

## Fixes Applied

### 1. Fixed LoginPage.tsx (User Transformation)

**Before**: Directly passed backend UserDto to auth store (field names didn't match)

**After**: Transform UserDto to frontend User format:
```typescript
const response = await apiClient.login(email, password);
const { user: backendUser, token, refreshToken } = response;

// Transform backend UserDto to frontend User
const user = {
  id: backendUser.id,
  email: backendUser.email,
  displayName: backendUser.handle || email.split('@')[0],
  avatar: backendUser.avatarUrl || undefined,
  role: (backendUser.userRoles?.[0] || 'user').toLowerCase() as 'user' | 'admin',
  createdAt: new Date().toISOString(),
};

setUser(user);
```

### 2. Fixed SignupPage.tsx (User Transformation)

**Same fix as LoginPage**: Transform UserDto to frontend User format before setting in auth store

### 3. Fixed DashboardPage.tsx (Profile with Wallet Data)

**Before**: Only fetched `/users/me`, missing wallet data
```typescript
const userData = await apiClient.getCurrentUser();
setProfile(userData);  // ❌ Missing coins/diamonds
```

**After**: Fetch both endpoints and combine into PlayerProfile:
```typescript
const userData = await apiClient.getCurrentUser();
const walletData = await apiClient.getUserWallet();

// Combine into PlayerProfile format
const profileData = {
  playerId: userData.id,
  username: userData.handle,
  coins: walletData.credits || 0,        // ← From wallet endpoint
  diamonds: walletData.synapseShards || 0,  // ← From wallet endpoint
  tier: userData.tier ? (userData.tier.toLowerCase() as any) : 'bronze',
  // ... other fields
};

setProfile(profileData);
```

### 4. AppShell.tsx (Already Correct)

✅ AppShell was already set up to display wallet info:
```typescript
{profile && (
  <div className="flex items-center gap-3">
    <div className="flex items-center gap-2 px-3 py-2 rounded-lg">
      <Coins size={18} />
      <span className="font-semibold text-sm">{profile.coins}</span>
    </div>
    <div className="flex items-center gap-2 px-3 py-2 rounded-lg">
      <span>💎</span>
      <span className="font-semibold text-sm">{profile.diamonds}</span>
    </div>
  </div>
)}
```

## Data Flow (After Fix)

```
1. User logs in
   ↓
2. LoginPage calls apiClient.login(email, password)
   ↓
3. Backend returns: { user: UserDto, token, refreshToken }
   ↓
4. LoginPage transforms UserDto → User (field mapping)
   ↓
5. LoginPage stores token and calls setUser(user)
   ↓
6. Auth store updates, isAuthenticated = true
   ↓
7. Router redirects to /dashboard (protected route)
   ↓
8. DashboardPage mounts
   ↓
9. DashboardPage calls:
   - apiClient.getCurrentUser()  → UserDto
   - apiClient.getUserWallet()   → PlayerWalletDto
   ↓
10. DashboardPage combines both into PlayerProfile
    {
      playerId: userData.id,
      coins: walletData.credits,
      diamonds: walletData.synapseShards,
      ...
    }
   ↓
11. DashboardPage calls setProfile(profileData)
    ↓
12. Profile store updates
    ↓
13. AppShell re-renders, displays:
    - User name and email (from auth store)
    - Coins and diamonds (from profile store)
```

## Files Modified

| File | Change | Type |
|------|--------|------|
| `src/features/auth/pages/LoginPage.tsx` | Transform UserDto to User before setting in store | Auth |
| `src/features/auth/pages/SignupPage.tsx` | Transform UserDto to User before setting in store | Auth |
| `src/features/dashboard/pages/DashboardPage.tsx` | Fetch both user data and wallet data, combine into PlayerProfile | Profile |

## API Contracts

### Request: Signup
```bash
POST /api/v1/auth/signup
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "deviceId": "web_550e8400-e29b-41d4-a716-446655440000",
  "username": "Display Name"
}
```

### Response: Signup (200 OK)
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh-token-value",
  "expiresIn": 3600,
  "userId": "ccca006c-a43e-4fb1-81af-9e65b9a230c0",
  "user": {
    "id": "ccca006c-a43e-4fb1-81af-9e65b9a230c0",
    "handle": "test",
    "email": "test@example.com",
    "country": null,
    "avatarUrl": null,
    "tier": "T1",
    "mmr": 1000,
    "userRoles": null
  }
}
```

### Request: Get Wallet
```bash
GET /api/v1/users/me/wallet
Authorization: Bearer {accessToken}
```

### Response: Get Wallet (200 OK)
```json
{
  "playerId": "ccca006c-a43e-4fb1-81af-9e65b9a230c0",
  "credits": 500,
  "neuralXp": 0,
  "synapseShards": 10,
  "updatedAtUtc": "2026-06-25T22:31:00.4644046+00:00"
}
```

## Verification Steps

1. Open http://localhost:5173/signup
2. Fill in email, password, display name
3. Submit form
4. Should redirect to dashboard
5. **Check header bar**: Should display:
   - User name and email (from auth store)
   - Coins icon with number (from wallet.credits)
   - Diamond emoji with number (from wallet.synapseShards)

## Expected Result

✅ **Dashboard displays**:
- User profile name and email in top right
- Coins count (default 0 for new users)
- Diamonds count (default 0 for new users)
- All stats cards (level, rank, streak, accuracy) - if profile data available

✅ **Menu/AppShell displays**:
- User name in header
- Coins and diamonds in header

## Known Limitations

- Level, rank, streak, accuracy are hard-coded to default values (1, 1, 0, 0) — full profile data will come from a comprehensive profile endpoint in Phase 3.3
- Onboarding reward can be claimed via `/users/me/onboarding-reward` to get starter coins

## Next Steps for Phase 3.3

1. ✅ User profile and currency display working
2. ⏳ Create comprehensive `/users/me/profile` endpoint that returns all PlayerProfile data (level, xp, rank, streak, accuracy, skills, achievements, etc.)
3. ⏳ Replace hard-coded values in DashboardPage with data from comprehensive profile endpoint
4. ⏳ Implement profile update endpoints (level up, skill unlock, achievement unlock, etc.)
5. ⏳ Add wallet transaction history to profile

---

**Summary**: The web app now properly fetches and displays user profile data (name, email, avatar) and in-game currency (coins/diamonds) on the dashboard and app shell menu. The fixes include proper data transformation from backend DTOs to frontend types, and fetching wallet data from a separate endpoint.
