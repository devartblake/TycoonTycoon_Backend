# Synaptix Alpha/Beta Release Stabilization Plan
## TycoonTycoon Backend Production Narrowing Strategy
### Target Window: Alpha/Beta Release Before June 1, 2026

---

# Executive Summary

This document outlines a practical and technically grounded strategy for reducing platform complexity, stabilizing the backend, and preparing Synaptix/TycoonTycoon for an Alpha/Beta release.

The primary objective is NOT to complete the entire platform.

The primary objective IS to:

```text
Allow real users to:
- Register/Login
- Play core trivia gameplay
- Earn XP/coins safely
- Appear on leaderboards
- Use the application consistently without data corruption
```

This strategy preserves previously built systems while reducing operational and development risk through backend-owned feature flags and controlled scope reduction.

---

# 1. Current Technical Assessment

The backend architecture is already approaching a multiplayer platform architecture rather than a simple trivia API.

## Existing Technology Stack

| Layer | Technology |
|---|---|
| Gateway | YARP |
| Backend APIs | ASP.NET Core (.NET 8/.NET 10) |
| Database | PostgreSQL |
| Cache | Redis |
| Messaging | RabbitMQ / Redis Streams |
| Background Jobs | Hangfire |
| Realtime | SignalR |
| Storage | MinIO |
| Analytics/AI | FastAPI Sidecar |
| Monitoring | OpenTelemetry + Grafana + Prometheus |
| Frontend | Flutter |

---

# 2. Primary Technical Risk

The primary technical risk is:

## OVER-SCOPING

Too many systems are evolving simultaneously:
- multiplayer
- ToM personalization
- analytics
- tournaments
- crypto
- social systems
- realtime
- skill trees
- missions
- dashboards
- experimentation systems

This creates:
- release instability
- slower testing
- migration complexity
- deployment coupling
- debugging difficulty
- increased operational burden

---

# 3. Alpha/Beta Release Objective

The Alpha/Beta release should focus ONLY on the core gameplay loop.

## Golden Path

```text
Register/Login
â Load Profile
â Load Wallet
â Start Trivia Session
â Answer Questions
â Submit Results
â Grant XP/Coins
â Update Leaderboard
â Return to Home Screen
```

If this path is stable:
- the release succeeds.

If this path is unstable:
- the platform is not ready.

---

# 4. Production Scope Reduction Strategy

## Keep Enabled

These systems remain active for Alpha/Beta.

| System | Status |
|---|---|
| Authentication | ENABLED |
| User Profiles | ENABLED |
| Wallet System | ENABLED |
| Core Trivia Gameplay | ENABLED |
| Match Result Submission | ENABLED |
| XP/Coin Rewards | ENABLED |
| Leaderboards | ENABLED |
| Basic Store Catalog | OPTIONAL |
| Basic Admin Dashboard | ENABLED |
| Health Checks | ENABLED |
| Logging/Monitoring | ENABLED |
| PostgreSQL | ENABLED |
| Redis | ENABLED |
| MinIO | ENABLED only if required |

---

# 5. Systems To Disable Via Feature Flags

These systems should remain in the repository but be disabled.

| Feature | Feature Flag | Default |
|---|---|---|
| Realtime Multiplayer | realtime_multiplayer_enabled | false |
| Ranked Matchmaking | matchmaking_enabled | false |
| Tournaments | tournaments_enabled | false |
| Advanced Seasons | advanced_seasons_enabled | false |
| Crypto Systems | crypto_enabled | false |
| ToM Personalization | tom_personalization_enabled | false |
| AI Sidecar Scoring | ai_sidecar_enabled | false |
| Friends/Social | social_enabled | false |
| Guilds/Clans | guilds_enabled | false |
| Advanced Skill Tree | skill_tree_enabled | false |
| Notifications | notifications_enabled | false |
| Experiments/A-B Tests | experiments_enabled | false |
| Territory Systems | territory_enabled | false |
| Guardian Systems | guardians_enabled | false |

---

# 6. Feature Flag Architecture

## Core Principle

Feature flags must be:
- backend-owned
- centrally controlled
- enforced server-side

The Flutter frontend must NEVER decide availability independently.

---

# 7. Recommended Backend Config Endpoint

## Endpoint

```http
GET /api/v1/app/config
```

## Example Response

```json
{
  "environment": "beta",
  "minimumClientVersion": "0.1.0",
  "features": {
    "coreTriviaEnabled": true,
    "walletEnabled": true,
    "leaderboardEnabled": true,
    "storeEnabled": true,

    "missionsEnabled": false,
    "skillTreeEnabled": false,
    "realtimeMultiplayerEnabled": false,
    "matchmakingEnabled": false,
    "tournamentsEnabled": false,
    "cryptoEnabled": false,
    "tomPersonalizationEnabled": false,
    "socialEnabled": false,
    "notificationsEnabled": false
  }
}
```

