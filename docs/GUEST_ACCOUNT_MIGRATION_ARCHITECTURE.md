# Guest Account Migration Architecture

## Overview

This document describes the recommended architecture for guest account management and seamless migration to full user accounts in Synaptix Trivia Tycoon.

---

## ✅ **Recommended Approach: Guest → Full Account Migration**

Your suggested approach is **excellent and industry-standard**. This is the best practice used by apps like Slack, Discord, Duolingo, and Firebase.

### **Why This Is the Right Choice**

| Aspect | Benefit |
|--------|---------|
| **Low Friction** | Users start playing immediately without signup friction |
| **Data Continuity** | All progress transfers seamlessly to full account |
| **Engagement** | Higher initial engagement = better onboarding metrics |
| **Optional Full Account** | Users can play indefinitely as guest if they prefer |
| **Industry Standard** | Proven pattern with millions of users |

---

## Architecture

### **Phase 1: Guest Account Creation** (Device Bootstrap)

```
POST /auth/device/bootstrap
{
  "deviceId": "unique-device-id",
  "displayName": "Player_12345"
}
```

**What Happens:**
```
1. Backend creates anonymous User record
   - Email: "guest_{guid}@guest.local" (synthetic, non-deliverable)
   - Handle: Auto-generated (e.g., "Player_12345")
   - IsAnonymous: true ← KEY FLAG

2. Backend creates Player record
   - Username: matches User.Handle
   - Level: 1, Xp: 0
   - Links to User by Handle

3. Backend creates PlayerWallet
   - Coins: 0
   - Diamonds: 0
   - Xp: 0

4. Return access/refresh tokens
   - User can immediately start playing
   - All actions (XP, coins) accumulate in Player/Wallet
```

**Database State After Phase 1:**
```
Users table:
  id: {guid}
  email: "guest_{guid}@guest.local"
  handle: "Player_12345"
  is_anonymous: true ✓
  country: null
  avatar_url: null
  created_at: now

Players table:
  id: {guid}
  username: "Player_12345"
  level: 1
  xp: 0
  score: 0
  coins: 0
  diamonds: 0
  created_at: now

PlayerWallets table:
  player_id: {guid}
  coins: 0
  diamonds: 0
  xp: 0
```

---

### **Phase 2: User Reaches Final Onboarding Screen**

Guest plays game, earns XP, collects coins, builds progress. At the end of onboarding (or whenever convenient), app shows:

```
"Create Account to Save Your Progress"
├─ Email
├─ Password
├─ Display Name (optional)
└─ Country (optional)
```

---

### **Phase 3: Full Account Creation (Migration)**

```
POST /account/migrate-to-full
Authorization: Bearer {guest-access-token}

{
  "email": "user@example.com",
  "password": "password123",
  "deviceId": "{current-device-id}",
  "username": "custom_username",  // optional, uses guest username if not provided
  "country": "US",               // optional
  "deleteGuestAccount": true     // whether to clean up guest player record
}
```

**What Happens (Transactional):**

```
1. Validate guest account is anonymous (IsAnonymous=true)
   ✓ Yes → continue
   ✗ No  → Error 400 "Already registered"

2. Upgrade User record to full account
   - Set: email, password, handle
   - Set: IsAnonymous = false ← KEY CHANGE
   - Preserve: all Flags, Country, AvatarUrl
   - Preserve: CreatedAt (shows when guest was created)

3. Player & Wallet data STAYS AS-IS
   - NO data transfer needed (same User → same Player by Handle)
   - All progress is preserved automatically
   - Coins, XP, Level all carried forward

4. Database transaction commits
   - All-or-nothing: if any step fails, rollback everything
   - No orphaned guests

5. Return new access/refresh tokens
   - User now authenticated as full account
   - Can log in with email/password going forward
   - Guest tokens no longer work

6. Optional: Delete guest Player record
   - If deleteGuestAccount=true, remove duplicate Player if needed
   - In this architecture, typically NOT needed (Player already linked)
```

**Database State After Phase 3:**

```
Users table (SAME RECORD UPDATED):
  id: {guid}                          ← Same ID
  email: "user@example.com"           ← CHANGED
  handle: "custom_username"           ← CHANGED (if provided)
  password_hash: "bcrypt_hash"        ← ADDED
  is_anonymous: false                 ← CHANGED (was true)
  country: "US"                       ← UPDATED
  created_at: {original}              ← UNCHANGED
  
// Players table: NO CHANGE
// PlayerWallet table: NO CHANGE
// All progress preserved!
```

---

## **Endpoint Reference**

### **1. Guest Account Creation**
```
POST /auth/device/bootstrap
Response: LoginResponse (access_token, refresh_token)
```

