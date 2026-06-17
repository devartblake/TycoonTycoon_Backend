# Alpha Release — Enabled Features

**Release:** alpha-beta-2026  
**Last updated:** 2026-06-16

All features listed here are active and accessible on the Alpha backend. Feature flags default to `true` for these systems; they may be toggled off without a deployment via `PATCH /api/v1/admin/config`.

---

## Authentication

| Endpoint | Description |
|----------|-------------|
| `POST /auth/register` | Email registration (creates `Player` + `PlayerWallet`) |
| `POST /auth/signup` | Combined register + login for mobile clients |
| `POST /auth/login` | Email + password login; returns JWT access + refresh tokens |
| `POST /auth/refresh` | JWT refresh token rotation |
| `POST /auth/logout` | Session termination; revokes refresh token |

JWT Bearer authentication with role-based access control (RBAC). Admin login at `POST /admin/auth/login` with email allowlist enforcement.

---

## User Profile

| Endpoint | Description |
|----------|-------------|
| `GET /users/me` | Authenticated player profile |
| `PATCH /users/me` | Update display name, bio, social links |
| `GET /avatars` | Available avatar catalog |
| `POST /avatars/equip` | Equip a player avatar |

Player entity includes: display name, avatar URL, XP level, handle, and preferences.

---

## Wallet System

| Endpoint | Description |
|----------|-------------|
| `GET /users/me/wallet` | Current wallet balances (XP, Coins, Diamonds) |

- `PlayerWallet` entity with three currency types
- `EconomyTransaction` + `EconomyTransactionLine` double-entry ledger
- `PlayerEconomySafeguardState` abuse detection
- All wallet grants are idempotent via `EventId` unique index

---

## Core Trivia Gameplay

| Endpoint | Description |
|----------|-------------|
| `GET /questions/bootstrap` | Load question set for a quiz session |
| `GET /questions` | Paginated question catalog |
| `POST /study` | Create a study session |
| `GET /study/{sessionId}` | Study session state |

Question loading, answer grading, study sessions with session mode support (solo, ranked, study). Question difficulty and tag filtering supported.

---

## Quiz Completion Rewards

| Endpoint | Description |
|----------|-------------|
| `POST /quiz/complete` | Authoritative server-side XP/coin grant with idempotency |

- Idempotent via `EconomyService.ApplyAsync` (checks `EventId` unique index before granting)
- `ProcessedGameplayEvent` recorded for mission-tracking deduplication
- Rate-limited via `matches-submit` policy (10 requests/10 seconds per player)
- Returns updated wallet balances (`BalanceXp`, `BalanceCoins`, `BalanceDiamonds`)

---

## Match Result Submission

| Endpoint | Description |
|----------|-------------|
| `POST /matches` | Submit match result |
| `GET /matches/{matchId}` | Match result lookup |
| `POST /leaderboard` | Score submission (fire-and-forget after quiz) |

`Match`, `MatchResult`, `MatchParticipantResult` entities. Score submission triggers async leaderboard update via Hangfire.

---

## Leaderboards

| Endpoint | Description |
|----------|-------------|
| `GET /leaderboards/tiers/{tierId}` | Tier leaderboard with pagination |
| `GET /leaderboards/me/{playerId}` | Player rank lookup |

Six tiers seeded: Neural Initiate → Synaptix Prime. Hangfire recalculation job runs daily at 05:00 UTC.

---

## Missions

| Endpoint | Description |
|----------|-------------|
| `GET /missions` | Active mission list for player |
| `POST /missions/{missionId}/claim` | Claim completed mission reward |

Daily and weekly missions with XP/Coin rewards. Mission progress tracked via `ProcessedGameplayEvent`. Claims are idempotent.

---

## Arcade Spins

| Endpoint | Description |
|----------|-------------|
| `POST /spins/arcade` | Trigger arcade spin |
| `GET /spins/arcade/status` | Current spin availability |

Arcade spin claims tracked in `ArcadeSpinClaims` table. Spin availability resets on daily schedule.

---

## App Configuration

| Endpoint | Description |
|----------|-------------|
| `GET /api/v1/app/config` | **Unauthenticated** — returns `minimumClientVersion` + all 14 feature flags |

Called by the Flutter client on every app launch. Reads from `AdminAppConfig.FeatureFlagsJson`; falls back to safe defaults if no config row exists. `AppConfig:MinimumClientVersion` is configurable in `appsettings.json`.

---

## Health Checks

| Endpoint | Description |
|----------|-------------|
| `GET /healthz` | Simple liveness check |
| `GET /health/ready` | Readiness probe with dependency status |
| `GET /` | Service info with feature availability |

---

## Admin Dashboard

Full admin suite at `/api/v1/admin/` covering:
- User management, banning, moderation
- Question management (create, approve, reject)
- Economy transactions and wallet inspection
- Season management and reward rules
- Anti-cheat analytics and flag management
- Feature flag management: `GET /api/v1/admin/config`, `PATCH /api/v1/admin/config`
- Notification dispatch
- Store catalog management
- Personalization and experiment admin views

---

## Compliance (COPPA / CCPA / PCI)

Dedicated `Synaptix.Compliance` microservice (port 5070) connected to the main backend via typed HTTP client with service-token authentication.

| Endpoint | Description |
|----------|-------------|
| `POST /compliance/age-verification` | Declare age; creates `AgeVerification` record |
| `POST /compliance/consent` | Record consent (ToS, Privacy, Marketing, Analytics, DoNotSell) |
| `GET /compliance/consent/status` | Current consent state for authenticated user |
| `POST /compliance/parental-consent/initiate` | Request parent consent (user-initiated) |
| `GET /compliance/parental-consent/status` | Parental consent status |
| `POST /compliance/parental-consent/verify` | Parent verifies via email token |
| `POST /compliance/privacy-requests` | Submit CCPA/COPPA request (Know, Delete, OptOut, DataPortability) |
| `POST /users/me/parental-consent/request` | Main backend: initiate consent + dispatch parent email |

**Compliance service** enforces:
- Age-based feature restrictions (COPPA under-13 gates)
- Parental consent workflow with 72-hour expiry tokens and email delivery
- Privacy request intake (CCPA right-to-know, delete, opt-out, data portability)
- Compliance audit trail for all COPPA/CCPA/PCI-relevant events
- Hangfire fulfillment job: polls pending requests every 15 min; anonymises PII (preserving financial records), exports data to MinIO, or records opt-out

---

## Purchase History

| Endpoint | Description |
|----------|-------------|
| `GET /users/me/purchases` | Paginated purchase history (store, IAP, Stripe, PayPal kinds) |

---

## Admin — Privacy Fulfillment

| Endpoint | Description |
|----------|-------------|
| `POST /admin/privacy-requests/{id}/process` | Operator-initiated fulfillment of a pending privacy request |

---

## Infrastructure

- PostgreSQL 16 with EF Core migrations (main DB + compliance schema)
- Redis (cache + SignalR backplane — backplane active, hub connections gated)
- MinIO (object storage, seed catalog, compliance data exports)
- RabbitMQ (messaging backbone)
- Hangfire (background job processing, privacy fulfillment every 15 min)
- OpenTelemetry tracing + Serilog structured logging
- Rate limiting: API (100 req/min), match-submit (10/10s), admin-auth (5/min)
- `Synaptix.Compliance` microservice (port 5070, service-token auth, auto-migrate on startup)
