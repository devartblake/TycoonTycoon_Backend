# Synaptix Backend Bootstrap, Credential, and Seed Setup Plan

Repository analyzed: `devartblake/TycoonTycoon_Backend`  
Target system: Synaptix / TycoonTycoon backend  
Primary goal: replace scattered hardcoded Docker/env/service seed defaults with a controlled, repeatable setup and bootstrap workflow.

---

## Implementation Status (2026-06-04)

The core Alpha/Beta bootstrap path is now implemented:

- `Synaptix.Setup` exists as a standalone CLI and one-shot Compose service.
- `docker/.env.example` uses generated-secret placeholders instead of committed secret defaults.
- Compose requires security-sensitive values and runs `setup` before `migration`.
- PostgreSQL, MongoDB, Redis, RabbitMQ, MinIO, and optional Elasticsearch setup tasks exist.
- MongoDB provisioning creates or updates the app user in `MONGO_AUTH_DB`, validates app-user authentication, warns about a same-named legacy user in `admin`, and removes that legacy user only when `SETUP_MONGO_REMOVE_LEGACY_ADMIN_APP_USER=true`.
- Redis provisioning parses complete structured connection strings and supports raw `REDIS_*` fallback configuration.
- The setup container uses internal MongoDB and Redis ports (`27017` and `6379`).
- `Synaptix.Setup.Tests` covers MongoDB and Redis configuration behavior.
- `Synaptix.MigrationService` remains the authoritative EF migration and application-data seed runner.

Remaining roadmap items include richer manifest enforcement, KMS-backed setup-secret protection, and CLI-authored setup-run event/audit history. The read-only setup visibility architecture documented in [`Synaptix_Setup_UI_CLI_Architecture_Handoff.md`](Synaptix_Setup_UI_CLI_Architecture_Handoff.md) is implemented for Backend-generated diagnostics and durable history.

Sections describing the repository before `Synaptix.Setup` was implemented are retained as historical rationale and are labeled accordingly.

---

## 1. Executive Summary

The backend now has an implemented setup/bootstrap layer around `Synaptix.MigrationService`. The original problem was scattered hardcoded development credentials and manually coordinated provisioning. Those immediate gaps have been addressed for the local Alpha/Beta workflow; this document now records the implemented design and remaining roadmap.

The ideal solution is **not** to put more seed logic into Docker. Docker should only start infrastructure. The application should own:

1. environment/bootstrap validation,
2. service credential generation,
3. super administrator creation,
4. database seeding,
5. MongoDB app-user/database setup,
6. Redis logical key initialization,
7. MinIO bucket and seed object provisioning,
8. RabbitMQ vhost/user/permission setup,
9. Elasticsearch index/template setup,
10. readiness validation.

The recommended solution is to add a dedicated **Synaptix Setup and Bootstrap System** built around the existing `Synaptix.MigrationService`, with a new explicit setup layer:

```text
Synaptix.Setup
  ├── validates required configuration
  ├── generates local-only secrets when missing
  ├── writes docker/.env from a template
  ├── provisions infrastructure services
  ├── runs EF migrations
  ├── seeds core relational data
  ├── seeds object-storage catalog/game data
  ├── creates/updates the super administrator account
  └── writes a bootstrap status report
```

This gives you a single repeatable command for local setup and a controlled pre-start job for staging/production.

---

## 2. Historical Repository Findings

The findings in this section describe the pre-implementation baseline that motivated `Synaptix.Setup`. They are not the current configuration.

### 2.1 Docker Compose embedded fallback credentials

Before the setup work, `docker/compose.yml` embedded fallback credentials directly in Compose variable defaults. Current Compose configuration requires security-sensitive values generated or supplied through the environment.

Examples:

