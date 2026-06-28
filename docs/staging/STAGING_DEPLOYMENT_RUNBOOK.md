# Staging Deployment Runbook

## Purpose

Step-by-step guide for standing up the Synaptix staging environment, applying database
migrations, and validating the stack before Alpha/Beta release sign-off.

---

## Prerequisites

| Requirement | Detail |
|---|---|
| Linux host | 4 vCPU, 16 GB RAM, 100+ GB SSD recommended |
| Docker Engine | ≥ 24.0 |
| Docker Compose | v2 (bundled with Docker Desktop or `docker compose` plugin) |
| DNS A record | `api.DOMAIN`, `admin.DOMAIN` → staging host IP |
| Ports open | 80, 443 inbound; all others internal-only |
| Repo clone | `git clone` on the staging host |

---

## First-Time Setup

### 1. Create the staging environment file

```bash
cp docker/.env.staging.example docker/.env.staging
```

Edit `docker/.env.staging` and replace every `<generated-by-synaptix-setup>` value:

```bash
# Generate strong random secrets locally, then paste them in:
dotnet run --project Synaptix.Setup -- init-local
```

Mandatory values to set:

| Variable | Notes |
|---|---|
| `DOMAIN` | Your staging domain, e.g. `staging.synaptixplay.com` |
| `ACME_EMAIL` | Email for Let's Encrypt certificate notifications |
| `POSTGRES_PASSWORD` | Strong random password |
| `JWT_SECRET_KEY` | ≥ 32 characters, unique to staging |
| `ADMIN_OPS_KEY` | Strong random key for admin ops header |
| `MINIO_ROOT_PASSWORD` | Strong random password |
| `REDIS_PASSWORD` | Strong random password |
| `ELASTIC_PASSWORD` | Strong random password |
| `SUPER_ADMIN_EMAIL` | Staging operator email |
| `SUPER_ADMIN_PASSWORD` | Strong random password |

> **Note:** `compose.staging.yml` uses the Let's Encrypt **staging** CA. Certificates
> will show as untrusted in browsers. This is expected. Switch to the production CA
> by removing the `--certificatesresolvers.le.acme.caserver` line from
> `docker/compose.staging.yml` once the stack is validated.

---

## Bootstrap — Full Stack

Bring up all services (data stores, migration, API):

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.staging.yml \
  --env-file docker/.env.staging \
  up -d --build
```

The startup order is enforced by `depends_on`:

```text
postgres + mongodb + redis + elasticsearch + rabbitmq (healthy)
  → setup (completes)
  → migration (applies all 35 EF migrations, seeds data, exits 0)
  → backend-api (starts, serves /healthz)
  → operator-dashboard (starts)
```

---

## Verify Migrations

### Check migration logs

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.staging.yml \
  --env-file docker/.env.staging \
  logs migration
```

Expected output contains lines like:

```
[INF] Applying migration: 20260325180201_InitialCreate
...
[INF] Applying migration: 20260601000000_AddPasswordResetTokens
[INF] All 35 migrations applied successfully.
[INF] Seeding complete.
[INF] Dashboard readiness validated.
```

### Verify in PostgreSQL

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.staging.yml \
  --env-file docker/.env.staging \
  exec postgres psql -U synaptix_user -d synaptix_db \
  -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";"
```

Expected: `count = 35`

---

## Run Smoke Tests

### Local compose smoke (from the repo root on the staging host)

```bash
bash scripts/compose-smoke.sh
```

Or with make:

```bash
make smoke-live
```

The smoke script checks:
- `GET /healthz` → 200
- `GET /health/ready` → 200
- `GET /api/v1/app/config` → 200
- `POST /auth/login` → 200 (with test credentials)
- `GET /users/me/wallet` → 200

---

## Validate Release Gates

### 1. Trigger `release-gate.yml` via GitHub Actions

Go to **Actions → Database Release Gate** → **Run workflow**:

| Input | Value |
|---|---|
| `environment` | `staging` |
| `api_url` | `https://api.staging.yourdomain.com` |

The workflow:
1. Verifies migration artifacts exist in CI
2. Checks `/healthz` and `/health/ready`
3. Verifies disabled features return `403 FeatureDisabled`
4. Publishes a readiness report

### 2. Trigger `operator-cutover-readiness.yml` via GitHub Actions

Go to **Actions → Operator Cutover Readiness** → **Run workflow**:

| Input | Value |
|---|---|
| `environment` | `staging` |
| `api_url` | `https://api.staging.yourdomain.com` |
| `dashboard_url` | `https://admin.staging.yourdomain.com` |
| `operator_email` | Your operator email |

Fill in the 6 gate inputs based on observed staging state. The workflow produces
`artifacts/operator-cutover/` which is required for 4-role sign-off.

---

## 4-Role Sign-Off

After all gates pass, complete the sign-off table in
`docs/releases/ALPHA_RELEASE_CRITERIA.md`:

| Role | Name | Date |
|---|---|---|
| Backend Lead | | |
| QA Lead | | |
| On-Call | | |
| Product Owner | | |

Once all four are signed, Alpha launch is unblocked.

---

## Rollback

If any step fails, follow `docs/releases/ALPHA_ROLLBACK_PLAN.md`:

- **Level 1** (< 5 min): disable feature flags via `PATCH /api/v1/config`
- **Level 2** (5–15 min): roll back API container to previous image tag
- **Level 3** (30–60 min): restore PostgreSQL from `pg_dump` backup

Migration-level rollback steps are in `artifacts/migrations/rollback-notes.md`.

---

## Tear Down

```bash
docker compose \
  -f docker/compose.yml \
  -f docker/compose.staging.yml \
  --env-file docker/.env.staging \
  down -v
```

`-v` removes named volumes (database data). Omit to preserve data across restarts.