---

# 8. Backend Enforcement Rules

Frontend flags only hide UI.

The backend MUST also reject disabled endpoints.

## Example

```json
{
  "error": "FeatureDisabled",
  "feature": "tournamentsEnabled",
  "message": "This feature is not available in the current release."
}
```

Recommended status:
- HTTP 403

---

# 9. Recommended Backend Implementation

## Feature Flag Options

### Example

```csharp
public sealed class FeatureFlagsOptions
{
    public bool CoreTriviaEnabled { get; set; }
    public bool WalletEnabled { get; set; }
    public bool LeaderboardEnabled { get; set; }

    public bool MatchmakingEnabled { get; set; }
    public bool RealtimeMultiplayerEnabled { get; set; }
    public bool TournamentsEnabled { get; set; }

    public bool CryptoEnabled { get; set; }
    public bool SocialEnabled { get; set; }
    public bool SkillTreeEnabled { get; set; }
}
```

---

# 10. Recommended Frontend Integration

Flutter should:
- load `/app/config` during startup
- cache feature flags
- expose flags via Riverpod providers
- hide routes/screens/buttons based on config

## Example

```dart
final featureFlagsProvider = Provider<FeatureFlags>((ref) {
  return ref.watch(appConfigProvider).features;
});
```

---

# 11. Alpha/Beta MVP Scope

## Required Systems

### Authentication
- register
- login
- refresh tokens

### Profile
- profile load
- avatar
- XP level

### Wallet
- coins
- XP
- diamonds

### Gameplay
- solo trivia sessions
- question loading
- answer validation
- match result submission

### Progression
- XP rewards
- coin rewards
- leaderboard updates

### Admin
- health monitoring
- player visibility
- logs/errors

---

# 12. Optional MVP Systems

Optional systems that may remain if already stable.

| Feature | Status |
|---|---|
| Store Catalog | OPTIONAL |
| Daily Missions | OPTIONAL |
| Limited Skill Tree | OPTIONAL |
| Basic Presence | OPTIONAL |

---

# 13. Explicitly Excluded Systems

These systems should NOT block release.

```text
- Full realtime multiplayer
- Ranked matchmaking
- Tournament engine
- Crypto rewards
- ToM systems
- AI personalization
- Friends/chat
- Guilds
- Territory systems
- Complex analytics
- Experiment systems
- Advanced notifications
```

---

# 14. Recommended Release Branch Strategy

## Create Dedicated Release Branch

```bash
git checkout -b release/alpha-june-2026
```

---

# 15. Required Release Documentation

Create the following documents:

```text
/docs/releases/
 âââ ALPHA_ENABLED_FEATURES.md
 âââ ALPHA_DISABLED_FEATURES.md
 âââ ALPHA_RELEASE_CRITERIA.md
 âââ ALPHA_KNOWN_ISSUES.md
 âââ ALPHA_ROLLBACK_PLAN.md
```

---

# 16. Recommended Development Freeze

## Effective Immediately

NO:
- new major systems
- new infrastructure
- new game modes
- new dashboards
- new database technologies

ONLY:
- stabilization
- bug fixing
- release hardening
- core gameplay verification

---

# 17. Stability Priorities

## Highest Priority

### 1. Reward Idempotency

Prevent:
- duplicate XP grants
- duplicate coin grants
- double result submissions

---

### 2. Migration Stability

Prevent:
- manual patching in production
- migration drift
- inconsistent schema states

---

### 3. Session Stability

Prevent:
- invalid token loops
- stale sessions
- wallet/profile desync

---

### 4. Error Recovery

Flutter must gracefully handle:
- backend unavailable
- question load failures
- expired auth
- leaderboard failures

---

# 18. Operational Requirements

## Required Monitoring

| Requirement | Status |
|---|---|
| Health checks | REQUIRED |
| Structured logs | REQUIRED |
| Error dashboards | REQUIRED |
| DB backups | REQUIRED |
| Redis monitoring | REQUIRED |
| API latency tracking | REQUIRED |

---

# 19. Testing Requirements

## Minimum Required Testing

### Backend
- auth tests
- wallet tests
- reward idempotency tests
- migration tests

### Frontend
- login flow
- offline handling
- retry behavior
- feature flag behavior

### Integration
- full golden-path test

---

# 20. Recommended Timeline