```yaml
POSTGRES_PASSWORD: "${POSTGRES_PASSWORD:-synaptix_password_123}"
MONGO_INITDB_ROOT_PASSWORD: "${MONGO_INITDB_ROOT_PASSWORD:-synaptix_mongo_password_123}"
REDIS_PASSWORD: "${REDIS_PASSWORD:-synaptix_redis_password_123}"
ELASTIC_PASSWORD: ${ELASTIC_PASSWORD:-synaptix_elastic_password_123}
RABBITMQ_DEFAULT_PASS: "${RABBITMQ_PASSWORD:-synaptix_rabbitmq_password_123}"
MINIO_ROOT_PASSWORD: "${MINIO_ROOT_PASSWORD:-synaptix_minio_password_123}"
```

This is convenient for local development, but it creates three problems:

1. secrets are duplicated,
2. changing credentials requires manually editing `.env`,
3. staging/production can accidentally inherit weak defaults.

### 2.2 `.env.example` contained development secrets and admin credentials

Before the setup work, `docker/.env.example` contained development secret values. It now uses `<generated-by-synaptix-setup>` placeholders for generated secrets.

Examples:

```env
POSTGRES_PASSWORD=synaptix_password_123
MONGO_INITDB_ROOT_PASSWORD=synaptix_mongo_password_123
REDIS_PASSWORD=synaptix_redis_password_123
ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION
JWT_SECRET_KEY=your-super-secret-jwt-key-change-me-in-production-minimum-32-characters-long
SUPER_ADMIN_EMAIL=admin@tycoon.local
SUPER_ADMIN_PASSWORD=ChangeMe123!
```

This is useful as documentation, but it should not be the authoritative bootstrap source.

### 2.3 `AppSeeder` currently seeds tiers, missions, and the super admin

`Synaptix.MigrationService/Seeding/AppSeeder.cs` currently:

- seeds tier definitions,
- seeds daily/weekly missions,
- creates the configured super admin user,
- ensures the super admin email exists in `AdminEmailAcls`.

The current super admin seed reads:

```csharp
_cfg["SuperAdmin:Email"]
_cfg["SuperAdmin:Password"]
_cfg["SuperAdmin:Handle"]
```

If email/password are missing, it logs and skips super admin creation.

This is good for idempotency, but the source of truth should be a setup manifest or secret provider, not scattered environment variables.

### 2.4 `MinioSeeder` already supports object-storage and bundled seed fallback

`MinioSeeder` reads seed JSON for:

- store items,
- skill nodes,
- season reward rules,
- questions.

It supports seed source behavior through `MigrationService:SeedSource`:

- `Auto`: try object storage first, then bundled files,
- `Bundled`: read bundled files,
- `MinIO`: require object storage.

This is a good design. The new setup system should keep this behavior and expand it into a broader seed manifest.

### 2.5 Existing docs already describe a migration/bootstrap sequence

The repository already documents a startup sequence where infrastructure starts, the migration service applies migrations, seeds tiers/missions/super admin/catalog/skills/rewards/questions, validates dashboard readiness, then starts the backend API and dashboard.

This plan should formalize that into a first-class setup system.

---

## 3. Recommended Architecture

## 3.1 Dedicated setup project (implemented)

Implemented project:

```text
Synaptix.Setup/
  Synaptix.Setup.csproj
  Program.cs
  Bootstrap/
    BootstrapManifest.cs
    BootstrapOptions.cs
    BootstrapRunner.cs
    BootstrapStateWriter.cs
  Secrets/
    SecretGenerator.cs
    SecretManifestWriter.cs
    SecretValidationService.cs
  Services/
    PostgresSetupTask.cs
    MongoSetupTask.cs
    RedisSetupTask.cs
    RabbitMqSetupTask.cs
    MinioSetupTask.cs
    ElasticsearchSetupTask.cs
    SuperAdminSetupTask.cs
  Templates/
    docker.env.template
    bootstrap.local.template.json
```

Purpose:

- generate or validate required local credentials,
- write `docker/.env`,
- create a setup manifest,
- optionally provision service users/databases/buckets,
- invoke the existing migration service or call the same seeding primitives.

Implemented CLI commands:

