# Alpha/Beta Database Migration Automation Implementation Plan
## Synaptix / TycoonTycoon Backend
### Target: Reliable Migration Workflow Before Alpha/Beta Release

---

# 1. Purpose

This document defines an implementation plan for streamlining and automating the database migration process for the Alpha/Beta release.

The goal is to eliminate manual database patching and create a repeatable migration workflow using the existing backend infrastructure:

- .NET / EF Core
- PostgreSQL
- Docker / Docker Compose
- GitHub Actions
- Existing backend projects
- Existing deployment pipeline concepts
- Existing health checks and smoke testing approach

This plan does **not** introduce a new migration platform such as Flyway, Liquibase, or Atlas. The recommended path is to use the existing .NET/EF Core foundation properly.

---

# 2. Primary Objective

Create a controlled migration workflow where:

```text
Code change
  -> EF migration created
  -> CI validates migration
  -> idempotent SQL script generated
  -> migration runner image built
  -> database migrator runs before API deployment
  -> smoke tests verify release readiness
```

---

# 3. Problem Being Solved

The current risk is that database schema changes may be applied inconsistently through:

- manual SQL patches
- API startup migration logic
- developer-specific local commands
- unclear production/staging deployment order
- missing schema drift validation

For Alpha/Beta, this is risky because wallet, rewards, leaderboard, auth, and gameplay systems depend on consistent schema state.

---

# 4. Recommended Migration Architecture

## 4.1 Dedicated Migration Runner

Create a dedicated .NET console project:

```text
src/Tycoon.DatabaseMigrator/
```

This project should:

- connect to PostgreSQL
- acquire a PostgreSQL advisory lock
- list current migrations
- list pending migrations
- apply pending EF Core migrations
- optionally run baseline seeders
- log all actions
- exit with code `0` on success
- exit with non-zero code on failure

---

# 5. Migration Ownership Rule

## Production Rule

Main APIs should **not** automatically apply migrations during production startup.

Avoid this pattern in production API startup:

```csharp
db.Database.Migrate();
```

## Allowed Use

Automatic migration during API startup may remain available only for:

```text
- local development
- test containers
- throwaway dev environments
```

Production and Alpha/Beta environments should use the dedicated migrator.

---

# 6. Required Implementation Phases

---

# Phase 1 — Add Migration Runner Project

## Goal

Create a standalone migration executable.

## New Project

```bash
dotnet new console -n Tycoon.DatabaseMigrator -o src/Tycoon.DatabaseMigrator
```

## Add References

The migrator should reference the project that contains:

- `DbContext`
- EF Core infrastructure
- migrations
- database configuration

Example:

```bash
dotnet add src/Tycoon.DatabaseMigrator/Tycoon.DatabaseMigrator.csproj reference src/Tycoon.Infrastructure/Tycoon.Infrastructure.csproj
```

Adjust project names to match the actual repo structure.

---

## Recommended Files

```text
src/Tycoon.DatabaseMigrator/
 ├── Program.cs
 ├── MigrationRunner.cs
 ├── MigrationLock.cs
 ├── MigrationReport.cs
 ├── appsettings.json
 └── Tycoon.DatabaseMigrator.csproj
```

---

# Phase 2 — Implement PostgreSQL Advisory Lock

## Goal

Prevent two migrator instances from running at the same time.

## Recommended Behavior

Before applying migrations:

```sql
SELECT pg_advisory_lock(987654321);
```

After migration:

```sql
SELECT pg_advisory_unlock(987654321);
```

## Why This Matters

Without a lock, parallel deployments or duplicate containers could attempt schema changes simultaneously.

That can cause:

- deadlocks
- failed migrations
- partial schema changes
- broken API startup

---

# Phase 3 — Implement Migration Runner Logic

## Required Runtime Flow

```text
Start migrator
  ↓
Load configuration
  ↓
Connect to PostgreSQL
  ↓
Acquire advisory lock
  ↓
Print current migration
  ↓
Print pending migrations
  ↓
Apply pending migrations
  ↓
Run baseline seeders if enabled
  ↓
Release advisory lock
  ↓
Exit successfully
```

---

## Required Logging

The migrator should log:

```text
- environment name
- database host
- current migration
- pending migrations
- migration start time
- migration end time
- success/failure status
```

Do not log:

```text
- passwords
- full connection strings
- secrets
- JWT keys
```

---

# Phase 4 — Add Idempotent SQL Script Generation

## Goal

CI should generate a SQL artifact for review, backup, and emergency manual execution.

## Command

```bash
dotnet ef migrations script --idempotent \
  --project src/Tycoon.Infrastructure \
  --startup-project src/Tycoon.Api \
  --output artifacts/migrations/idempotent.sql
```