# Phase 1 â Scope Freeze (Immediate)

## Goals
- freeze new features
- define Alpha scope
- implement release branch

---

# Phase 2 â Feature Flag System (1â2 Days)

## Goals
- backend flags
- frontend config
- route hiding
- endpoint enforcement

---

# Phase 3 â Golden Path Stabilization (3â5 Days)

## Goals
- login stability
- trivia gameplay
- rewards
- leaderboard consistency

---

# Phase 4 â Data & Migration Hardening (2â4 Days)

## Goals
- migration cleanup
- seed validation
- wallet consistency

---

# Phase 5 â Operational Hardening (2â3 Days)

## Goals
- logging
- health checks
- dashboards
- backups

---

# Phase 6 â Alpha/Beta Validation (Final Week)

## Goals
- internal testing
- small external user testing
- crash validation
- deployment verification

---

# 21. Recommended Immediate Priorities

## Priority 1

Implement:
- backend feature flags
- frontend feature gating

---

## Priority 2

Stabilize:
- auth
- wallet
- leaderboard
- solo gameplay

---

## Priority 3

Disable:
- unfinished systems
- unstable endpoints
- experimental flows

---

## Priority 4

Verify:
- reward safety
- migration safety
- deployment repeatability

---

# 22. Realistic June 1 Outcome

The realistic target is:

```text
A stable Alpha/Beta release where:
- users can play
- progression works
- rewards are safe
- leaderboards update
- backend remains stable
```

NOT:
- a fully mature multiplayer platform

---

# 23. Final Strategic Recommendation

The correct strategy is:

```text
Preserve all systems.
Disable most systems.
Ship the core gameplay loop.
Collect operational data.
Re-enable systems gradually.
```

Do NOT continue expanding platform scope before proving:
- retention
- gameplay stability
- reward integrity
- operational reliability

The platform foundation is already strong enough.

The immediate requirement is discipline and scope control.

---

# 24. Flutter Frontend Implementation Status

**Last updated: 2026-05-16**

## Completed ✅

### Blocker 1 — Feature Flag System
- `AppConfig` + `FeatureFlags` model with `fromJson` factory (`lib/core/models/app_config.dart`)
- `appConfigProvider` (`FutureProvider<AppConfig>`) fetches `GET /api/v1/app/config` on startup
- `featureFlagsProvider` (synchronous derived `Provider<FeatureFlags>`) — safe default-off while loading
- `FeatureDisabledException` type + HTTP 403 `FeatureDisabled` detection in `ApiService`
- `featureFlagGuard()` helper (synchronous, BuildContext-safe for chaining with `onboardingGuard`)
- Route gating applied to: all multiplayer routes, crypto wallet, social/friends/messages, skill tree (20+ routes)

### Blocker 2 — Wallet Backend Sync
- Startup sync: `walletSyncProvider` activated in `SynaptixApp.didChangeDependencies` when logged in
- `walletProvider` fetches `GET /users/me/wallet` and mirrors to all local currency providers on load
- Post-quiz refresh: `ref.invalidate(walletProvider)` added to `ProfileDataUpdater.updateAfterQuiz()` so wallet re-fetches after every quiz

### Blocker 3 — Solo Quiz Result Submission
- Fire-and-forget `POST /leaderboard` call added to `ProfileDataUpdater.updateAfterQuiz()` using `currentUserIdProvider` + `leaderboardControllerProvider`
- Same error-swallowing pattern as the existing pity system — app never blocked by network failure
- Closes golden path step 6: Update Leaderboard

### Blocker 4 — Health Check + Minimum Version Check on Startup
- `package_info_plus` added to dependencies
- `_runStartupChecks()` added to `SynaptixApp` — called after crash recovery phase completes
- Calls `SynaptixApiClient.healthCheck()` (`GET /health`) — shows "Cannot reach server" screen with Retry on failure
- Reads `appConfigProvider.minimumClientVersion`, compares with installed version via `PackageInfo.fromPlatform()`
- Shows non-dismissible "Update Required" dialog if client is outdated

### Blocker 5 — Register Route
- `startInSignUpMode: bool` parameter added to `LoginScreen` (web) and `LoginScreenMobile`
- `/register` GoRoute added to `app_router.dart` — opens login screen in signup mode (`_isSignUpMode = true`)
- No additional screen widget needed — signup UI was already embedded in the login screen

### Additional Completions
- CMake generator mismatch fixed (`windows/CMakeLists.txt` policy range updated)
- `games_services` API corrected: `getPlayerID()` not `getPlayerId()` (4.1.1 breaking change)
- 4 categories of test compile errors fixed (MapEntry access, StateNotifier subclassing, const DateTime, Answer model)
- `/multiplayer/rooms/:roomId` parameterized route enabled with auto-join on mount