```bash
dotnet run --project Synaptix.Setup -- init-local
dotnet run --project Synaptix.Setup -- validate
dotnet run --project Synaptix.Setup -- provision-services
dotnet run --project Synaptix.Setup -- seed
dotnet run --project Synaptix.Setup -- status
```

For staging/production, it should support non-interactive validation:

```bash
dotnet run --project Synaptix.Setup -- validate --environment staging --strict
```

---

## 3.2 Keep `Synaptix.MigrationService` as the authoritative migration/seed runner

Do **not** move EF migration logic into Docker scripts.

Keep:

```text
Synaptix.MigrationService
```

as the runtime pre-start job that:

1. applies EF migrations,
2. seeds relational data,
3. invokes MinIO/bundled data seeding,
4. validates dashboard readiness.

Add setup coordination around it:

```text
Synaptix.Setup
  ↓ writes docker/.env / bootstrap manifest
docker compose up infrastructure
  ↓
Synaptix.MigrationService
  ↓
backend-api
  ↓
operator-dashboard
```

This avoids splitting database responsibility across shell scripts, Docker init scripts, and application code.

---

## 3.3 Introduce a bootstrap manifest

Create:

```text
config/bootstrap/bootstrap.local.json
config/bootstrap/bootstrap.schema.json
config/bootstrap/bootstrap.example.json
```

Example:

```json
{
  "environment": "local",
  "project": "synaptix",
  "services": {
    "postgres": {
      "database": "synaptix_db",
      "appUser": "synaptix_user",
      "port": 5432,
      "passwordSecret": "POSTGRES_PASSWORD"
    },
    "mongo": {
      "rootUser": "synaptix_admin",
      "appUser": "synaptix_app_user",
      "databases": [
        "synaptix_analytics",
        "synaptix_crypto"
      ],
      "passwordSecret": "MONGO_APP_PASSWORD"
    },
    "redis": {
      "logicalDatabases": {
        "cache": 0,
        "rateLimit": 1,
        "sessions": 2,
        "locks": 3,
        "jobs": 4
      },
      "passwordSecret": "REDIS_PASSWORD"
    },
    "rabbitmq": {
      "vhost": "synaptix",
      "appUser": "synaptix_user",
      "passwordSecret": "RABBITMQ_PASSWORD"
    },
    "minio": {
      "bucket": "synaptix-assets",
      "seedPrefix": "seeds/",
      "rootUserSecret": "MINIO_ROOT_USER",
      "rootPasswordSecret": "MINIO_ROOT_PASSWORD"
    },
    "elasticsearch": {
      "enabled": true,
      "indices": [
        "synaptix-daily-rollups-write",
        "synaptix-player-daily-rollups-write"
      ]
    }
  },
  "superAdmin": {
    "emailSecret": "SUPER_ADMIN_EMAIL",
    "passwordSecret": "SUPER_ADMIN_PASSWORD",
    "handle": "superadmin"
  },
  "seed": {
    "source": "Auto",
    "manifest": "config/seeds/seed-manifest.json",
    "strict": true
  }
}
```

The manifest should describe what must exist. Secrets should still come from:

- local generated `.env`,
- Docker secrets,
- GitHub Actions secrets,
- cloud secret manager,
- KMS-backed provider,
- or a future Synaptix Security Gateway/KMS integration.

The manifest should **not** contain plaintext production secrets.

---

## 3.4 Introduce a seed manifest

Create:

```text
config/seeds/seed-manifest.json
```

Example:

```json
{
  "version": 1,
  "strict": true,
  "sources": {
    "bundledRoot": "Synaptix.MigrationService/seeds",
    "objectStoragePrefix": "seeds"
  },
  "relational": [
    { "name": "tiers", "required": true, "mode": "upsert" },
    { "name": "missions", "required": true, "mode": "upsert" },
    { "name": "super-admin", "required": true, "mode": "ensure" }
  ],
  "objectStorage": [
    { "name": "store-items", "key": "seeds/store-items.json", "required": true, "mode": "upsert" },
    { "name": "skill-nodes", "key": "seeds/skill-nodes.json", "required": true, "mode": "upsert" },
    { "name": "season-rewards", "key": "seeds/season-rewards.json", "required": true, "mode": "insert-missing" },
    { "name": "questions", "key": "seeds/questions.json", "required": false, "mode": "upsert" }
  ],
  "redis": [
    {
      "name": "cache-prefixes",
      "required": true,
      "keys": [
        "synaptix:cache:",
        "synaptix:rate-limit:",
        "synaptix:sessions:",
        "synaptix:locks:"
      ]
    }
  ],
  "mongo": [
    {
      "name": "analytics-db",
      "database": "synaptix_analytics",
      "collections": ["events", "rollups", "personalization"],
      "indexes": [
        {
          "collection": "events",
          "keys": { "playerId": 1, "createdAtUtc": -1 }
        }
      ]
    },
    {
      "name": "crypto-db",
      "database": "synaptix_crypto",
      "collections": ["settlements", "ledger_events"]
    }
  ]
}
```

This lets the migration/setup system know:

- what is required,
- what can be skipped,
- what source to use,
- whether failure should block startup.

---

## 4. Service-by-Service Setup Plan

## 4.1 PostgreSQL

### Current state

Docker starts PostgreSQL with `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD`.

### Recommended setup

Move PostgreSQL setup to generated `.env` plus migration service validation.

Add `PostgresSetupTask`:

Responsibilities:

- validate `POSTGRES_DB`,
- validate app user,
- verify connection string,
- refuse weak default password outside `local`,
- ensure migration database exists,
- report applied migrations,
- report pending migrations.

Local:

```bash
dotnet run --project Synaptix.Setup -- init-local
docker compose -f docker/compose.yml up -d postgres
dotnet run --project Synaptix.Setup -- validate-postgres
dotnet run --project Synaptix.MigrationService
```

Production:

- database should already exist,
- Setup validates,
- MigrationService applies migrations,
- destructive reset disabled.

Rules:

- `MIGRATION_RESET_DATABASE=true` only allowed in local.
- `MIGRATION_ALLOW_ENSURE_CREATED=true` only allowed in local.
- staging/prod must use real EF migrations.

---

## 4.2 Super Administrator Account

### Current state

`AppSeeder` creates a super admin when `SuperAdmin:Email` and `SuperAdmin:Password` exist. It also creates or updates an `AdminEmailAcl` allowlist row.

### Recommended setup

Keep the idempotent super admin creation logic, but move source of truth into the bootstrap manifest and secret provider.

Add:

```text
SuperAdminSetupTask
SuperAdminBootstrapOptions
SuperAdminCredentialValidator
```

Rules:

- local setup may generate or prompt for password,
- staging/prod require externally supplied secret,
- no default password is allowed outside local,
- password rotation should be supported,
- setup should write a one-time local admin credential file only if explicitly requested.

Example local file:

```text
.local/bootstrap/super-admin.local.txt
```

Contents:

```text
Email: admin@synaptix.app
Password: <generated>
CreatedAtUtc: <timestamp>
```

This file must be `.gitignore`d.

Recommended CLI:

```bash
dotnet run --project Synaptix.Setup -- create-super-admin --local
dotnet run --project Synaptix.Setup -- rotate-super-admin-password
dotnet run --project Synaptix.Setup -- validate-super-admin
```

For Alpha/Beta:

- keep pgAdmin on a syntactically valid local login email such as `admin@synaptix.app`,
- generate a strong local password,
- remove `ChangeMe123!` as the default in `.env.example`,
- document how to retrieve the generated password.

---

## 4.3 MongoDB

### Current state

Docker defines root and app credentials through environment variables. The setup container receives raw root credentials on internal port `27017`; runtime services authenticate the app user through `MONGO_AUTH_DB`.

### Implemented setup and remaining work

`MongoSetupTask` provisions and validates:

