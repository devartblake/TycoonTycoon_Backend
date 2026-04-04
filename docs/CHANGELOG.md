# Changelog — `claude/add-minio-docker-QBFUx`

All changes made on this branch relative to `main`.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Tycoon.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Tycoon.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Tycoon.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Tycoon.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-17] Operator Dashboard — Full Feature Expansion

Expanded `Tycoon.OperatorDashboard` from 7 foundation pages to a complete ops control panel with 12 pages, 46 AdminApiClient methods, and grouped navigation.

### New Pages

#### `Questions.razor`
- Full question bank management: list with search + category filter, paginated results
- Create/edit question form (body, option A–D, correct answer, category, difficulty)
- Delete individual questions, bulk JSON import via textarea paste
- `AdminApiClient`: `ListQuestionsAsync`, `CreateQuestionAsync`, `UpdateQuestionAsync`, `DeleteQuestionAsync`, `BulkImportQuestionsAsync`

#### `Notifications.razor`
- Tabbed interface: **Send**, **Scheduled**, **Dead-Letter Queue**, **Templates**, **History**
- Send immediate notification to any channel with title/body/metadata JSON
- Schedule future notifications with ISO-8601 send-at time, cancel scheduled items
- Replay failed dead-letter deliveries
- Template CRUD (create with key/title/body, delete)
- History search by channel key and delivery status
- `AdminApiClient`: `ListChannelsAsync`, `SendNotificationAsync`, `ScheduleNotificationAsync`, `ListScheduledAsync`, `CancelScheduledAsync`, `GetDeadLetterAsync`, `ReplayDeadLetterAsync`, `ListTemplatesAsync`, `CreateTemplateAsync`, `DeleteTemplateAsync`, `GetNotificationHistoryAsync`

#### `AntiCheat.razor`
- Tabbed interface: **Player Flags**, **Party Flags**, **Analytics**
- Review queue with severity filter; reviewer name + note fields; mark flags reviewed
- Party exploit flags with same review workflow
- Analytics summary for configurable window (6 / 12 / 24 / 48 h)
- `AdminApiClient`: `ListAnticheatFlagsAsync`, `ReviewAnticheatFlagAsync`, `GetAnticheatSummaryAsync`, `ListPartyFlagsAsync`, `ReviewPartyFlagAsync`

#### `AuditLog.razor`
- Security audit log with date-range + status filters
- Paginated results: timestamp, admin, action, IP, status
- `AdminApiClient`: `GetSecurityAuditAsync`

#### `Matches.razor`
- Paginated list of recent matches: ID, mode, host, status, player count, timestamps
- `AdminApiClient`: `ListMatchesAsync`

### Enhanced Pages

#### `Moderation.razor` — expanded
- Added **Player Profile** tab: look up by UUID, display current status/reason/expiry
- Added **Set Status** form: Mute / Warning / Suspended / Banned + reason + notes + optional expiry timestamp
- Added **Action Logs** tab: search historical moderation actions by player ID
- `AdminApiClient`: `GetModerationProfileAsync`, `SetModerationStatusAsync`, `GetModerationLogsAsync`

#### `Economy.razor` — expanded
- Added **Player Wallet** tab: look up wallet balances by player ID
- Added **Grant** tab: grant coins / XP / premium currency to any player
- Added **Transaction History** tab: paginated ledger lookup by player ID
- `AdminApiClient`: `GetPlayerEconomyHistoryAsync`, `CreateTransactionAsync`

#### `Seasons.razor` — expanded
- Added **Reward Claims** tab: search reward claims by season ID and/or player ID
- Added **Recompute Tiers** button on active seasons (purple accent)
- `AdminApiClient`: `GetRewardClaimsAsync`, `ForceRecomputeAsync`

### Navigation Overhaul (`MainLayout.razor`)
Grouped sidebar navigation with four sections:
- **Content** — Questions, Notifications
- **Operations** — Seasons, Game Events, Feature Flags
- **Players** — Users, Moderation, Economy, Anti-Cheat
- **Audit** — Matches, Security Log

