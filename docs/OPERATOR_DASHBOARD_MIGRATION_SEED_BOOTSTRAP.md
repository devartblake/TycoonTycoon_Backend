# Operator Dashboard Migration and Seed Bootstrap

`Tycoon.OperatorDashboard.Django` does not run backend database migrations directly. The dashboard depends on the backend API, and the backend API depends on the one-shot `migration` service completing successfully.

## Dev Bootstrap

1. Copy the Docker env template if needed:

   ```bash
   cp docker/.env.example docker/.env
   ```

2. Start the stack:

   ```bash
   docker compose -f docker/compose.yml up -d --build
   ```

3. The startup order is:

   - PostgreSQL, MongoDB, Redis, Elasticsearch, RabbitMQ, and MinIO become healthy.
   - `migration` applies `Tycoon.Backend.Migrations`.
   - `migration` seeds tiers, missions, super admin, admin ACL, store items, skill nodes, season rewards, and questions.
   - `migration` validates Django dashboard readiness.
   - `backend-api` starts after `migration` completes.
   - `operator-dashboard` starts after `backend-api` and `sidecar` are healthy.

4. Dev dashboard login:

   - URL: `http://localhost:8200/login`
   - Email: `admin@tycoon.local`
   - Password: `ChangeMe123!`
   - Matching ops key: `ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION`

## Seed Source

The migration service supports:

- `MIGRATION_SEED_SOURCE=Auto`: try MinIO/object storage first, then bundled files.
- `MIGRATION_SEED_SOURCE=Bundled`: read published files from `Tycoon.MigrationService/seeds`.
- `MIGRATION_SEED_SOURCE=MinIO`: require object storage seed files.

Default dev Docker uses `Auto`, so a fresh machine can bootstrap from bundled seed files without a manual MinIO upload.

Seed object keys are relative to the configured bucket:

- `seeds/store-items.json`
- `seeds/skill-nodes.json`
- `seeds/season-rewards.json`
- `seeds/questions.json`

Do not include the bucket name in the key. The bucket is configured separately with `MINIO_BUCKET`.

## Staging and Production

For staging/prod, run the same migration service as a pre-start job with:

```bash
MIGRATION_MODE=MigrateAndSeed
MIGRATION_SEED_SOURCE=Auto
MIGRATION_DASHBOARD_READINESS_ENABLED=true
MIGRATION_DASHBOARD_READINESS_STRICT=true
MIGRATION_RESET_DATABASE=false
MIGRATION_ALLOW_ENSURE_CREATED=false
```

Strict readiness fails the migration job if dashboard-critical seed data is missing. This protects the Django dashboard from starting against a partially seeded backend.

Optional Elastic rebuild:

```bash
MIGRATION_MODE=MigrateSeedAndRebuildElastic
REBUILD_ELASTIC=true
```

Windows helper:

```powershell
./scripts/run-dashboard-bootstrap.ps1 -Mode docker
./scripts/run-dashboard-bootstrap.ps1 -Mode local
./scripts/run-dashboard-bootstrap.ps1 -Mode docker -ResetDev
./scripts/run-dashboard-bootstrap.ps1 -Mode docker -RebuildElastic
```

## Readiness Checks

The migration job validates:

- EF migrations are recorded as applied.
- tiers exist.
- missions exist.
- questions exist.
- store items exist.
- skill nodes exist.
- season reward rules exist.
- configured super admin user exists.
- configured super admin has an `Allow` / `SuperAdmin` `AdminEmailAcl` entry.

If `MIGRATION_DASHBOARD_READINESS_STRICT=false`, failures are logged as warnings. Keep strict mode enabled for staging and production.
