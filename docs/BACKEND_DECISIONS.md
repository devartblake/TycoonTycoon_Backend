# BACKEND_DECISIONS.md

## Purpose
This document freezes the backend security + integration decisions so the admin wiring can be completed without guesswork. These decisions are **authoritative** for the admin auth flow, token refresh semantics, user enums, event ingestion dedupe, and config/notification management.

---

## 1) Admin MFA (otpCode): Required Always vs Conditional

### Decision
**MFA is ALWAYS required for all admin roles.**

### Scope
MFA requirement applies to any user whose `role` is in the admin role set (server-defined):
- `ADMIN`
- `SUPER_ADMIN`
- (Optional additional privileged roles if you add them later: `OPS`, `FINANCE`, etc.)

### Behavior
- **Admin login is a two-step flow**:
  1) `/auth/login` returns an MFA challenge (`challenge_id`) without issuing tokens if MFA is required.
  2) `/auth/mfa/verify` validates the `otpCode` and then returns tokens.

- **Step-up authentication** is required for sensitive actions (even after login), such as:
  - Role/permission changes
  - Exporting PII
  - Billing/payment configuration
  - API key / webhook management

### MFA Method
- Primary method: **TOTP**
- `otpCode` refers to the **TOTP code** provided by the authenticator app.
- Recovery codes may exist as a future enhancement; store only hashed codes if implemented.

---

## 2) Token Refresh Contract: Rotation Policy & Expiry Semantics

### Decision
**Rotating refresh tokens on every use** + **replay detection** + **session model**.

### Token Types
- **Access token**
  - Type: JWT
  - TTL: **10 minutes**
  - Used on all authenticated requests

- **Refresh token**
  - Type: Opaque string (server-side validated; store hashed)
  - TTL: **14 days**
  - **Rotates on every refresh**
  - Stored/validated per session and token family

### Rotation + Replay Defense
On `POST /auth/refresh`:
- If refresh token is valid and not revoked:
  - Issue a new refresh token and new access token
  - Revoke the previous refresh token (link it to the new one)

If a **previously rotated/revoked refresh token** is presented again:
- Treat as a replay/theft event
- **Revoke the entire session / token family**
- Require full re-auth + MFA for admins

### Session Model (Required)
Tokens are tied to a `session_id` (e.g., device/session tracking):
- `session_id` is returned with tokens
- Server can revoke by session or token family

---

## 3) Canonical Enums for Users API Integration

### Decision
Enums are **server-canonical**. Clients map these values for display only.

#### AccountStatus
- `ACTIVE`
- `PENDING_VERIFICATION`
- `SUSPENDED`
- `BANNED`
- `DELETED`

#### UserRole
- `USER`
- `MODERATOR`
- `ADMIN`
- `SUPER_ADMIN`

#### AgeGroup (coarse buckets)
- `UNDER_13`
- `AGE_13_17`
- `AGE_18_24`
- `AGE_25_34`
- `AGE_35_44`
- `AGE_45_PLUS`
- `UNKNOWN`

Optional (recommended): expose `/meta/enums` to return enum values for admin UI filtering.

---

## 4) Event Upload Dedupe Strategy (eventId vs hash)

### Decision
- **Primary idempotency key**: `eventId` (client-generated UUIDv7/ULID recommended)
- **Secondary monitoring key**: `eventHash` computed server-side from normalized fields

### Behavior
- If `eventId` duplicates:
  - Respond idempotently (`duplicate`)
  - Do not re-enqueue / reprocess

- If `eventHash` repeats with different IDs:
  - Allowed (analytics often repeats)
  - May be flagged for monitoring/rate controls

### Storage Expectations
- Unique index on `eventId`
- Index on `eventHash`, `user_id`, and `occurredAt`

---

## 5) Notifications & Configuration: Server-managed vs Local-only

### Decision
**Hybrid model.**

#### Server-managed (authoritative)
- MFA requirement policy
- Token TTL/rotation policy
- Role/permission matrix
- Kill-switches / flags affecting money/security/compliance
- Notification eligibility rules (compliance/system notices)

#### Local-first (device UX)
- Theme/layout preferences
- Non-critical UX toggles

#### Mixed (server default, client override + sync)
- Notification preference (email/push/sms/none)
- Sampling/performance toggles
- Experiment flags (if needed)

### Precedence Rules
- `policy.*`: server always wins
- `featureFlags.*`: server wins
- `preferences.*`: local wins but sync to server for portability

---

## Appendix: Contract Summary (Copilot-ready)

- MFA required? **Always for admin roles**. Two-step login using `challenge_id` + `otpCode`.
- Refresh contract? Access JWT TTL=10m; Refresh opaque TTL=14d; **rotate refresh every use**; replay detection revokes session family.
- Canonical enums?
  - status: ACTIVE, PENDING_VERIFICATION, SUSPENDED, BANNED, DELETED
  - role: USER, MODERATOR, ADMIN, SUPER_ADMIN
  - ageGroup: UNDER_13, AGE_13_17, AGE_18_24, AGE_25_34, AGE_35_44, AGE_45_PLUS, UNKNOWN
- Event dedupe? Primary: `eventId` unique. Secondary: server `eventHash` for monitoring.
- Notifications/config? **Hybrid** with clear precedence rules.