### AdminApiClient Completion
All 46 typed methods now implemented. Full coverage of every admin API domain:
Auth (2), Config (2), Seasons (3+2 rewards), Event Queue (3), Users (3), Moderation (4), Economy (3), Questions (5), Notifications (11), Anti-Cheat (5), Matches (1), Security Audit (1).

---

## [2026-03-17] Operator Dashboard + FastAPI Sidecar

Adds two new services to the solution:

### `Tycoon.OperatorDashboard` — Blazor Server operator control panel
- Browser UI for the ops team — no more Swagger/Postman for day-to-day operations
- **Pages:** Dashboard, Seasons (activate/close), Game Events (open/close), Feature Flags (toggle on/off), Users (ban/unban), Moderation (escalation list), Economy (overview)
- Authenticates against `/admin/auth/login`; JWT stored server-side (`TokenStore`) — never sent to the browser
- Typed `AdminApiClient` wraps all admin REST endpoints; attaches `Authorization: Bearer` + `X-Admin-Ops-Key` header
- Registered in `Tycoon.AppHost` with Aspire service discovery (`WithReference(api)`)
- Docker: `docker/Dockerfile.dashboard`, exposed on port `8200`

### `Tycoon.Sidecar` — FastAPI Python service
- `/ml` — match quality scoring, churn risk prediction, question difficulty estimation
- `/analytics` — season KPIs, event funnel, D1/D7/D30 retention (wire up Motor/Elasticsearch)
- `/webhooks` — Stripe payments, generic signed webhooks, push notification proxy
- `/utilities` — season snapshot to MongoDB, bulk question import, backend health probe
- Registered in `Tycoon.AppHost` via `AddExecutable("tycoon-sidecar", "uvicorn", ...)` on port `8100`
- Docker: `docker/Dockerfile.sidecar`, exposed on port `8100`

### Infrastructure changes
- `Tycoon.AppHost/Program.cs` — registers `tycoon-dashboard` and `tycoon-sidecar`
- `docker/compose.yml` — adds `sidecar` and `operator-dashboard` services
- `TycoonTycoon_Backend.slnx` — `Tycoon.OperatorDashboard` added under `/Hosting/`

---

## [2026-03-16] Feature Flag Activation Controls (Part A)

Adds runtime on/off toggles for the three game modes (Game Events, Guardians, Territory) without requiring Hangfire dashboard access or redeployment.

### New Service: `FeatureFlagService`
- **File:** `Tycoon.Backend.Application/Config/FeatureFlagService.cs`
- Scoped per-request; lazy-loads `AdminAppConfig.FeatureFlagsJson` once per scope
- Missing keys default to `true` (safe for zero-downtime rollouts)
- Constants: `GameEventsEnabled = "game_events_enabled"`, `GuardianEnabled = "guardian_enabled"`, `TerritoryEnabled = "territory_enabled"`

### Guards Added
| Component | Flag checked | Early-return status |
|---|---|---|
| `GameEventSchedulerJob` (Hangfire) | `game_events_enabled` | Skips entire job |
| `EnterGameEvent` handler | `game_events_enabled` | `"FeatureDisabled"` → HTTP 503 |
| `ChallengeGuardian` handler | `guardian_enabled` | `"FeatureDisabled"` → HTTP 503 |
| `GuardianAssignmentJob` (Hangfire) | `guardian_enabled` | Skips entire job |
| `StartTerritoryDuel` handler | `territory_enabled` | `"FeatureDisabled"` → HTTP 503 |

### API Layer Changes
- `GameEventsEndpoints` — `"FeatureDisabled"` mapped to HTTP 503
- `GuardiansEndpoints` — `"FeatureDisabled"` mapped to HTTP 503
- `TerritoryEndpoints` — `"FeatureDisabled"` mapped to HTTP 503

### Admin Config Defaults
- `AdminConfigEndpoints.GetOrCreate()` now seeds `game_events_enabled=true`, `guardian_enabled=true`, `territory_enabled=true` on first startup

### How to toggle
```http
PATCH /admin/config
Content-Type: application/json

{ "featureFlags": { "game_events_enabled": false } }
```

---

## [2026-03-16] Flutter Frontend Integration Guide