Adjust project paths to match the actual backend structure.

---

## Output Folder

```text
artifacts/
 └── migrations/
      ├── idempotent.sql
      ├── migration-manifest.json
      └── rollback-notes.md
```

---

# Phase 5 — Add Migration Manifest

## Goal

Each CI run should produce a migration manifest.

## Example

```json
{
  "release": "alpha-beta-2026",
  "environment": "staging",
  "generatedAtUtc": "2026-05-14T00:00:00Z",
  "database": "tycoon",
  "requiresBackup": true,
  "script": "artifacts/migrations/idempotent.sql"
}
```

---

# Phase 6 — Add Docker Migrator Image

## New Dockerfile

```text
deploy/dockerfiles/Dockerfile.migrator
```

## Purpose

Build a small container that runs only the migrator.

---

## Example Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore src/Tycoon.DatabaseMigrator/Tycoon.DatabaseMigrator.csproj
RUN dotnet publish src/Tycoon.DatabaseMigrator/Tycoon.DatabaseMigrator.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Tycoon.DatabaseMigrator.dll"]
```

If the project remains on .NET 8, use:

```text
mcr.microsoft.com/dotnet/sdk:8.0
mcr.microsoft.com/dotnet/runtime:8.0
```

---

# Phase 7 — Add Docker Compose Migrator Service

## Goal

Allow local/dev/staging compose environments to run migrations before API startup.

## Example

```yaml
db-migrator:
  build:
    context: .
    dockerfile: deploy/dockerfiles/Dockerfile.migrator
  environment:
    ASPNETCORE_ENVIRONMENT: Development
    ConnectionStrings__DefaultConnection: ${DATABASE_URL}
  depends_on:
    postgres:
      condition: service_healthy
  restart: "no"
```

---

## API Dependency Pattern

For local/dev compose:

```yaml
game-api:
  depends_on:
    db-migrator:
      condition: service_completed_successfully
```

This ensures APIs do not start against an outdated schema.

---

# Phase 8 — Add GitHub Actions Migration Validation

## Goal

CI should fail if migrations are broken.

## Recommended Workflow Name

```text
.github/workflows/database-migrations.yml
```

---

## CI Checks

The workflow should:

```text
1. checkout repo
2. setup .NET
3. restore
4. build
5. run tests
6. install dotnet-ef
7. generate idempotent SQL
8. upload migration artifact
```

---

## Example Workflow

```yaml
name: Database Migration Validation

