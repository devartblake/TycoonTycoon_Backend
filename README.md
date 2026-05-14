# Synaptix Backend

A modern, cloud-native backend API built with .NET 10, designed for the scalable Synaptix cognitive competition platform with real-time analytics, robust data persistence, and comprehensive observability.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Authentication & Login](#authentication--login)
- [API Feature Areas](#api-feature-areas)
- [Personalization & Experimentation](#personalization--experimentation)
- [Store Management](#store-management)
- [Sidecar Integration](#sidecar-integration)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Development Workflows](#development-workflows)
  - [Local Development](#local-development-option-a-recommended)
  - [Docker Development](#docker-development-option-b)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Available Services](#available-services)
- [Testing](#testing)
- [Operator Dashboard](#operator-dashboard)
- [Troubleshooting](#troubleshooting)
- [Documentation](#documentation)
- [Contributing](#contributing)

---

## 🎯 Overview

Synaptix Backend provides a complete platform backend infrastructure featuring:

- **🎮 Game State Management**: PostgreSQL-based persistent game state with EF Core 10
- **🧠 Unified Personalization Layer**: Adaptive recommendations, player archetypes, AI Coach, guardrail-protected sidecar scoring
- **🧪 A/B Experimentation**: Built-in experiment assignment, variant weighting, impression and outcome tracking
- **🏪 Store Management**: Flash sales, per-SKU stock policies, purchase limits, avatar catalog with object storage
- **📊 Real-time Analytics**: MongoDB for event tracking and player analytics
- **⚡ High-Performance Caching**: Redis for session management and SignalR backplane
- **🔍 Full-Text Search**: Elasticsearch for advanced search and log aggregation
- **🔄 Background Jobs**: Hangfire for scheduled tasks and job processing
- **📡 Message Queue**: RabbitMQ for event-driven architecture
- **🔐 JWT Authentication**: Secure API access with ownership-validated, role-based authorization
- **📈 Observability**: OpenTelemetry, structured audit logs, recommendation decision traces, admin debug endpoints
- **🛡️ Operator Dashboard**: Django-based operator control panel with full Wave A/B/C parity

---

## 🏗️ Architecture

The solution follows Clean Architecture principles with the following structure:

```
TycoonTycoon_Backend/
├── Tycoon.Backend.Api/              # Web API project (Minimal API, SignalR Hubs)
├── Tycoon.Backend.Application/      # Application logic (Services, Use Cases, Handlers)
├── Tycoon.Backend.Domain/           # Domain entities and business logic
├── Tycoon.Backend.Infrastructure/   # Data access, external services, EF Core
├── Tycoon.Backend.Migrations/       # EF Core migrations
├── Tycoon.MigrationService/         # Database migration runner service
├── Tycoon.OperatorDashboard.Django/ # Django operator dashboard — canonical (port 8200)
├── Tycoon.OperatorDashboard/        # Blazor Server dashboard — legacy fallback (port 8201, soft-frozen)
├── Tycoon.OperatorDashboard.Vue/    # [DEPRECATED] — superseded by Django dashboard
├── Tycoon.OperatorDashboard.Web/    # [DEPRECATED] — superseded by Django dashboard
├── Tycoon.Sidecar/                  # FastAPI Python sidecar — ML scoring, personalization (port 8100)
├── Tycoon.AppHost/                  # .NET Aspire orchestration host
├── Tycoon.Shared/                   # Shared contracts, DTOs, utilities
├── Tycoon.ServiceDefaults/          # Common Aspire service configurations
├── docker/                          # Docker infrastructure configuration
└── scripts/                         # Development automation scripts
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| Runtime | .NET 10 |
| Web Framework | ASP.NET Core 10 (Minimal API) |
| ORM | Entity Framework Core 10 |
| Primary Database | PostgreSQL 16 |
| Analytics / Events | MongoDB 7 |
| Cache / SignalR Backplane | Redis 7 |
| Search | Elasticsearch 8.11 |
| Message Queue | RabbitMQ 3.13 |
| Object Storage | MinIO (S3-compatible) |
| Background Jobs | Hangfire |
| Real-time | SignalR |
| Orchestration | .NET Aspire |
| Operator Dashboard | Django 5.2 (primary), Blazor Server (legacy) |
| Sidecar | Python 3.12, FastAPI, uvicorn |

---

## Authentication & Login

The backend uses **JWT (JSON Web Tokens)** for all authentication. There is no external OAuth provider — auth is handled entirely by the API.

### Player Auth Endpoints (`/auth`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/auth/register` | Create a new account (email, handle, password) |
| POST | `/auth/signup` | Register + immediate login in one call (preferred for mobile) |
| POST | `/auth/login` | Login with email and password |
| POST | `/auth/refresh` | Exchange a refresh token for a new access token |
| POST | `/auth/logout` | Revoke refresh tokens for the current device |

### Admin Auth Endpoints (`/admin/auth`)

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/admin/auth/login` | Admin login (email allowlist enforced) |
| POST | `/admin/auth/refresh` | Admin token refresh |
| GET | `/admin/auth/me` | Current admin profile |

Admin routes additionally require the `X-Admin-Ops-Key` header (configured via `AdminOps:Key` in appsettings).

### How It Works

1. **Register** — `POST /auth/signup` with `{ email, password, handle, deviceId }`. Password is hashed with BCrypt. Returns an access token + refresh token immediately.
2. **Login** — `POST /auth/login` with `{ email, password, deviceId }`. Returns a JWT access token (60 min) and a refresh token (30 days, per-device).
3. **Authenticated requests** — Include `Authorization: Bearer <access_token>` header.
4. **Token refresh** — `POST /auth/refresh` with the refresh token before the access token expires.
5. **WebSocket auth** — SignalR hubs accept the token via `?access_token=` query parameter on `/ws/*` endpoints.
6. **Ownership validation** — Player-scoped endpoints (personalization, coach, notifications) verify the JWT `sub` claim matches the route `playerId`; returns `403 Forbidden` on mismatch.

### JWT Configuration

```json
{
  "JwtSettings": {
    "SecretKey": "YOUR-SUPER-SECRET-KEY-MINIMUM-32-CHARACTERS-LONG",
    "Issuer": "TycoonBackendApi",
    "Audience": "TycoonFrontendApp",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30
  }
}
```

### JWT Claims

| Claim | Description |
|-------|-------------|
| `sub` | User ID (GUID) — used for ownership validation |
| `email` | User email |
| `handle` | Username |
| `role` | `"admin"` or `"user"` |
| `scope` | Space-separated permissions |
| `aud` | `"mobile-app"` or `"admin-app"` |

---

## API Feature Areas

The API is organized into feature modules under `Tycoon.Backend.Api/Features/`:

### Player & Auth
Auth, Users, Players, Profile, Referrals, Votes

### Gameplay
Matches, Matchmaking, Party, Territory, Questions, Leaderboards, Seasons, Guardians, GameEvents

### Progression & Economy
Skills, Powerups, Missions, Economy, Player Transactions, Store (catalog, avatars, flash sales, stock policies)

### Social & Messaging
Friends, Social Presence, Direct Messages, Notifications, Player Inbox

### Learning
Learning Modules, Study Sessions, Question Favorites, Flashcards

### Personalization & Coach
Player Mind Profiles, Behavior Events, Recommendations, Coach Daily Brief, Coach Feedback

### Analytics
Player analytics, event tracking, question performance

### Experimentation
A/B experiment assignment, variant resolution, impression and outcome tracking

### Mobile
Dedicated mobile endpoints for Matches, Players, Seasons, Leaderboards, Economy

### Admin
Full admin panel endpoints for: Users, Matches, Economy, Seasons, Moderation, Anti-Cheat, Notifications, Questions, Skills, Powerups, Config, Media, Analytics, Email ACL, Event Queue, Store (flash sales, stock policies, purchase analytics), Personalization, Experiments

### gRPC Services (port 5001)

| Service | Proto | Purpose |
|---------|-------|---------|
| SidecarService | `protos/sidecar.proto` | Python sidecar integration (analytics, ML inference) |
| MobileMatchService | `protos/mobile.proto` | Flutter mobile client (match lifecycle, streaming) |

### SignalR Hubs (port 5000)

| Hub | Path | Purpose |
|-----|------|---------|
| Match Hub | `/ws/match` | Real-time match state |
| Presence Hub | `/ws/presence` | Online presence tracking |
| Notification Hub | `/ws/notify` | Push notifications |

---

## Personalization & Experimentation

### Unified Personalization Layer

The backend is the **sole authority** for all personalization decisions. The FastAPI sidecar provides scoring intelligence; all guardrail enforcement, recommendation persistence, and audit logging happen in .NET.

#### Player-Facing Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/personalization/profile/{playerId}` | Get or create player mind profile |
| POST | `/personalization/profile/{playerId}/event` | Record a behavior event |
| POST | `/personalization/profile/{playerId}/recalculate` | Trigger sidecar recalculation |
| GET | `/personalization/home/{playerId}` | Personalized home screen recommendations |
| GET | `/personalization/recommendations/{playerId}` | Active recommendations for player |
| POST | `/personalization/recommendations/{id}/accept` | Accept a recommendation |
| POST | `/personalization/recommendations/{id}/dismiss` | Dismiss a recommendation |
| GET | `/coach/{playerId}/daily-brief` | AI Coach daily brief (tone + archetype-aware) |
| POST | `/coach/{playerId}/feedback` | Submit feedback on coach message |

All player-facing endpoints enforce ownership validation — the JWT `sub` must match the route `playerId`.

#### Admin Endpoints (`/admin/personalization`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/admin/personalization/summary` | Archetype distribution, churn/frustration counts |
| GET | `/admin/personalization/archetypes` | Archetype frequency breakdown |
| GET | `/admin/personalization/recommendations/performance` | Acceptance and dismissal rates |
| GET | `/admin/personalization/player/{playerId}` | Full player mind profile |
| GET | `/admin/personalization/debug/{playerId}` | Profile + last 25 behavior events + last 25 audit entries |
| POST | `/admin/personalization/player/{playerId}/recalculate` | Admin-triggered recalculation |
| POST | `/admin/personalization/player/{playerId}/reset` | Reset profile to safe defaults |
| GET | `/admin/personalization/rules` | List guardrail rules |
| PUT | `/admin/personalization/rules/{ruleKey}` | Upsert a guardrail rule |

#### Guardrails

All recommendations pass through `PersonalizationGuardrailService` before persistence. Thresholds are runtime-configurable via `appsettings.json`:

```json
{
  "Personalization": {
    "Enabled": true,
    "UseSidecar": true,
    "AdaptiveMissions": true,
    "AdaptiveStore": true,
    "AdaptiveNotifications": true,
    "CoachEnabled": true,
    "AdaptiveQuestions": false,
    "FrustrationPaidOfferSuppressionThreshold": 0.75,
    "NotificationFatigueThreshold": 0.70
  },
  "SidecarPersonalization": {
    "BaseUrl": "http://localhost:8001",
    "TimeoutSeconds": 3,
    "Enabled": true
  }
}
```

| Guardrail | Behaviour |
|-----------|-----------|
| Frustration suppression | Paid offers blocked when `FrustrationRiskScore ≥ threshold` |
| Ranked manipulation | `ranked_difficulty_modifier` candidates blocked unconditionally |
| Notification fatigue | Notifications suppressed when `NotificationFatigueScore ≥ threshold` |
| Sidecar authority | Sidecar provides scores only — all decisions made in .NET |
| High-frustration missions | Only low-pressure `confidence_builder` missions recommended when `FrustrationRiskScore ≥ 0.65` |

Blocked recommendations are written to `personalization_audit_logs` only; they are never persisted to `personalization_recommendations`.

#### Audit Trail

Every recommendation decision (allowed and blocked) is logged to `personalization_audit_logs` with full JSONB fields: input signals, sidecar candidate, guardrails applied, and final decision.

#### Database Tables

| Table | Purpose |
|-------|---------|
| `player_mind_profiles` | 21-column profile: archetype, risk scores, preferences, sidecar scores |
| `player_behavior_events` | Raw behavior event stream (question answered, match completed, etc.) |
| `personalization_recommendations` | Persisted allowed recommendations with accept/dismiss lifecycle |
| `personalization_rules` | Admin-managed guardrail rules (JSONB, keyed) |
| `personalization_audit_logs` | Full decision trace per evaluation (JSONB) |

#### Gameplay Hooks

Behavior events are emitted automatically from:

| Event | Trigger |
|-------|---------|
| `question_answered` | `QuestionAnsweredMissionJob` |
| `match_completed` | `MissionProgressService.ApplyMatchCompletedAsync` |
| `learning_module_completed` | `CompleteModuleHandler` |
| `store_item_purchased` | `StoreEndpoints.Purchase` |
| `notification_opened` | `PlayerInboxService.MarkReadAsync` |
| `notification_dismissed` | `PlayerInboxService.DeleteAsync` |
| `mission_completed` | `ClaimMissionHandler` (on successful reward claim) |

#### Mission Personalization

The `/personalization/home/{playerId}` response includes a `recommendedMissions` array with archetype-aligned mission recommendations built by `PersonalizationService.BuildMissionRecommendations`.

**Mission archetypes:** `confidence_builder`, `streak_seeker`, `explorer`, `comeback_player`, `collector`, `risk_taker`, `social_challenger`, `mastery_path`

Each entry contains:
- `missionArchetype` — which style of mission to recommend
- `reason` — human-readable explanation
- `isLowPressure` — `true` when the recommendation is a deliberately low-pressure suggestion

**High-frustration rule:** when `FrustrationRiskScore ≥ 0.65` the service returns only the `confidence_builder` archetype (marked `isLowPressure: true`) regardless of the player's overall archetype.

The sidecar's `/personalization/recommendation-candidates` endpoint also emits archetype-matched `mission` candidates so that the full sidecar → guardrail → audit pipeline is exercised for sidecar-enabled profiles.

---

### A/B Experimentation

The experimentation system provides deterministic player assignment to experiment variants with impression and outcome tracking.

#### Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/experiments/{key}/assignment` | Get or create player assignment for an experiment |
| POST | `/experiments/{key}/impression` | Record that a player saw this variant |
| POST | `/experiments/{key}/outcome` | Record an outcome metric |

#### Admin Endpoints (`/admin/experiments`)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/admin/experiments` | List all experiments |
| POST | `/admin/experiments` | Create a new experiment |
| GET | `/admin/experiments/{id}` | Get experiment details |
| PUT | `/admin/experiments/{id}/status` | Transition status (draft → running → paused → completed) |

#### Database Tables

| Table | Purpose |
|-------|---------|
| `experiments` | Experiment definition (key, name, status, allocation %, date window) |
| `experiment_variants` | Named variants with relative traffic weights |
| `experiment_assignments` | One row per player per experiment — variant, impression count, outcome count |

---

## Store Management

### Flash Sales

Time-windowed discount promotions tied to a SKU.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/admin/store/flash-sales` | List active and scheduled flash sales |
| POST | `/admin/store/flash-sales` | Create a new flash sale |
| DELETE | `/admin/store/flash-sales/{id}` | Cancel a flash sale |

### Stock Policies

Per-SKU purchase limits with automatic reset intervals.

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/admin/store/stock-policies` | List stock policies (filterable by SKU, active) |
| POST | `/admin/store/stock-policies` | Create a policy |
| PUT | `/admin/store/stock-policies/{id}` | Update a policy |
| GET | `/admin/store/analytics` | Purchase analytics with date-range filter |

Purchase limits are enforced per player per SKU at checkout. `GetSpecialOffers` additionally checks `FrustrationRiskScore` against the guardrail threshold before returning paid offers.

### Avatar Catalog

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/store/catalog?category=avatar` | Avatar catalog (shows `owned: true/false` per authenticated player) |
| POST | `/store/avatars/{sku}/purchase` | Purchase an avatar (409 if already owned) |
| GET | `/v1/assets/avatars/{sku}` | Signed asset download URL (403 if not owned) |

Avatar assets are stored in MinIO and served via pre-signed URLs.

### Database Tables

| Table | Purpose |
|-------|---------|
| `store_stock_policies` | Per-SKU purchase limit configuration |
| `player_store_stock_states` | Per-player per-SKU quantity tracking |
| `flash_sales` | Time-windowed promotions |
| `reward_claim_rules` | Rate-limiting rules for claimable rewards |
| `season_reward_rules` | Per-tier season-end reward definitions |

---

## Sidecar Integration

The FastAPI sidecar (`Tycoon.Sidecar/`) provides ML scoring and analytics processing as a lightweight HTTP service.

### Personalization Routes

| Method | Route | Description |
|--------|-------|-------------|
| POST | `/personalization/score-player` | Miss-rate + slow-rate frustration model, archetype classification, churn risk |
| POST | `/personalization/recommendation-candidates` | 4 candidate types based on profile signals |

The .NET `PersonalizationSidecarClient` calls these routes with a configurable timeout (`SidecarPersonalization:TimeoutSeconds`). On timeout or failure, a fault-tolerant fallback returns empty candidates — the backend proceeds with rule-based defaults.

### Analytics / gRPC Routes

| Route | Description |
|-------|-------------|
| `ReportAnalyticsEvent` / `StreamAnalyticsEvents` | Accepts `question_answered` events; persists via `IAnalyticsEventWriter` |
| `SubmitInferenceResult` | Persists to file-backed `FileSidecarInferenceStore`; falls back to in-memory on startup failure |
| `TriggerBackendAction` | Supports `admin_event_queue_reprocess` with `scope`, `limit`, `adminUser` params |

### Mobile gRPC

- `WatchLeaderboard` uses live leaderboard queries instead of static snapshots.
- `PlayMatch` evaluates answer correctness using persisted question answer keys and emits running score/correct-count updates in stream events.

---

## ✅ Prerequisites

### Required Tools

1. **[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)**
   ```bash
   dotnet --version  # Should show 10.0.x
   ```

2. **[Docker Desktop](https://www.docker.com/get-started)** (includes Docker Compose V2)
   ```bash
   docker --version
   docker compose version
   ```

### Optional but Recommended

- **[Make](https://www.gnu.org/software/make/)** — For convenient command shortcuts
  - macOS: `brew install make`
  - Linux: Usually pre-installed, or `apt-get install build-essential`
  - Windows: Included with Git Bash or install via Chocolatey

---

## 🚀 Quick Start

### Automated Setup (Recommended)

**Linux/macOS:**
```bash
chmod +x scripts/setup-dev.sh
./scripts/setup-dev.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\setup-dev.ps1
```

These scripts will:
- Check for required tools (.NET SDK, Docker)
- Create/validate `.env` configuration file
- Validate appsettings.json files
- Optionally start Docker infrastructure

### Manual Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/devartblake/TycoonTycoon_Backend.git
   cd TycoonTycoon_Backend
   ```

2. **Start infrastructure**
   ```bash
   make -f docker/MakeFile up
   # OR without make:
   docker compose -f docker/compose.yml up -d
   ```

3. **Run migrations**
   ```bash
   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
   ```

4. **Start the API**
   ```bash
   dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
   ```

5. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Hangfire Dashboard: http://localhost:5000/hangfire
   - Operator Dashboard: http://localhost:8300

---

## 💻 Development Workflows

### Local Development (Option A - Recommended)

#### 1. Start Infrastructure

```bash
make -f docker/MakeFile up
```

This starts: PostgreSQL (5432), MongoDB (27017), Redis (6379), Elasticsearch (9200), RabbitMQ (5672, 15672), MinIO (9000).

#### 2. Verify Health

```bash
make -f docker/MakeFile health
```

#### 3. Run Database Migrations

```bash
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

This applies all EF Core migrations, seeds reference data, and configures Elasticsearch indices.

#### 4. Start the API

```bash
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

#### 5. Development Tools

```bash
make -f docker/MakeFile up-dev
```

| Tool | URL | Purpose |
|------|-----|---------|
| pgAdmin | http://localhost:5050 | PostgreSQL UI |
| Mongo Express | http://localhost:8081 | MongoDB UI |
| Kibana | http://localhost:5601 | Elasticsearch UI |
| RabbitMQ Management | http://localhost:15672 | Message queue UI |
| Prometheus | http://localhost:9090 | Metrics |
| Grafana | http://localhost:3000 | Dashboards |

#### Connection Strings for Local Development

```json
{
  "ConnectionStrings": {
    "db": "Host=localhost;Port=5432;Database=tycoon_db;Username=tycoon_user;Password=tycoon_password_123",
    "redis": "localhost:6379,password=tycoon_redis_password_123,defaultDatabase=0",
    "mongo": "mongodb://tycoon_admin:tycoon_mongo_password_123@localhost:27017/tycoon_db?authSource=admin",
    "elasticsearch": "http://localhost:9200"
  }
}
```

### Docker Development (Option B)

```bash
# Start everything
docker compose -f docker/compose.yml up -d --build

# View logs
make -f docker/MakeFile logs
make -f docker/MakeFile api-logs

# Stop everything
make -f docker/MakeFile down
```

---

## ⚙️ Configuration

### Environment Variables

| Service | Variable | Default | Description |
|---------|----------|---------|-------------|
| PostgreSQL | `POSTGRES_DB` | `tycoon_db` | Database name |
| | `POSTGRES_USER` | `tycoon_user` | Database user |
| | `POSTGRES_PASSWORD` | `tycoon_password_123` | Database password |
| MongoDB | `MONGO_INITDB_ROOT_USERNAME` | `tycoon_admin` | Admin username |
| | `MONGO_INITDB_ROOT_PASSWORD` | `tycoon_mongo_password_123` | Admin password |
| Redis | `REDIS_PASSWORD` | `tycoon_redis_password_123` | Redis password |
| Elasticsearch | `ELASTIC_PASSWORD` | `tycoon_elastic_password_123` | Elasticsearch password |
| RabbitMQ | `RABBITMQ_USER` | `tycoon_user` | RabbitMQ username |
| | `RABBITMQ_PASSWORD` | `tycoon_rabbitmq_password_123` | RabbitMQ password |

**⚠️ Security Note**: Change all passwords before deploying to production.

### Application Settings

- `appsettings.json` — Base configuration
- `appsettings.Development.json` — Development overrides (includes Personalization and SidecarPersonalization sections)
- `appsettings.Production.json` — Production configuration

---

## 🗄️ Database Migrations

### Overview

- **`Tycoon.MigrationService`** is the sole owner of migrations and seeding
- The API does **not** run migrations automatically (by design)

### Running Migrations

```bash
# Standalone (recommended for local dev)
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj

# Via Docker
make -f docker/MakeFile migrate
```

### Creating New Migrations

```bash
dotnet ef migrations add YourMigrationName \
  --project Tycoon.Backend.Migrations/Tycoon.Backend.Migrations.csproj \
  --startup-project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj \
  --context AppDb \
  --output-dir Migrations
```

Or use the helper script:

```bash
./scripts/update-ef-migration.sh --name AddYourChange
```

Options:
- `--remove-last` — Remove the latest migration first, then create the new one
- `--apply` — Run `dotnet ef database update` after generating
- `--no-build` — Pass `--no-build` to EF commands

### Pending Migrations (Staging/Production)

If you need to apply schema changes manually (e.g., staging/prod where `dotnet ef` is not available):

```bash
# Apply via idempotent SQL script — run in a single transaction
psql -U tycoon_user -d tycoon_db -f docs/pending_migrations_2026-04-29.sql
```

The SQL script covers all 10 outstanding migrations (store stock system, flash sales, reward claim rules, season reward rules, personalization tables, experiment tables, personalization audit log, and recommendation `reason` column).

### Migration Modes

Configure via `MigrationService__Mode`:

| Mode | Description |
|------|-------------|
| `MigrateAndSeed` (default) | Run migrations and seed data |
| `RebuildElastic` | Rebuild Elasticsearch indices only |
| `MigrateSeedAndRebuildElastic` | Run everything |

---

## 🔌 Available Services

### Infrastructure Services

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Primary relational database |
| MongoDB | 27017 | Analytics, events, document data |
| Redis | 6379 | Caching & SignalR backplane |
| Elasticsearch | 9200 | Search & log aggregation |
| RabbitMQ | 5672 | Message broker |
| RabbitMQ Management | 15672 | RabbitMQ admin UI |
| MinIO | 9000 | S3-compatible object storage |
| MinIO Console | 9001 | MinIO admin UI |

### Application Services

| Service | Port | Purpose |
|---------|------|---------|
| Backend API | 5000 | Main REST API + SignalR |
| Swagger UI | 5000/swagger | Interactive API docs |
| Hangfire Dashboard | 5000/hangfire | Background job monitoring |
| Operator Dashboard | 8200 | Django ops control panel (primary) |
| Operator Dashboard (legacy) | 8201 | Blazor Server ops panel (soft-frozen) |
| FastAPI Sidecar | 8100 | ML scoring, personalization, analytics |

### Development Tools (dev profile only)

| Service | Port | Credentials |
|---------|------|-------------|
| pgAdmin | 5050 | admin@tycoon.local / admin_password_123 |
| Mongo Express | 8081 | admin / admin_password_123 |
| Kibana | 5601 | elastic / tycoon_elastic_password_123 |
| Prometheus | 9090 | (no auth) |
| Grafana | 3000 | admin / admin_password_123 |
| DBGate | 3001 | (no auth) — multi-DB browser |

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test Tycoon.Backend.Api.Tests/

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=../coverage
```

### Test Projects

- `Tycoon.Backend.Api.Tests` — API integration and security contract tests
- `Tycoon.Backend.Application.Tests` — Application logic tests
- `Tycoon.Backend.Infrastructure.Tests` — Data access and avatar handler tests

---

## 🛠️ Operator Dashboard

### Current State (May 2026)

| Project | Status | Port |
|---------|--------|------|
| `Tycoon.OperatorDashboard.Django` | **Active — canonical replacement** | 8200 |
| `Tycoon.OperatorDashboard` (Blazor) | Soft-frozen April 22; warm fallback until June 12 | 8201 |
| `Tycoon.OperatorDashboard.Vue` | **Deprecated** — superseded by Django | — |
| `Tycoon.OperatorDashboard.Web` | **Deprecated** — superseded by Django | — |

### Django Dashboard Coverage

The Django dashboard has full Wave A/B/C parity with the Blazor dashboard:

| Surface | Route |
|---------|-------|
| Command Center (health) | `/` |
| Users (list, ban/unban) | `/users` |
| Moderation (logs, profile lookup, status) | `/moderation` |
| Security Audit | `/security/audit` |
| Questions (list, approve, reject) | `/content/questions` |
| Game Events (open/start/close lifecycle) | `/events/game-events` |
| Seasons (activate/close/recompute/leaderboard) | `/operations/seasons` |
| Economy (history, coin grants) | `/economy/player` |
| Anti-Cheat (flag review queue) | `/security/anticheat` |
| Notifications (send/schedule/dead-letter) | `/operations/notifications` |
| Event Queue (reprocess) | `/operations/event-queue` |
| Store (flash sales, stock policies, purchase analytics) | `/store/*` |
| Media Intent / MinIO Diagnostics | `/media/*` |

### Cutover Timeline

| Date | Event |
|------|-------|
| April 22, 2026 | Blazor soft-freeze enforced |
| May 8–14, 2026 | Staging parallel-run window (runbook: `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`) |
| May 15, 2026 | Hard cutover — nginx upstream switched to Django |
| June 12, 2026 | Blazor rollback window closes |

### Pending Release Gates

- [ ] Apply `docs/pending_migrations_2026-04-29.sql` to staging + production (DBA action)
- [ ] Execute staging parallel-run (May 8–14) per `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`
- [ ] Capture operator sign-off (QA Lead + Backend Lead + On-call Operator)

### Personalization Admin

The `.NET` backend exposes full `/admin/personalization/*` APIs. Django operator UI routes are implemented at `/personalization`, `/personalization/player`, and `/personalization/rules`.

---

## CI/CD

GitHub Actions (`.github/workflows/dotnet-ci.yml`) runs on every PR and push to `main`:

1. **Build & Test** — Restore, build (Release), run all tests
2. **Security Contract Tests** — Admin auth, rate limiting, moderation, banned player, error envelope validation
3. **Schema Validation** — Detects EF Core schema drift

---

## 🔧 Troubleshooting

### Services Won't Start

1. Check Docker is running: `docker info`
2. Check for port conflicts: `docker compose ps`
3. View logs: `docker compose -f docker/compose.yml logs [service-name]`
4. Clean restart:
   ```bash
   make -f docker/MakeFile down && make -f docker/MakeFile up
   ```

### API Can't Connect to Database

1. Verify infrastructure is healthy: `make -f docker/MakeFile health`
2. Check connection strings in `appsettings.Development.json` match `docker/.env`
3. Ensure migrations have run
4. Verify PostgreSQL: `docker compose -f docker/compose.yml exec postgres psql -U tycoon_user -d tycoon_db`

### Migrations Fail

1. Ensure PostgreSQL is healthy and accessible
2. Check database credentials match `.env` file
3. If `PendingModelChangesWarning` appears, verify `AppDbModelSnapshot.cs` is up to date and the design-time factory includes `ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))`
4. Drop and recreate (⚠️ data loss):
   ```bash
   make -f docker/MakeFile clean
   make -f docker/MakeFile up
   make -f docker/MakeFile migrate
   ```

### Personalization / Sidecar Not Working

1. Verify sidecar is running: `docker compose ps sidecar`
2. Check `SidecarPersonalization:BaseUrl` and `SidecarPersonalization:Enabled` in appsettings
3. The system has a built-in fault-tolerant fallback — if the sidecar is unavailable, personalization continues with empty candidates and rule-based defaults
4. View decision trace in admin debug endpoint: `GET /admin/personalization/debug/{playerId}`

### Elasticsearch Not Responding

1. Increase memory if needed: edit `ES_JAVA_OPTS` in `.env`
2. Wait 30–60 seconds for ES to fully initialize
3. Verify credentials:
   ```bash
   curl -u elastic:tycoon_elastic_password_123 http://localhost:9200/_cluster/health?pretty
   ```

### Performance Issues

1. Allocate more resources to Docker Desktop (Settings → Resources)
2. Recommended minimums: 4 CPU cores, 8 GB RAM, 20 GB disk
3. Disable dev profile services if not needed: `make -f docker/MakeFile up`

### Clean Slate Reset

```bash
make -f docker/MakeFile clean   # ⚠️ Deletes all data
make -f docker/MakeFile up
make -f docker/MakeFile migrate
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

---

## 📚 Documentation

### Architecture & Decisions
- **[docs/BACKEND_DECISIONS.md](docs/BACKEND_DECISIONS.md)** — Frozen architectural decisions (auth, enums, event dedupe, MFA)
- **[docs/auth_flow_backend_plan.md](docs/auth_flow_backend_plan.md)** — Auth flow design and backend plan
- **[docs/frontend_backend_auth_analysis.md](docs/frontend_backend_auth_analysis.md)** — Frontend/backend auth integration analysis
- **[docs/security_error_envelope_contract.md](docs/security_error_envelope_contract.md)** — Error envelope security contract

### Personalization & Experimentation
- **[docs/backend_personalization_plan.md](docs/backend_personalization_plan.md)** — Personalization hardening plan (complete)
- **[docs/personalization_alignment_audit.md](docs/personalization_alignment_audit.md)** — Layer-by-layer alignment audit (100%)
- **[docs/unified_personalization_next_steps.md](docs/unified_personalization_next_steps.md)** — Personalization architecture and workstream status

### Operator Dashboard
- **[docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md](docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md)** — Django vs Blazor feature parity checklist
- **[docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md](docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md)** — Parallel-run execution guide and sign-off table
- **[docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md](docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md)** — Incident response runbook
- **[docs/pending_migrations_2026-04-29.sql](docs/pending_migrations_2026-04-29.sql)** — Idempotent DDL for all outstanding migrations

### Infrastructure & Setup
- **[Docker.md](Docker.md)** — Detailed Docker setup and infrastructure guide
- **[docs/ON_PREM_CLOUDFLARE_DEPLOYMENT.md](docs/ON_PREM_CLOUDFLARE_DEPLOYMENT.md)** — On-prem production deployment via Cloudflare
- **[docs/minio-setup.md](docs/minio-setup.md)** — MinIO bucket setup (console, mc CLI, AWS CLI, .NET SDK)

### Game Systems
- **[docs/PLAYER_TRANSACTIONS.md](docs/PLAYER_TRANSACTIONS.md)** — Economy and season point transaction system reference
- **[docs/GAME_BALANCE_AUTOMATION_PLAN.md](docs/GAME_BALANCE_AUTOMATION_PLAN.md)** — Energy/lives mode balancing and sidecar automation
- **[docs/REBALANCE_OPERATIONS_RUNBOOK.md](docs/REBALANCE_OPERATIONS_RUNBOOK.md)** — Rebalance operations runbook

### Client Integration
- **[docs/FLUTTER_INTEGRATION.md](docs/FLUTTER_INTEGRATION.md)** — Flutter client integration guide
- **[docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md](docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md)** — Flutter economy, safeguards, and mode entry UX

### Admin & Operations
- **[docs/admin_backend_priority_plan.md](docs/admin_backend_priority_plan.md)** — Admin backend priority plan
- **[docs/frontend_admin_security_rollout_plan.md](docs/frontend_admin_security_rollout_plan.md)** — Frontend admin security rollout
- **[docs/FASTAPI_SIDECAR_IMPLEMENTATION_PROCESS.md](docs/FASTAPI_SIDECAR_IMPLEMENTATION_PROCESS.md)** — FastAPI sidecar implementation guide

### Changelog & Status
- **[docs/CHANGELOG.md](docs/CHANGELOG.md)** — Branch changelog
- **[docs/REMAINING_TASKS.md](docs/REMAINING_TASKS.md)** — Outstanding work tracker

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Follow C# coding conventions; write unit tests for new features
4. Run tests: `dotnet test`
5. Commit and push: `git commit -m "Add my feature" && git push origin feature/my-feature`
6. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License — see the LICENSE file for details.

---

## 🌟 Support

- **GitHub Issues**: https://github.com/devartblake/TycoonTycoon_Backend/issues
- **Discussions**: https://github.com/devartblake/TycoonTycoon_Backend/discussions

---

Built with .NET, PostgreSQL, MongoDB, Redis, Elasticsearch, RabbitMQ, Hangfire, MinIO, FastAPI, and Django.