- Created `docs/FLUTTER_INTEGRATION.md` — authoritative Flutter client reference covering:
  - Project setup (recommended packages: `dio`, `signalr_netcore`, `flutter_secure_storage`)
  - Full authentication flow: signup, login, token refresh, secure storage, Dio interceptor
  - Complete REST API reference for all non-admin endpoints grouped by feature
  - Real-time (SignalR) hub setup, group subscriptions, and all 6 server-push event payloads with payload shapes
  - End-to-end feature flows: Game Event battle royale, Guardian Challenge, Territory Capture, Ranked Match
  - Event system activation controls (how seasons and game events are turned on/off, including the gap: no global toggle)
  - Error handling patterns: HTTP codes, domain status strings, recommended Dart error model
  - Rate limit reference table

---

## [2026-03-16] Game Event Tracking System

Adds a separate event analytics layer covering the GameEvent, Guardian, and Territory game modes. Deliberately **not** mixed with the ranked-ladder (`PlayerSeasonProfile` / `LeaderboardEntry`) so tier assignment is never distorted.

### New Entity: `PlayerEventStats`
- **File:** `Tycoon.Backend.Domain/Entities/PlayerEventStats.cs`
- One row per player-season; updated incrementally (no batch recompute job needed)
- Fields:
  - **GameEvent** — `EventsEntered`, `EventsTop20`, `EventsWon`, `TotalEventXpEarned`, `TotalEventCoinsEarned`, `ChampionBattleEliminations`
  - **Guardian** — `GuardianPromotions`, `GuardianDefencesWon`, `GuardianDefencesLost`, `GuardianDaysTotal`
  - **Territory** — `TilesEverCaptured`, `CurrentTilesOwned`, `PeakXpMultiplierBps`

### EF Infrastructure
- **`PlayerEventStatsConfiguration`** — `player_event_stats` table; unique index on `(SeasonId, PlayerId)`; composite indexes on `(SeasonId, EventsWon)`, `(SeasonId, GuardianDefencesWon)`, `(SeasonId, CurrentTilesOwned)`
- **`IAppDb` / `AppDb`** — `DbSet<PlayerEventStats> PlayerEventStats`
- **Migration `20260315000000_AddPlayerEventStats`** — `CREATE TABLE player_event_stats` with all columns and indexes

### New Service: `PlayerEventStatsService`
- **File:** `Tycoon.Backend.Application/EventStats/PlayerEventStatsService.cs`
- `GetOrCreateAsync(seasonId, playerId, ct)` — upsert helper used by all hooks

### New Query Handlers (`Application/EventStats/`)
| Handler | What it answers |
|---|---|
| `GetGameEventLeaderboard(GameEventId, Page, PageSize)` | Ranked participant list for a closed event, ordered by `FinalRank`, with prize amounts |
| `GetPlayerEventHistory(PlayerId, SeasonId?, Page, PageSize)` | All game events a player entered (optionally filtered to a season), with rank and prize outcomes |
| `GetEventSeasonLeaderboard(SeasonId, SortBy, Page, PageSize)` | Season-wide event standings; `SortBy` = `event_wins` (default), `events_entered`, `guardian_defences`, `tiles_owned` |
| `GetTerritoryDominanceLeaderboard(SeasonId, TierNumber, Top)` | Live top-N tile owners in a tier, aggregated from `TerritoryTile` (no extra table) |

### New DTOs (`Tycoon.Shared.Contracts/Dtos/EventStatsDtos.cs`)
- `EventLeaderboardEntryDto(PlayerId, FinalRank, AwardedXp, AwardedCoins, EliminatedAt?)`
- `PlayerEventHistoryDto(GameEventId, Kind, FinalRank?, AwardedXp, AwardedCoins, EnteredAt)`
- `EventSeasonLeaderboardEntryDto(PlayerId, EventsWon, EventsTop20, EventsEntered, GuardianDefencesWon, GuardianDaysTotal, CurrentTilesOwned, PeakXpMultiplierBps)`
- `TerritoryDominanceDto(PlayerId, TilesOwned, TotalXpMultiplierBps)`