### **2. Get Full Profile (works for both guest & full)**
```
GET /api/v1/users/me
Authorization: Bearer {token}
Response: CurrentUserProfileDto
  - Shows IsAnonymous: true/false
  - Shows all player progress
  - Shows email (null if guest)
```

### **3. Upgrade Guest to Full Account**
```
POST /account/migrate-to-full
Authorization: Bearer {guest-token}
Request: AccountMigrationRequest
Response: AccountMigrationResponse
  - New access/refresh tokens
  - Full profile
  - Confirmation that guest was deleted (if requested)
```

---

## **Flutter Implementation Flow**

### **Onboarding Screens**

```dart
Sequence:
1. [SplashScreen]
   → Check if already logged in
   → If yes: go to Home
   → If no: go to GuestOrLoginChoice

2. [GuestOrLoginChoice]
   ├─ "Play as Guest" 
   │  → POST /auth/device/bootstrap
   │  → Store guest tokens in secure storage
   │  → Go to HomeScreen
   │
   └─ "Sign In / Register"
      → Go to LoginRegisterScreen

3. [HomeScreen] (as Guest)
   → User plays game, accumulates progress
   → Action: "Save Your Progress"
   → Go to CreateAccountScreen

4. [CreateAccountScreen]
   ├─ Email input
   ├─ Password input
   ├─ Display name (optional)
   ├─ Country (optional)
   └─ "Create Account" button
      → POST /account/migrate-to-full
      → Receive new tokens
      → Store new tokens (replacing guest tokens)
      → Go to HomeScreen (now as full user)

5. [HomeScreen] (as Full User)
   → All previous progress intact
   → User can now sign in on other devices
```

### **Key Flutter Logic**

```dart
// During onboarding completion
Future<void> createFullAccount({
  required String email,
  required String password,
  required String? displayName,
  required String? country,
}) async {
  try {
    final response = await apiClient.post(
      '/account/migrate-to-full',
      body: AccountMigrationRequest(
        email: email,
        password: password,
        deviceId: deviceIdService.deviceId,
        username: displayName,
        country: country,
        deleteGuestAccount: true,
      ),
    );

    // Update secure storage with new tokens
    await secureStorage.saveTokens(
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
    );

    // Get full profile
    final profile = await apiClient.get('/users/me');
    
    // Save profile locally
    await profileService.saveProfile(profile);

    // Navigate to home
    context.go('/home');
  } on ConflictException catch (e) {
    if (e.error == 'email_already_exists') {
      showErrorDialog('Email already in use');
    } else if (e.error == 'username_taken') {
      showErrorDialog('Username taken, please choose another');
    }
  }
}
```

---

## **Error Handling**

| Scenario | Error | Solution |
|----------|-------|----------|
| Email already exists | 409 Conflict | Show: "Email already registered" |
| Username taken | 409 Conflict | Show: "Username taken" |
| Weak password | 400 Bad Request | Show: "Password must be 8+ chars" |
| Missing field | 400 Bad Request | Validate client-side first |
| User not anonymous | 400 Bad Request | "Account already registered" |
| Network failure | Connection error | Retry with exponential backoff |

---

## **Data Integrity Guarantees**

### **Transaction Safety**
- Migration uses database transaction
- If ANY step fails: entire migration rolls back
- No orphaned data

### **Progress Preservation**
- Player data linked by Handle (auto-matches)
- No XP/coins lost
- Level preserved
- Score preserved

### **Backward Compatibility**
- Old `/auth/account/upgrade` still works
- New `/account/migrate-to-full` has full data handling
- Both endpoints available

---

## **Security Considerations**

### **Phase 1 (Guest)**
✅ Device ID as primary identity  
✅ No password required  
✅ Synthetic email (non-functional)  
✅ Tokens issued via Device Bootstrap flow  

### **Phase 3 (Upgrade)**
✅ Verify user is authenticated (JWT)  
✅ Verify account is actually anonymous  
✅ Require strong password (8+ chars)  
✅ Require valid email  
✅ Database transaction for atomicity  
✅ New tokens issued after upgrade  

---

## **Metrics & Monitoring**

Track these to measure success:

```
- Guest account creation rate
- Migration conversion rate (guests → full)
- Avg progress before migration
- Migration success rate
- Migration failure reasons
- Time to migrate (from guest creation)
- % of users who never migrate (stay guest)
```

---

## **Summary**

This architecture is:
- ✅ **Simple**: Minimal code, clear flow
- ✅ **Safe**: Transactional, data-preserving
- ✅ **Proven**: Used by Slack, Discord, Duolingo
- ✅ **Flexible**: Users can play indefinitely as guest
- ✅ **Engaging**: No friction, high conversion

Implement Phase 1 and Phase 3 exactly as shown above, and your onboarding will be solid.