---

## Remaining — Flutter Frontend ⏳

| Item | Priority | Notes |
|---|---|---|
| Reward idempotency client-side guard | Medium | Prevent double-call to `updateAfterQuiz` on screen re-entry; backend idempotency via `POST /quiz/complete` is complete |
| Live smoke test against staging | Required before Alpha launch | `test/integration/live_backend_smoke_test.dart` exists; run against migrated staging env |

---

## Golden Path Status

```text
Register/Login         ✅ /register + /login routes, BackendAuthService.signup()
Load Profile           ✅ ProfileSyncService.fetchRemoteProfile() on startup
Load Wallet            ✅ walletProvider + walletSyncProvider active on login
Start Trivia Session   ✅ question_loader_service.dart, quiz state machine
Answer Questions       ✅ AdaptedQuizNotifier, encrypted session storage
Submit Results         ✅ POST /leaderboard (score) — backend reward endpoint TBD
Grant XP/Coins         ✅ Local grants + post-quiz wallet refresh from backend
Update Leaderboard     ✅ LeaderboardController.submitScore() called after quiz
Return to Home         ✅ Router navigates to /home on completion
```

---

# 25. Backend System Implementation Status

**Last updated: 2026-05-16**

## Completed ✅

### Authentication
- `POST /auth/register` — email registration
- `POST /auth/signup` — combined register + login for mobile
- `POST /auth/login` — email + password login
- `POST /auth/refresh` — JWT token refresh
- `POST /auth/logout` — session termination
- JWT Bearer authentication with role-based access control (RBAC)
- Admin login via `POST /admin/auth/login` with email allowlist (`AdminEmailAcl`)

### User Profiles
- `Player` entity with profile fields, avatar, XP level
- Profile sync on startup via `ProfileSyncService`
- Avatar customization endpoints (`/avatars`)

### Wallet System
- `PlayerWallet` entity (XP, Coins, Diamonds currencies)
- `EconomyTransaction` + `EconomyTransactionLine` double-entry ledger
- `PlayerEconomySafeguardState` abuse detection
- `GET /users/me/wallet` — wallet retrieval

### Core Trivia Gameplay
- Question loading endpoints (`/questions`, `/questions/bootstrap`)
- Answer grading and validation
- Study sessions (`/study`) and study sets
- Quiz state machine (question loader service)
- `ProcessedGameplayEvent` entity for deduplication infrastructure

### Match Result Submission
- `Match`, `MatchResult`, `MatchParticipantResult` entities
- `POST /matches` — match creation and submission

### XP/Coin Rewards
- `EconomyTransaction` reward system
- `POST /leaderboard` — score submission (fires after every quiz)
- Hangfire leaderboard recalculation job (daily 05:00 UTC)

### Leaderboards
- Tier-based leaderboards with pagination (`/leaderboards/tiers/{tierId}`)
- Player rank lookup (`/leaderboards/me/{playerId}`)
- 6 tiers seeded (Neural Initiate → Synaptix Prime)

### Health Checks
- `GET /healthz` — simple liveness check
- `GET /health/ready` — readiness probe with dependency status
- `GET /` — service info with feature availability

### Logging / Monitoring
- Serilog structured logging throughout all projects
- OpenTelemetry distributed tracing with OTLP exporter
- `AdminSecurityAudit` and `AdminSecurityMetrics` for security auditing
- Hangfire dashboard at `/hangfire` (development)

### Admin Dashboard
- 20+ admin endpoint groups: users, questions, economy, seasons, anti-cheat, moderation, analytics, notifications, store, personalization, experiments
- `GET /api/v1/admin/config` — feature flag read
- `PATCH /api/v1/admin/config` — feature flag update

### Feature Flag Infrastructure
- `FeatureFlagService` (`Tycoon.Backend.Application/Config/FeatureFlagService.cs`)
- `AdminAppConfig` entity — stores flags as JSON in `FeatureFlagsJson` column
- Group-level `AddEndpointFilter` gates enforced across all 14 user-facing systems (all return HTTP 403 `FeatureDisabled`)
- SignalR hubs gated via path-based middleware (`/ws/*`) in `Program.cs`
- All flags togglable at runtime via `PATCH /api/v1/admin/config` with no restart

### Migration Safety
- `pg_advisory_lock(987654321)` / `pg_advisory_unlock` in `MigrationWorker.ExecuteAsync` — prevents concurrent migrator container runs
- `artifacts/migrations/rollback-notes.md` — all 24 migrations documented with data risk assessment and rollback steps
- `artifacts/migrations/idempotent.sql` — generated in CI for every `main` branch push