### New API Endpoints (`GameEventStatsEndpoints`)
```
GET /game-events/{gameEventId}/leaderboard?page&pageSize
GET /game-events/players/{playerId}/event-history?seasonId&page&pageSize   [Authorized]
GET /game-events/season-leaderboard?seasonId&sortBy&page&pageSize
GET /territory/{seasonId}/{tierNumber}/dominance?top
```

### Incremental Hooks Added to Existing Handlers
| Handler | Stats updated |
|---|---|
| `EnterGameEvent` | `EventsEntered++` |
| `CloseGameEventAndDistributePrizes` | `EventsTop20++`, `EventsWon++` (rank 1), `TotalEventXpEarned +=`, `TotalEventCoinsEarned +=` |
| `ResolveGuardianChallenge` (challenger wins) | `GuardianPromotions++` (challenger), `GuardianDefencesLost++` (deposed guardian) |
| `ResolveGuardianChallenge` (guardian wins) | `GuardianDefencesWon++` |
| `ResolveTerritoryDuel` (challenger wins) | `TilesEverCaptured++`, `CurrentTilesOwned` refresh, `PeakXpMultiplierBps` high-water mark |
| `GuardianAssignmentJob` (daily) | `GuardianDaysTotal++` per active guardian (idempotent — only increments when economy txn is newly applied) |

### DI Registration
- `PlayerEventStatsService` registered as `AddScoped` in `Application/DependencyInjection.cs`

---

## [2026-03-15] MinIO Backend Integration

### `IObjectStorage` Abstraction
- Added `Tycoon.Backend.Application/Abstractions/IObjectStorage.cs`
  - `Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct)`
  - `string GetPublicUrl(string key)`

### Storage Implementations
- **`MinioObjectStorage`** — Minio SDK v6.0.5, auto-creates bucket on first upload, returns configurable public URL
- **`LocalObjectStorage`** — zero-config fallback writing to `wwwroot/`; used in local dev and tests

### `MinioOptions` POCO
Fields: `Endpoint`, `AccessKey`, `SecretKey`, `Bucket` (default `tycoon-assets`), `UseSSL`, `PublicEndpoint`

### Dependency Injection
- `DependencyInjection.cs` selects implementation at startup:
  - MinIO: when `MinIO:Endpoint` is configured
  - Local: fallback (including in-memory/test branch)

### `AdminMediaEndpoints` Refactor
- Upload handler now injects `IObjectStorage` instead of inlining file-write logic

### Config
- `appsettings.Docker.json` — added `MinIO` section (`minio:9000`, `tycoon-assets`, `PublicEndpoint: localhost:9000`)

### NuGet
- `Directory.Packages.props` — `Minio` v6.0.5
- `Tycoon.Backend.Infrastructure.csproj` — `<PackageReference Include="Minio" />`

### Test
- `AdminMediaTests.Upload_Stores_File_And_Returns_AssetKey_And_Url`
  Chains `/admin/media/intent` → `/admin/media/upload/{assetKey}` multipart POST, asserts `200 OK`, correct `assetKey`, non-empty `url`

---

## [2026-03-15] Realtime File Layout Fix

- Moved `ConnectionRegistry.cs` and `SignalRMatchmakingNotifier.cs` from `Features/Realtime/` → `Realtime/` to match their declared namespace `Tycoon.Backend.Api.Realtime`

---

## [2026-03-15] Vote Feature — SignalR Broadcast

- `VoteCastMessage` shared contract (`Topic`, `Option`, `Counts` dictionary)
- `INotificationClient.VoteCast(VoteCastMessage)` method added
- `NotificationHub.JoinTopic(string topic)` — lets clients subscribe to a topic group (`topic:{topic}`)
- `VoteCastEventHandler` — domain event handler that broadcasts to the topic group on every vote

**Frontend integration:** connect to `/ws/notify`, call `JoinTopic(topic)`, listen for `"VoteCast"` events to update live tallies.

---

## [2026-03-15] Vote Feature — Expanded Options

- Valid vote options extended to: `!A`, `!B`, `!C`, `!D`, `!True`, `!False`
  (supports 3-choice, 4-choice, and true/false poll formats)