on:
  pull_request:
    branches:
      - main
      - develop
      - release/*
  push:
    branches:
      - main
      - develop
      - release/*

jobs:
  validate-migrations:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run tests
        run: dotnet test --no-build --configuration Release

      - name: Install EF Tool
        run: dotnet tool install --global dotnet-ef

      - name: Generate migrations directory
        run: mkdir -p artifacts/migrations

      - name: Generate idempotent SQL
        run: |
          dotnet ef migrations script --idempotent \
            --project src/Tycoon.Infrastructure \
            --startup-project src/Tycoon.Api \
            --output artifacts/migrations/idempotent.sql

      - name: Generate migration manifest
        run: |
          cat > artifacts/migrations/migration-manifest.json <<EOF
          {
            "release": "alpha-beta-2026",
            "generatedAtUtc": "$(date -u +"%Y-%m-%dT%H:%M:%SZ")",
            "requiresBackup": true,
            "script": "idempotent.sql"
          }
          EOF

      - name: Upload migration artifacts
        uses: actions/upload-artifact@v4
        with:
          name: database-migration-artifacts
          path: artifacts/migrations/
```

Adjust .NET version and project paths as needed.

---

# Phase 9 — Add Release Migration Gate

## Goal

No Alpha/Beta deployment should proceed unless migration succeeds.

## Required Release Steps

```text
1. Backup database
2. Run db-migrator
3. Confirm migration success
4. Start API services
5. Run smoke tests
6. Validate golden path
```

---

# Phase 10 — Add Smoke Tests After Migration

## Minimum Smoke Tests

After migration, verify:

```text
GET /healthz
GET /readyz
POST /auth/login
GET /users/me/wallet
GET /leaderboard
GET /questions/bootstrap or equivalent
```

If any smoke test fails, block deployment.

---

# 7. Alpha/Beta Critical Tables To Protect

The migration workflow must protect tables related to:

```text
- users
- auth/refresh tokens
- wallet balances
- wallet transactions
- questions
- game sessions
- match results
- leaderboard entries
- rewards
- anti-cheat flags
```

These areas should receive extra testing.

---

# 8. Reward and Wallet Migration Rules

Because wallet and rewards are high-risk, all schema changes must support:

```text
- idempotency
- audit history
- no duplicate grants
- rollback documentation
- nullable columns before required columns where possible
- safe backfills
```

---

# 9. Migration Safety Rules

## Rule 1 — Never Drop Production Columns Immediately

Use phased removal:

```text
Phase A: stop writing column
Phase B: stop reading column
Phase C: verify no usage
Phase D: drop column in later release
```

---

## Rule 2 — Add Columns Safely

Prefer:

```sql
ALTER TABLE table_name ADD COLUMN new_column TEXT NULL;
```

Then backfill.

Then make required later if needed.

---

## Rule 3 — Avoid Long Locking Operations During Release

Large table changes should be:

```text
- reviewed separately
- tested on copied production-sized data
- scheduled outside critical release windows
```

---

## Rule 4 — Every Migration Needs Rollback Notes

Even if EF rollback is not fully automated, document:

```text
- what changed
- what data is affected
- whether rollback is safe
- manual recovery steps
```

---

# 10. Recommended Rollback Notes Template

Create:

```text
artifacts/migrations/rollback-notes.md
```

Template:

```markdown
# Rollback Notes

## Release
alpha-beta-2026

## Migration Summary
Describe the schema changes.

## Data Risk
Low / Medium / High

## Can This Be Rolled Back Automatically?
Yes / No

## Manual Rollback Steps
1.
2.
3.

## Backup Required?
Yes

## Verification After Rollback
- health check
- login
- wallet load
- leaderboard load
```

---

# 11. Required Environment Variables

The migrator should support:

```text
ASPNETCORE_ENVIRONMENT
ConnectionStrings__DefaultConnection
MIGRATIONS_ENABLE_SEEDING
MIGRATIONS_LOCK_KEY
MIGRATIONS_TIMEOUT_SECONDS
```

---

# 12. Suggested Config Defaults

```json
{
  "Migrations": {
    "EnableSeeding": false,
    "LockKey": 987654321,
    "TimeoutSeconds": 300
  }
}
```

---

# 13. Implementation Checklist

## Backend Project

- [ ] Create `Tycoon.DatabaseMigrator`
- [ ] Add project references
- [ ] Add config loading
- [ ] Add DbContext wiring
- [ ] Add advisory lock
- [ ] Add pending migration logging
- [ ] Add migration apply command
- [ ] Add optional seeding flag
- [ ] Add success/failure exit codes

---

## Docker

- [ ] Add `Dockerfile.migrator`
- [ ] Add `db-migrator` compose service
- [ ] Add API dependency on migrator for local/dev
- [ ] Confirm migrator exits after completion

---

## CI/CD

- [ ] Add migration validation workflow
- [ ] Generate idempotent SQL
- [ ] Generate migration manifest
- [ ] Upload artifacts
- [ ] Fail build on migration generation errors

---

## Release

- [ ] Backup database
- [ ] Run migration runner
- [ ] Run smoke tests
- [ ] Confirm golden path
- [ ] Deploy API
- [ ] Monitor logs

---

# 14. Alpha/Beta Timeline

## Day 1

```text
- Create migrator project
- Wire DbContext
- Implement logging
```

## Day 2

```text
- Add advisory lock
- Apply migrations
- Add exit codes
```

## Day 3

```text
- Add Dockerfile.migrator
- Add docker-compose db-migrator service
- Test locally from clean database
```

## Day 4

```text
- Add GitHub Actions workflow
- Generate idempotent SQL
- Upload artifacts
```

## Day 5

```text
- Add smoke tests
- Test release branch flow
- Document rollback notes
```

---

# 15. Definition of Done

The migration automation work is complete when:

```text
- db-migrator runs successfully against local PostgreSQL
- db-migrator applies pending migrations
- duplicate migrator runs are protected by advisory lock
- CI generates idempotent SQL
- CI uploads migration artifacts
- APIs start only after migrations succeed in local/dev compose
- smoke tests pass after migration
- rollback notes exist for release
```

---

# 16. What To Avoid Before Alpha/Beta

Do NOT add:

```text
- Flyway
- Liquibase
- Atlas
- custom schema diff engines
- complex multi-database orchestration
- automatic destructive migrations
- production auto-migrate inside API startup
```

The current need is controlled automation, not new tooling complexity.

---

# 17. Final Recommendation

For Alpha/Beta, the correct migration strategy is:

```text
EF Core migrations
+ dedicated .NET migrator project
+ PostgreSQL advisory lock
+ Docker migrator service
+ GitHub Actions validation
+ idempotent SQL artifact
+ release smoke tests
```

This gives the project a stable, repeatable, and production-friendly migration process without adding unnecessary third-party complexity.