- app databases,
- app user create/update in `MONGO_AUTH_DB`,
- app-user authentication,
- analytics database,
- crypto database,
- required analytics/crypto collections and indexes.

It also detects a same-named legacy app user in `admin`. Cleanup is disabled by default and requires:

```env
SETUP_MONGO_REMOVE_LEGACY_ADMIN_APP_USER=true
```

Mongo setup is schema-only: it does not seed analytics or crypto documents. Empty Mongo collections are expected immediately after `provision-services`; valid gameplay or analytics smoke events populate `question_answered_events`, `qa_daily_rollups`, and `qa_player_daily_rollups`.

The Mongo init file remains useful for first-run volume initialization:

```text
docker/init/mongo/001-create-app-user.js
```

But do not hardcode credentials in that file. It should read env variables already generated by `Synaptix.Setup`.

Example logic:

```javascript
const appUser = process.env.MONGO_APP_USER;
const appPassword = process.env.MONGO_APP_PASSWORD;
const analyticsDb = process.env.MONGO_ANALYTICS_DB;
const cryptoDb = process.env.MONGO_CRYPTO_DB;

db = db.getSiblingDB(analyticsDb);
db.createUser({
  user: appUser,
  pwd: appPassword,
  roles: [
    { role: "readWrite", db: analyticsDb },
    { role: "readWrite", db: cryptoDb }
  ]
});
```

`MongoSetupTask` validates:

- root connection works,
- app user can connect,
- required databases exist,
- required collections/indexes exist.

---

## 4.4 Redis

### Current state

Redis starts with required `REDIS_PASSWORD`. The setup container connects over internal port `6379`.

### Implemented setup and remaining work

Redis does not need “data seeding” the same way PostgreSQL/Mongo does, but it does need:

1. password validation,
2. logical database assignment,
3. key namespace conventions,
4. optional warm-up keys,
5. optional distributed lock validation,
6. optional rate-limit key cleanup.

`RedisSetupTask` is implemented. It parses a complete `ConnectionStrings:redis` value when supplied, otherwise uses `REDIS_HOST`, `REDIS_PORT`, and `REDIS_PASSWORD`. It validates read/write/delete access across logical databases `0` through `4`.

Recommended bootstrap manifest section:

```json
"redis": {
  "logicalDatabases": {
    "cache": 0,
    "rateLimit": 1,
    "sessions": 2,
    "locks": 3,
    "jobs": 4
  },
  "prefixes": {
    "cache": "synaptix:cache:",
    "rateLimit": "synaptix:rate-limit:",
    "session": "synaptix:session:",
    "lock": "synaptix:lock:"
  }
}
```

Recommended validation:

```bash
dotnet run --project Synaptix.Setup -- validate-redis
```

Checks:

- can authenticate,
- can write/read/delete a test key,
- required logical DBs are reachable,
- configured prefixes are valid,
- no weak local default password outside local.

---

## 4.5 RabbitMQ

### Current state

RabbitMQ is configured directly from env vars in Compose.

### Recommended setup

Add `RabbitMqSetupTask`.

Responsibilities:

- ensure vhost exists,
- ensure app user exists,
- assign permissions,
- optionally create exchanges/queues for Alpha-critical jobs,
- validate connection.

If RabbitMQ is only needed for post-Alpha features, gate it with feature flags for Alpha.

Recommended manifest:

```json
"rabbitmq": {
  "vhost": "synaptix",
  "users": [
    {
      "name": "synaptix_user",
      "tags": [],
      "permissions": {
        "configure": ".*",
        "write": ".*",
        "read": ".*"
      }
    }
  ],
  "queues": [
    {
      "name": "synaptix.background.jobs",
      "durable": true
    }
  ]
}
```

---

## 4.6 MinIO / Object Storage

### Current state

`MinioSeeder` reads seed JSON files from object storage or bundled files. The documented seed keys are:

```text
seeds/store-items.json
seeds/skill-nodes.json
seeds/season-rewards.json
seeds/questions.json
```