### Release Documentation
- `docs/releases/ALPHA_ENABLED_FEATURES.md` — full list of active Alpha endpoints
- `docs/releases/ALPHA_DISABLED_FEATURES.md` — all 9+ flag-gated systems with reasons
- `docs/releases/ALPHA_RELEASE_CRITERIA.md` — must-pass / should-pass checklist for launch sign-off
- `docs/releases/ALPHA_KNOWN_ISSUES.md` — P1/P2 issues with mitigations
- `docs/releases/ALPHA_ROLLBACK_PLAN.md` — Level 1 (flag), Level 2 (container), Level 3 (DB restore) procedures

### Release Gate CI
- `.github/workflows/release-gate.yml` — automated gate: verifies migration artifact → API health smoke → feature flag audit → readiness report to `GITHUB_STEP_SUMMARY` + uploaded as `release-gate-report-<env>-<run_number>` artifact (retained 90 days)

### Quiz Completion Rewards
- `POST /quiz/complete` — authoritative server-side XP/coin grant with idempotency
- `CompleteQuizHandler` uses `EconomyService.ApplyAsync` (idempotent via `EventId` unique index)
- `ProcessedGameplayEvent` recorded for mission-tracking deduplication
- Rate-limited via `matches-submit` policy (10 req/10s)

### Public App Config
- `GET /api/v1/app/config` — unauthenticated; serves `minimumClientVersion` + all 14 feature flags
- Reads from `AdminAppConfig.FeatureFlagsJson`; falls back to safe defaults (alpha-off for disabled systems)
- `AppConfig:MinimumClientVersion` settable in `appsettings.json`

### Rate Limiting
- `api` policy: 100 req/min per IP
- `matches-submit`: 10 req/10s per user
- `admin-auth-login`: 5 req/min per IP

### Background Jobs (Hangfire)
- Leaderboard recalculation: `0 5 * * *` (daily 05:00 UTC)
- Guardian assignment: `0 2 * * *` (daily 02:00 UTC)
- Admin notification dispatch: every 1 minute
- Game event scheduler: every 1 minute

### Infrastructure
- PostgreSQL with 24 applied EF Core migrations (through `20260515102821_AddMayCutoverSchemaSync`)
- Redis (cache + SignalR backplane)
- MinIO (object storage, seed catalog)
- RabbitMQ (messaging)
- SignalR hubs: `/ws/match`, `/ws/presence`, `/ws/notify` — gated via `realtime_multiplayer_enabled` inline middleware in `Program.cs`

---

## Partial ⚠️

### Feature Flag Enforcement Gap (14 of 14 enforced — complete)
- **All 14 Alpha/Beta feature flag gates are now enforced** server-side with HTTP 403 `FeatureDisabled`
- Fully gated systems: game events, guardian, territory, matchmaking, friends, social messages (`social_enabled`), party (`social_enabled`), experiments, personalization, crypto, skill tree, notifications, ML/AI sidecar, realtime multiplayer (SignalR `/ws/*` middleware gate)
- Remaining partial coverage: Tournaments and Advanced Seasons have no dedicated flag yet (no endpoint group exists; controlled by the `realtime_multiplayer_enabled` gate at the matchmaking level)

---

## Remaining — Backend ⏳

| Item | Priority | Notes |
|---|---|---|
| Live smoke test against staging | Required before Alpha | `test/integration/live_backend_smoke_test.dart` exists; blocked on stable staging environment with applied migrations and network access |
| Alpha release sign-off | Required | Complete checklist in `docs/releases/ALPHA_RELEASE_CRITERIA.md`; all Must Pass items green; four-role sign-off obtained |

---

## Golden Path Backend Status

```text
Register/Login         ✅ POST /auth/register, /auth/signup, /auth/login
Load Profile           ✅ Player entity + profile sync on startup
Load Wallet            ✅ GET /users/me/wallet — PlayerWallet entity
Start Trivia Session   ✅ GET /questions/bootstrap — question loader
Answer Questions       ✅ Answer grading, quiz state machine
Submit Results         ✅ POST /leaderboard (score) + POST /quiz/complete (authoritative reward grant)
Grant XP/Coins         ✅ POST /quiz/complete — EconomyService.ApplyAsync idempotent grant; ProcessedGameplayEvent deduplication
Update Leaderboard     ✅ Hangfire recalculation job + POST /leaderboard score record
Return to Home         ✅ (frontend-driven; backend is stateless between sessions)
```
