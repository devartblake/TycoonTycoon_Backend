# Backend plan: main login and admin login flows

## Current state
- Main user auth is exposed under `/auth/*`.
- Admin auth is exposed under `/admin/auth/*`.
- Both currently rely on the same underlying auth service behavior with route-level differences.

## Recommended target architecture

### 1) Keep endpoint surfaces separate (frontend clarity)
- User app calls only `/auth/*`.
- Admin app calls only `/admin/auth/*`.
- This avoids accidental cross-surface token usage and keeps frontend logic simple.

### 2) Unify token issuance logic in backend service layer
- Introduce one internal token issuing service that supports two token profiles:
  - `user` profile (default gameplay scopes)
  - `admin` profile (admin scopes + elevated claims)
- Keep refresh rotation logic shared.

### 3) Add explicit JWT claims and policies
- Add explicit claims:
  - `role` (`user|admin`)
  - `scope` (space-delimited or array, e.g. `users:read questions:write`)
  - `aud` (`mobile-app` vs `admin-app`)
- Enforce policies by scope for `/admin/**` routes.

### 4) Separate refresh token stores by client type (or client marker)
- Keep one table if preferred, but include `clientType` (`user`/`admin`) and enforce matching on refresh.
- Reject attempts to exchange a user refresh token on admin refresh endpoints and vice versa.

### 5) MFA gate for admin login
- Add policy/config toggle:
  - `AdminAuth:RequireMfa`
- If enabled, `/admin/auth/login` requires a valid `otpCode`.

### 6) Frontend contract guidance
- Frontend should treat user/admin auth as separate sessions and token caches.
- Never reuse `/auth` refresh tokens for `/admin/auth` calls.

## Suggested phased implementation
1. Add role/scope/audience claims to issued JWTs.
2. Add policy-based scope enforcement across admin endpoints.
3. Add refresh token `clientType` enforcement.
4. Add admin MFA enforcement toggle and validation.
5. Add contract tests for cross-surface token misuse.