### Recommended setup

Add `MinioSetupTask`.

Responsibilities:

- ensure bucket exists,
- upload bundled seed files if missing,
- verify object keys,
- optionally write seed manifest object,
- validate service account access.

Recommended commands:

```bash
dotnet run --project Synaptix.Setup -- provision-minio
dotnet run --project Synaptix.Setup -- upload-seeds --source Synaptix.MigrationService/seeds
dotnet run --project Synaptix.Setup -- validate-seeds
```

Keep `MinioSeeder` as the runtime database seeder.

Important rule:

- bucket name is configured separately,
- seed keys should not include the bucket name.

---

## 4.7 Elasticsearch

### Current state

`MigrationWorker` tries to ensure Elastic templates and indices before DB migration/seed work, and rebuild can be enabled by mode/config.

### Recommended setup

Keep this behavior, but formalize it as:

```text
ElasticSetupTask
ElasticTemplateSetupTask
ElasticIndexSetupTask
```

For Alpha:

- Elasticsearch should not block core gameplay unless analytics/search is P0.
- if disabled, app should continue cleanly.
- if enabled, setup should validate index templates and auth.

Recommended flags:

```env
ELASTIC_ENABLED=false
ANALYTICS_ENABLED=false
```

for lean Alpha if analytics is not needed for the first release.

---

## 4.8 Grafana, Prometheus, Kibana, pgAdmin, Mongo Express, DBGate

These are developer/admin support tools, not core runtime dependencies.

Recommended:

- keep behind Docker `dev` profile,
- remove real-looking default credentials from Compose,
- generate local admin passwords through `Synaptix.Setup`,
- never require these services for Alpha backend startup.

Example:

```bash
dotnet run --project Synaptix.Setup -- init-local --include-dev-tools
docker compose -f docker/compose.yml --profile dev up -d
```

---

## 5. Configuration Strategy

## 5.1 Replace hardcoded Compose defaults with required env vars

Current style:

```yaml
POSTGRES_PASSWORD: "${POSTGRES_PASSWORD:-synaptix_password_123}"
```

Recommended local-safe style:

```yaml
POSTGRES_PASSWORD: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required. Run Synaptix.Setup init-local.}"
```

Do this for secrets:

- `POSTGRES_PASSWORD`
- `MONGO_INITDB_ROOT_PASSWORD`
- `MONGO_APP_PASSWORD`
- `REDIS_PASSWORD`
- `ELASTIC_PASSWORD`
- `RABBITMQ_PASSWORD`
- `MINIO_ROOT_PASSWORD`
- `GRAFANA_PASSWORD`
- `PGADMIN_DEFAULT_PASSWORD`
- `MONGO_EXPRESS_PASSWORD`
- `ADMIN_OPS_KEY`
- `JWT_SECRET_KEY`
- `KMS_SERVICE_TOKEN`
- `SUPER_ADMIN_PASSWORD`

Keep non-secret defaults acceptable:

```yaml
POSTGRES_DB: "${POSTGRES_DB:-synaptix_db}"
POSTGRES_USER: "${POSTGRES_USER:-synaptix_user}"
POSTGRES_PORT: "${POSTGRES_PORT:-5432}"
```

## 5.2 Replace `.env.example` secret values with placeholders

Recommended:

```env
POSTGRES_PASSWORD=<generated-by-synaptix-setup>
REDIS_PASSWORD=<generated-by-synaptix-setup>
JWT_SECRET_KEY=<generated-by-synaptix-setup>
SUPER_ADMIN_PASSWORD=<generated-by-synaptix-setup-or-secret-manager>
```

Add:

```text
docker/.env.local.generated
```

to `.gitignore`.

Workflow:

```bash
dotnet run --project Synaptix.Setup -- init-local --write docker/.env
```

## 5.3 Add config validation on startup

Add a shared startup validator:

```text
Tycoon.Shared.Configuration
  RequiredSecretValidator.cs
  EnvironmentSafetyValidator.cs
```

Rules:

- local may generate secrets,
- staging/prod must fail if placeholder values are used,
- reset database is blocked outside local,
- default admin password is blocked outside local,
- weak JWT/admin ops key is blocked outside local.

---

## 6. Bootstrap Workflow

## 6.1 Local developer bootstrap

Recommended one-command workflow:

```bash
dotnet run --project Synaptix.Setup -- init-local
docker compose -f docker/compose.yml up -d postgres mongodb redis rabbitmq minio elasticsearch
dotnet run --project Synaptix.Setup -- provision-services
dotnet run --project Synaptix.MigrationService
docker compose -f docker/compose.yml up -d backend-api operator-dashboard
```

Optional wrapper:

```bash
./scripts/bootstrap-local.ps1
./scripts/bootstrap-local.sh
```

## 6.2 Docker Compose bootstrap

Add a one-shot setup container:

```yaml
setup:
  build:
    context: ..
    dockerfile: Synaptix.Setup/Dockerfile
  env_file:
    - .env
  depends_on:
    postgres:
      condition: service_healthy
    mongodb:
      condition: service_healthy
    redis:
      condition: service_healthy
    rabbitmq:
      condition: service_healthy
    minio:
      condition: service_healthy
  command: ["provision-services", "--strict"]
```

Then:

```yaml
migration:
  depends_on:
    setup:
      condition: service_completed_successfully
```

Then:

```yaml
backend-api:
  depends_on:
    migration:
      condition: service_completed_successfully
```

## 6.3 Staging/production bootstrap

Production should be non-interactive:

```bash
dotnet Synaptix.Setup.dll validate --environment production --strict
dotnet Synaptix.Setup.dll provision-services --environment production --strict
dotnet Synaptix.MigrationService.dll
```

Rules:

- no generated secrets in production,
- no weak defaults,
- no reset database,
- no EnsureCreated,
- no bundled seed fallback unless explicitly allowed,
- strict readiness enabled.

---

## 7. Implementation Plan

## Phase 1: Immediate Alpha/Beta Stabilization (implemented)

Priority: P0/P1

1. Add `docs/setup/SYNAPTIX_BOOTSTRAP_AND_SEED_SETUP_PLAN.md`.
2. Add `scripts/bootstrap-local.ps1`.
3. Add `scripts/bootstrap-local.sh`.
4. Add `docker/.env.generated.example`.
5. Update `.gitignore` for generated local secrets.
6. Replace secret fallback defaults in Compose with required env expressions.
7. Keep non-secret defaults in Compose.
8. Update `.env.example` to use placeholders.
9. Add startup validation for weak/default secrets outside local.
10. Add readiness checks to ensure super admin, admin ACL, missions, tiers, store items, skill nodes, season rewards, and questions exist.

## Phase 2: Setup CLI (implemented)

Priority: P1

Create:

```text
Synaptix.Setup
```

Commands:

```bash
init-local
validate
provision-services
provision-minio
upload-seeds
validate-seeds
create-super-admin
rotate-super-admin-password
status
```

Generate:

```text
docker/.env
.local/bootstrap/super-admin.local.txt
.local/bootstrap/bootstrap-status.json
```

## Phase 3: Service Provisioners (implemented for Alpha/Beta)

Priority: P1/P2

Implement:

- `PostgresSetupTask`
- `MongoSetupTask`
- `RedisSetupTask`
- `RabbitMqSetupTask`
- `MinioSetupTask`
- `ElasticsearchSetupTask`
- `SuperAdminSetupTask`

Each task should be:

- idempotent,
- strict-mode aware,
- testable,
- safe for local/staging/prod differences.

## Phase 4: Seed Manifest System

Priority: P1/P2

Add:

```text
config/seeds/seed-manifest.json
config/bootstrap/bootstrap.schema.json
```

Update `AppSeeder` and `MinioSeeder` to read manifest metadata:

