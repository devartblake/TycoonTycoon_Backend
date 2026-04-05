# Synaptix Backend

A modern, cloud-native backend API built with .NET 9, designed for the scalable Synaptix cognitive competition platform with real-time analytics, robust data persistence, and comprehensive observability.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Authentication & Login](#authentication--login)
- [API Feature Areas](#api-feature-areas)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Development Workflows](#development-workflows)
  - [Local Development](#local-development-option-a-recommended)
  - [Docker Development](#docker-development-option-b)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Available Services](#available-services)
- [Testing](#testing)
- [Operator Dashboard Migration Status](#operator-dashboard-migration-status)
- [Troubleshooting](#troubleshooting)
- [Documentation](#documentation)
- [Contributing](#contributing)

---

## 🎯 Overview

Synaptix Backend provides a complete platform backend infrastructure featuring:

- **🎮 Game State Management**: PostgreSQL-based persistent game state with EF Core
- **📊 Real-time Analytics**: MongoDB for event tracking and player analytics
- **⚡ High-Performance Caching**: Redis for session management and SignalR backplane
- **🔍 Full-Text Search**: Elasticsearch for advanced search and log aggregation
- **🔄 Background Jobs**: Hangfire for scheduled tasks and job processing
- **📡 Message Queue**: RabbitMQ for event-driven architecture
- **🔐 JWT Authentication**: Secure API access with role-based authorization
- **📈 Observability**: Built-in OpenTelemetry support and comprehensive logging

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
├── Tycoon.OperatorDashboard/        # Blazor Server operator control panel (port 8200)
├── Tycoon.OperatorDashboard.Django/ # Django operator dashboard (port 8300)
├── Tycoon.Sidecar/                  # FastAPI Python sidecar — ML, analytics, webhooks (port 8100)
├── Tycoon.AppHost/                  # .NET Aspire orchestration host
├── Tycoon.Shared/                   # Shared contracts, DTOs, utilities
├── Tycoon.ServiceDefaults/          # Common Aspire service configurations
├── docker/                          # Docker infrastructure configuration
└── scripts/                         # Development automation scripts
```

> Operator dashboard container source of truth: **Django (`Tycoon.OperatorDashboard.Django`)**.
> Blazor dashboard remains available as `operator-dashboard-blazor` for side-by-side validation.

## 🧭 Operator Dashboard Migration Status

As of **April 5, 2026**:
- `Tycoon.OperatorDashboard.Django` is now the default operator dashboard service in Docker Compose.
- `Tycoon.OperatorDashboard` (Blazor) is retained as a legacy comparison target (`operator-dashboard-blazor`).
- `Tycoon.OperatorDashboard.Web` and `Tycoon.OperatorDashboard.Vue` remain migration reference implementations.
- Active migration tracker: `docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`.

### Technology Stack

- **Runtime**: .NET 9
- **Web Framework**: ASP.NET Core 9.0 (Minimal API)
- **Operator Dashboard**: Django 5.2 (primary), Blazor Server (.NET 9 legacy)
- **ORM**: Entity Framework Core 9.0
- **Databases**: PostgreSQL 16, MongoDB 7.0
- **Cache**: Redis 7
- **Search**: Elasticsearch 8.11
- **Message Queue**: RabbitMQ 3.13
- **Object Storage**: MinIO (S3-compatible)
- **Background Jobs**: Hangfire
- **Real-time Communication**: SignalR
- **Orchestration**: .NET Aspire
- **Sidecar**: Python 3.12, FastAPI, uvicorn

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
| GET | `/admin/auth/me` | Current admin profile (requires authorization) |

Admin routes additionally require the `X-Admin-Ops-Key` header (configured via `AdminOps:Key` in appsettings).

### How It Works

1. **Register** — `POST /auth/signup` with `{ email, password, handle, deviceId }`. Password is hashed with BCrypt. Returns an access token + refresh token immediately.
2. **Login** — `POST /auth/login` with `{ email, password, deviceId }`. Returns a JWT access token (60 min) and a refresh token (30 days, per-device).
3. **Authenticated requests** — Include `Authorization: Bearer <access_token>` header.
4. **Token refresh** — `POST /auth/refresh` with the refresh token before the access token expires.
5. **WebSocket auth** — SignalR hubs accept the token via `?access_token=` query parameter on `/ws/*` endpoints.

### JWT Configuration (`appsettings.json`)

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
| `sub` | User ID (GUID) |
| `email` | User email |
| `handle` | Username |
| `role` | `"admin"` or `"user"` |
| `scope` | Space-separated permissions (e.g. `profile:read gameplay:write`) |
| `aud` | `"mobile-app"` or `"admin-app"` |

---

## API Feature Areas

The API is organized into feature modules under `Tycoon.Backend.Api/Features/`:

**Player & Auth** — Auth, Users, Players, Profile
**Gameplay** — Matches, Matchmaking, Party, Territory, Questions, Leaderboards, Seasons
**Progression & Economy** — Skills, Powerups, Missions, Economy, Referrals, Votes
**Events** — GameEvents, Guardians
**Analytics** — Player analytics and event tracking
**Mobile** — Dedicated mobile endpoints for Matches, Players, Seasons, Leaderboards, Economy
**Admin** — Full admin panel endpoints for Users, Matches, Economy, Seasons, Moderation, Anti-Cheat, Notifications, Questions, Skills, Powerups, Config, Media, Analytics, Email ACL

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

## ✅ Prerequisites

Before you begin, ensure you have the following installed:

### Required Tools

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (9.0 or later)
   ```bash
   dotnet --version  # Should show 9.0.x
   ```

2. **[Docker Desktop](https://www.docker.com/get-started)** (includes Docker Compose V2)
   ```bash
   docker --version
   docker compose version
   ```

### Optional but Recommended

- **[Make](https://www.gnu.org/software/make/)** - For convenient command shortcuts
  - macOS: `brew install make`
  - Linux: Usually pre-installed, or `apt-get install build-essential`
  - Windows: Included with Git Bash or install via Chocolatey

- **[Git](https://git-scm.com/)** - For version control

---

## 🚀 Quick Start

### Automated Setup (Recommended)

We provide setup scripts for all platforms that automate the environment configuration:

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
- ✅ Check for required tools (.NET SDK, Docker)
- ✅ Create/validate `.env` configuration file
- ✅ Validate appsettings.json files
- ✅ Optionally start Docker infrastructure
- ✅ Provide next steps for running migrations and the API

### Manual Quick Start

If you prefer to set up manually:

1. **Clone the repository**
   ```bash
   git clone https://github.com/devartblake/TycoonTycoon_Backend.git
   cd TycoonTycoon_Backend
   ```

2. **Configure environment**
   ```bash
   # Ensure docker/.env exists (should be committed with defaults)
   # Review and customize if needed
   ```

3. **Start infrastructure**
   ```bash
   make -f docker/MakeFile up
   # OR without make:
   docker compose -f docker/compose.yml up -d
   ```

4. **Run migrations**
   ```bash
   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
   ```

5. **Start the API**
   ```bash
   dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
   ```

6. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Hangfire Dashboard: http://localhost:5000/hangfire

---

## 💻 Development Workflows

### Local Development (Option A - Recommended)

This is the **primary development workflow**. It runs infrastructure in Docker while running .NET services on your host machine for fast iteration.

#### 1. Start Infrastructure

```bash
make -f docker/MakeFile up
```

This starts:
- PostgreSQL (port 5432)
- MongoDB (port 27017)
- Redis (port 6379)
- Elasticsearch (port 9200)
- RabbitMQ (ports 5672, 15672)

#### 2. Verify Health

```bash
make -f docker/MakeFile health
```

Wait until all services report as healthy (typically 10-30 seconds).

#### 3. Run Database Migrations

```bash
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

This will:
- Apply all EF Core migrations
- Create database schemas and tables
- Seed reference data
- Configure Elasticsearch indices (if enabled)

#### 4. Start the API

```bash
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

The API will:
- Start on http://localhost:5000
- Connect to all Docker infrastructure services via localhost
- Enable hot reload for rapid development

#### 5. Development Tools

With the `dev` profile, you get additional admin UIs:

```bash
make -f docker/MakeFile up-dev
```

Access:
- **pgAdmin**: http://localhost:5050 (PostgreSQL UI)
- **Mongo Express**: http://localhost:8081 (MongoDB UI)
- **Kibana**: http://localhost:5601 (Elasticsearch UI)
- **RabbitMQ Management**: http://localhost:15672
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000

#### Connection Strings for Local Development

The API automatically uses these connection strings from `appsettings.Development.json`:

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

Run the entire stack (infrastructure + API) in Docker containers.

#### 1. Start Everything

```bash
docker compose -f docker/compose.yml up -d --build
```

This builds and starts:
- All infrastructure services
- Migration service (runs once and exits)
- Backend API (containerized)

#### 2. Access the Application

- API: http://localhost:5000
- All other services on their respective ports

#### 3. View Logs

```bash
# All services
make -f docker/MakeFile logs

# Specific service
make -f docker/MakeFile api-logs
make -f docker/MakeFile migration-logs
```

#### 4. Stop Everything

```bash
make -f docker/MakeFile down
```

---

## ⚙️ Configuration

### Environment Variables

Configuration is managed through `docker/.env` which contains defaults for local development. All values can be overridden:

**Key Configuration Values:**

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

**⚠️ Security Note**: Change all passwords before deploying to production!

### Application Settings

Application configuration is in `appsettings.json` files:

- `appsettings.json` - Base configuration (matches Docker defaults)
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production configuration

---

## 🗄️ Database Migrations

### Overview

- **Tycoon.MigrationService** is the **only owner** of database migrations and seeding
- The API **does not** run migrations automatically (by design)
- Migrations are explicit and controlled

### Running Migrations

**Standalone (recommended):**
```bash
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

**Via Docker:**
```bash
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

Or use the helper script to create/update migrations consistently:

```bash
./scripts/update-ef-migration.sh --name AddYourChange
```

Useful options:

- `--remove-last` : remove the latest migration first, then create the new one
- `--apply` : run `dotnet ef database update` after generating the migration
- `--no-build` : pass `--no-build` to EF commands
- `--configuration <Config>` : choose build config for EF commands (default `Debug`)

If schema validation reports drift, you can auto-fix and re-validate in one command:

```bash
./scripts/validate-ef-schema.sh --auto-fix --name AddYourChange
```

### Resetting Migrations (Start Over)

To wipe `Tycoon.Backend.Migrations/Migrations` and recreate a fresh baseline migration:

```bash
./scripts/reset-migrations.sh --force --name InitialCreate
```

Useful options:

- `--skip-add` : clear migration files only (no new migration generated)
- `--name <Name>` : set the baseline migration name
- Runs `scripts/validate-ef-schema.sh` after regeneration to catch pending model changes early

### Migration Modes

Configure via `MigrationService__Mode` in appsettings or environment variable:

- `MigrateAndSeed` (default) - Run migrations and seed data
- `RebuildElastic` - Rebuild Elasticsearch indices only
- `MigrateSeedAndRebuildElastic` - Do everything

---

## 🔌 Sidecar gRPC Integration (Current Status)

The sidecar talks to backend gRPC endpoints on the dedicated HTTP/2 port.

### Currently wired paths

- `ReportAnalyticsEvent` / `StreamAnalyticsEvents`
  - Accepts `event_type = "question_answered"` with valid `payload_json`.
  - Persists via `IAnalyticsEventWriter`.
  - Unsupported event types are explicitly rejected.

- `SubmitInferenceResult`
  - Persists via `ISidecarInferenceStore`.
  - Default implementation is file-backed (`FileSidecarInferenceStore`) with idempotency by `(modelName, entityId, score, metadataJson)`.
  - Default path: `/tmp/tycoon-sidecar/inference-store.jsonl` (overridable with `SidecarInference:StorePath` or `SIDECAR_INFERENCE_STORE_PATH`).
  - In Docker Compose, backend mounts `sidecar_inference_data` at `/var/lib/tycoon-sidecar` and sets `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl`.
  - If file-store initialization fails at startup (e.g., invalid/unwritable path), API falls back to `InMemorySidecarInferenceStore` with a startup warning.

- `TriggerBackendAction`
  - Supports `action = "admin_event_queue_reprocess"` with optional `params_json`:
    - `scope` (string, default `"all"`)
    - `limit` (int, default `1000`)
    - `adminUser` (string, optional)
  - Unknown actions return explicit errors.

### Next planned improvements

- Expand supported analytics event types beyond `question_answered`.
- Add a relational/warehouse-backed inference store implementation (file-backed store is current durable baseline).
- Continue SEQ-3 / SEQ-4 work in `docs/GITHUB_ISSUES_CHECKLIST.md` and `docs/GRPC_TECH_DEBT_NEXT_STEPS.md`.

### Mobile gRPC note

- `WatchLeaderboard` now uses live leaderboard queries (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshots.
- `PlayMatch` now evaluates answer correctness using persisted question answer keys and emits running score/correct-count updates in stream events.

---

## 🛠️ Available Services

### Infrastructure Services (Always Running)

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
| Operator Dashboard | 8200 | Blazor Server ops control panel |
| FastAPI Sidecar | 8100 | ML inference, analytics, webhooks |

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

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Tycoon.Backend.Api.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=../coverage
```

### Test Projects

- `Tycoon.Backend.Api.Tests` - API integration tests
- `Tycoon.Backend.Application.Tests` - Application logic tests
- `Tycoon.Backend.Infrastructure.Tests` - Data access tests

---

## 🔧 Troubleshooting

### Services Won't Start

**Problem**: Docker containers fail to start or crash immediately.

**Solutions**:
1. Check Docker is running: `docker info`
2. Check for port conflicts: `docker compose ps`
3. View logs: `docker compose -f docker/compose.yml logs [service-name]`
4. Try clean restart:
   ```bash
   make -f docker/MakeFile down
   make -f docker/MakeFile up
   ```

### API Can't Connect to Database

**Problem**: API throws database connection errors.

**Solutions**:
1. Verify infrastructure is healthy: `make -f docker/MakeFile health`
2. Check connection strings in `appsettings.Development.json` match `docker/.env`
3. Ensure migrations have run: `dotnet run --project Tycoon.MigrationService/...`
4. Verify PostgreSQL is accessible:
   ```bash
   docker compose -f docker/compose.yml exec postgres psql -U tycoon_user -d tycoon_db
   ```

### Migrations Fail

**Problem**: Migration service throws errors or doesn't complete.

**Solutions**:
1. Ensure PostgreSQL is healthy and accessible
2. Check database credentials match `.env` file
3. Drop and recreate database (⚠️ data loss):
   ```bash
   make -f docker/MakeFile clean  # Removes all data
   make -f docker/MakeFile up
   make -f docker/MakeFile migrate
   ```

### Elasticsearch Not Responding

**Problem**: Elasticsearch health check fails or shows yellow/red status.

**Solutions**:
1. Increase memory (if needed): Edit `ES_JAVA_OPTS` in `.env`
2. Wait longer - ES can take 30-60 seconds to fully start
3. Check ES logs: `docker compose -f docker/compose.yml logs elasticsearch`
4. Verify credentials:
   ```bash
   curl -u elastic:tycoon_elastic_password_123 http://localhost:9200/_cluster/health?pretty
   ```

### Redis Connection Timeout

**Problem**: API logs show Redis connection errors.

**Solutions**:
1. Verify Redis is running: `docker compose ps redis`
2. Test connection: `docker compose exec redis redis-cli -a tycoon_redis_password_123 ping`
3. Check password matches in both `appsettings.json` and `docker/.env`

### Port Already in Use

**Problem**: Docker fails to start because port is already allocated.

**Solutions**:
1. Identify the conflicting process:
   ```bash
   # Linux/Mac
   lsof -i :[port]
   # Windows
   netstat -ano | findstr :[port]
   ```
2. Stop the conflicting service or change port in `docker/.env`
3. Restart Docker stack

### Performance Issues

**Problem**: Slow response times or high resource usage.

**Solutions**:
1. Allocate more resources to Docker Desktop (Settings → Resources)
2. Recommended minimums:
   - CPU: 4 cores
   - Memory: 8 GB
   - Disk: 20 GB
3. Disable dev profile services if not needed:
   ```bash
   make -f docker/MakeFile up  # without -dev
   ```

### Clean Slate Reset

If all else fails, perform a complete reset:

```bash
# Stop and remove everything (⚠️ DELETES ALL DATA)
make -f docker/MakeFile clean

# Start fresh
make -f docker/MakeFile up
make -f docker/MakeFile migrate
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

---

## CI/CD

GitHub Actions workflow (`.github/workflows/dotnet-ci.yml`) runs on every PR and push to `main`/`master`:

1. **Build & Test** — Restore, build (Release), run all tests
2. **Security Contract Tests** — Validates admin auth, rate limiting, moderation, banned player checks, and error envelope hardening
3. **Schema Validation** — Detects EF Core schema drift using `dotnet-ef`

---

## 📚 Documentation

### Architecture & Decisions
- **[docs/BACKEND_DECISIONS.md](docs/BACKEND_DECISIONS.md)** - Frozen architectural decisions (auth, enums, event dedupe, MFA)
- **[docs/auth_flow_backend_plan.md](docs/auth_flow_backend_plan.md)** - Auth flow design and backend plan
- **[docs/frontend_backend_auth_analysis.md](docs/frontend_backend_auth_analysis.md)** - Frontend/backend auth integration analysis
- **[docs/security_error_envelope_contract.md](docs/security_error_envelope_contract.md)** - Error envelope security contract

### Infrastructure & Setup
- **[Docker.md](Docker.md)** - Detailed Docker setup and infrastructure guide
- **[docs/minio-setup.md](docs/minio-setup.md)** - MinIO bucket setup (console, mc CLI, AWS CLI, .NET SDK)
- **[docs/backend-migrations-analysis.md](docs/backend-migrations-analysis.md)** - Migration strategy and analysis

### Game Systems
- **[docs/PLAYER_TRANSACTIONS.md](docs/PLAYER_TRANSACTIONS.md)** - Economy and season point transaction system reference
- **[docs/GAME_BALANCE_AUTOMATION_PLAN.md](docs/GAME_BALANCE_AUTOMATION_PLAN.md)** - Energy/lives mode balancing and Sidecar automation
- **[docs/REBALANCE_OPERATIONS_RUNBOOK.md](docs/REBALANCE_OPERATIONS_RUNBOOK.md)** - Rebalance operations runbook

### Client Integration
- **[docs/FLUTTER_INTEGRATION.md](docs/FLUTTER_INTEGRATION.md)** - Flutter client integration guide (auth, REST API, SignalR, error handling)
- **[docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md](docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md)** - Flutter economy, safeguards, and mode entry UX

### Admin & Operations
- **[docs/admin_backend_priority_plan.md](docs/admin_backend_priority_plan.md)** - Admin backend priority plan
- **[docs/frontend_admin_security_rollout_plan.md](docs/frontend_admin_security_rollout_plan.md)** - Frontend admin security rollout
- **[docs/FASTAPI_SIDECAR_IMPLEMENTATION_PROCESS.md](docs/FASTAPI_SIDECAR_IMPLEMENTATION_PROCESS.md)** - FastAPI sidecar implementation guide

### Other
- **[docs/CHANGELOG.md](docs/CHANGELOG.md)** - Branch changelog
- **[docs/SWAGGER_FIX.md](docs/SWAGGER_FIX.md)** - Swagger configuration fixes

### Live Dashboards (when running)
- **[API Documentation](http://localhost:5000/swagger)** - Interactive Swagger UI
- **Hangfire Dashboard** - Background job monitoring at http://localhost:5000/hangfire
- **Operator Dashboard** - Ops control panel at http://localhost:8200

---

## 🤝 Contributing

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes following the coding standards
4. Run tests: `dotnet test`
5. Commit your changes: `git commit -m "Add my feature"`
6. Push to your fork: `git push origin feature/my-feature`
7. Open a Pull Request

### Coding Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

### Pull Request Process

1. Ensure all tests pass
2. Update relevant documentation
3. Add a clear description of changes
4. Request review from maintainers

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🌟 Support

For issues, questions, or contributions:
- **GitHub Issues**: https://github.com/devartblake/TycoonTycoon_Backend/issues
- **Discussions**: https://github.com/devartblake/TycoonTycoon_Backend/discussions

---

## 🙏 Acknowledgments

Built with:
- [.NET](https://dotnet.microsoft.com/) - Cross-platform framework
- [PostgreSQL](https://www.postgresql.org/) - Robust relational database
- [MongoDB](https://www.mongodb.com/) - Flexible document database
- [Redis](https://redis.io/) - Lightning-fast cache
- [Elasticsearch](https://www.elastic.co/) - Powerful search engine
- [RabbitMQ](https://www.rabbitmq.com/) - Reliable message broker
- [Hangfire](https://www.hangfire.io/) - Background job processor
- [MinIO](https://min.io/) - S3-compatible object storage
- [FastAPI](https://fastapi.tiangolo.com/) - Python web framework for sidecar services

---

**Happy coding! 🚀**