---

## [2026-03-15] Vote Feature — Domain, Handlers & API

### Domain
- `Vote` aggregate: `PlayerId`, `Option`, `Topic`, `TimestampUtc`; raises `VoteCastEvent`
- `VoteCastEvent` domain event

### Infrastructure
- `VoteConfiguration`: table `votes`, unique index on `(PlayerId, Topic)`, index on `Topic`
- `AppDb` / `IAppDb`: `DbSet<Vote> Votes`

### Application
- `CastVote` handler: validates option, enforces one-vote-per-player-per-topic, returns `Recorded / DuplicateVote / InvalidOption`
- `GetVoteResults` handler: groups by option, returns counts + percentages ordered by count

### API
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/votes` | Required | Cast a vote |
| `GET`  | `/votes/{topic}/results` | Public | Fetch tally |

### Shared Contracts
- `CastVoteRequest`, `CastVoteResponse`, `VoteOptionResult`, `VoteResultsResponse`

> **Note:** run an EF migration to add the `votes` table before deploying.

---

## [2026-03-15] Unit Test Coverage Expansion

### `Tycoon.Backend.Infrastructure.Tests`
| File | What's covered |
|------|---------------|
| `EfRepositoryTests` | `GetAsync` (found / null), Add staging and persistence, multi-entity |
| `IdempotencyIndexModelTests` | Unique index on `EventId` for `ProcessedGameplayEvent`, `EconomyTransaction`, `SeasonPointTransaction`; composite indexes |
| `AppDbDomainEventCollectionTests` | Domain event collection/clearing in `SaveChangesAsync`, multi-aggregate clearing, `MatchCompletedEvent` raised by `Finish()` |

### `Tycoon.Backend.Application.Tests` — Domain
| File | What's covered |
|------|---------------|
| `PlayerTests` | XP/leveling, score clamping, tier idempotency, match result application |
| `MissionClaimTests` | Progress clamping, completion, `MarkClaimed` guard/idempotency, `Reset` |

### `Tycoon.Backend.Application.Tests` — Handlers / Services
| File | What's covered |
|------|---------------|
| `StartMatchHandlerTests` | Match creation, mode defaulting, multi-host isolation |
| `ClaimMissionHandlerTests` | `NotFound / NotCompleted / AlreadyClaimed / Claimed` flows, type filter |
| `ListMissionsHandlerTests` | Active filter, type filter, ordering, DTO mapping |
| `MissionProgressServiceTests` | All mission keys, win/loss branching, multi-mission updates |
| `EconomyServiceTests` | Applied/Duplicate/Invalid/InsufficientFunds, wallet creation, balance accumulation, transaction persistence, history pagination/clamping |
| `SeasonPointsServiceTests` | Applied/Duplicate, profile creation, point accumulation, zero-clamp on negative delta, transaction persistence, `GetActiveSeasonAsync` scenarios |

---

## [2026-03-12] MinIO Docker Setup

### Docker Compose
- `compose.yml` — `minio` service: API port `9000`, console port `9001`, healthcheck, persistent volume, `tycoon-net`
- `compose.prod.yml` — hides ports in production, enforces required password

### Environment Variables
`.env` / `.env.example`:
```
MINIO_ROOT_USER
MINIO_ROOT_PASSWORD
MINIO_PORT        (default 9000)
MINIO_CONSOLE_PORT (default 9001)
```

### Tooling
- `MakeFile` — MinIO added to health check output; new `shell-minio` target
- `Docker.md` — MinIO documented in services table, connection strings, dev URLs, health check reference

---

## [2026-03-12] MinIO Bucket Setup Guide

- Added `docs/minio-setup.md` (278 lines)
  Covers: console access, bucket creation (UI / `mc` CLI / AWS CLI), naming conventions, access policies, upload/download, presigned URLs, .NET SDK connection examples, health check reference

---

## Pending

- ✅ Vote schema migration is already present in `20260319000000_AddGameEventTables` (`votes` table + indexes).
- Operator Dashboard Priority 4 pages: Media upload, Powerups, Skills seeding (planned)