- required vs optional,
- strict vs warning,
- seed version,
- object key,
- bundled fallback path,
- checksum/hash validation.

## Phase 5: CI/Release Validation

Priority: P1

Add GitHub Actions or local scripts:

```bash
dotnet run --project Synaptix.Setup -- validate --strict
dotnet test
docker compose -f docker/compose.yml config
```

Add release checklist output:

```text
.local/bootstrap/bootstrap-status.json
```

---

## 8. Original Recommended File Changes

This inventory records the original plan. Most listed setup files now exist; consult the implementation-status section and current repository before treating any item as pending.

### New files

```text
docs/setup/SYNAPTIX_BOOTSTRAP_AND_SEED_SETUP_PLAN.md
Synaptix.Setup/Synaptix.Setup.csproj
Synaptix.Setup/Program.cs
Synaptix.Setup/Bootstrap/BootstrapManifest.cs
Synaptix.Setup/Bootstrap/BootstrapRunner.cs
Synaptix.Setup/Secrets/SecretGenerator.cs
Synaptix.Setup/Secrets/SecretValidationService.cs
Synaptix.Setup/Services/PostgresSetupTask.cs
Synaptix.Setup/Services/MongoSetupTask.cs
Synaptix.Setup/Services/RedisSetupTask.cs
Synaptix.Setup/Services/RabbitMqSetupTask.cs
Synaptix.Setup/Services/MinioSetupTask.cs
Synaptix.Setup/Services/SuperAdminSetupTask.cs
config/bootstrap/bootstrap.example.json
config/bootstrap/bootstrap.schema.json
config/seeds/seed-manifest.json
scripts/bootstrap-local.ps1
scripts/bootstrap-local.sh
```

### Existing files to update

```text
docker/compose.yml
docker/.env.example
.gitignore
TycoonTycoon_Backend.slnx
Synaptix.MigrationService/Seeding/AppSeeder.cs
Synaptix.MigrationService/Seeding/MinioSeeder.cs
Synaptix.MigrationService/Seeding/MinioSeedOptions.cs
Synaptix.MigrationService/MigrationWorker.cs
Synaptix.MigrationService.Tests/Seeding/*
```

---

## 9. Security Rules

## Never commit

- generated `.env`,
- super admin generated password file,
- JWT signing secret,
- admin ops key,
- KMS service token,
- DB passwords,
- MinIO root password,
- RabbitMQ password,
- Redis password.

## Must fail startup outside local if

- `SUPER_ADMIN_PASSWORD=ChangeMe123!`,
- `ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION`,
- `JWT_SECRET_KEY` contains `change-me`,
- `MIGRATION_RESET_DATABASE=true`,
- `MIGRATION_ALLOW_ENSURE_CREATED=true`,
- required seed data is missing and strict mode is enabled.

## Recommended `.gitignore`

```gitignore
docker/.env
docker/.env.local
docker/.env.local.generated
.local/
*.local.secret
*.generated.secret
```

---

## 10. Final Recommended Target State

The ideal final workflow is:

```bash
# local
./scripts/bootstrap-local.ps1

# or manually
dotnet run --project Synaptix.Setup -- init-local
docker compose -f docker/compose.yml up -d
dotnet run --project Synaptix.Setup -- status
```

The final architecture should look like:

```text
Docker Compose
  └── starts infrastructure only

Synaptix.Setup
  └── generates local config, validates secrets, provisions infra services

Synaptix.MigrationService
  └── applies EF migrations and runs idempotent seeders

Synaptix.Backend.Api
  └── starts only after setup/migration success

Operator Dashboard
  └── starts only after API and admin bootstrap readiness
```

This approach gives you:

- one repeatable local setup,
- safer staging/production bootstrapping,
- no hardcoded secrets in Docker,
- clean super admin creation,
- Redis/Mongo/RabbitMQ/MinIO provisioning,
- better seed visibility,
- fewer silent startup failures,
- cleaner Alpha/Beta release readiness.
