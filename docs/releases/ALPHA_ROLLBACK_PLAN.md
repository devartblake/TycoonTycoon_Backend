# Alpha Release — Rollback Plan

**Release:** alpha-beta-2026  
**Last updated:** 2026-05-26

This document defines the rollback procedure if the Alpha deployment needs to be reverted. Three levels of rollback are available, ordered from fastest to most comprehensive.

---

## Level 1 — Feature Flag Rollback (< 5 minutes, no downtime)

Use this when a specific feature causes issues but the core API is healthy.

**Prerequisites:** Admin credentials for the operator dashboard or direct API access.

**Steps:**

1. Authenticate as admin:
   ```bash
   curl -X POST https://api.synaptix.app/admin/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "<admin-email>", "password": "<admin-password>"}'
   ```

2. Disable the problematic feature flag:
   ```bash
   curl -X PATCH https://api.synaptix.app/api/v1/admin/config \
     -H "Authorization: Bearer <admin-jwt>" \
     -H "Content-Type: application/json" \
     -d '{"featureFlags": {"<flag_key>": false}}'
   ```

3. Verify the flag took effect:
   ```bash
   curl https://api.synaptix.app/api/v1/app/config
   # Check that the relevant flag shows false
   ```

4. Monitor for 5 minutes; confirm affected endpoint returns `403 FeatureDisabled`.

**Available flags:** `core_trivia_enabled`, `wallet_enabled`, `leaderboard_enabled`, `missions_enabled`, `store_enabled` (and all disabled-by-default flags)

**Note:** This rollback takes effect immediately — no restart or redeployment required.

---

## Level 2 — API Container Rollback (5–15 minutes, brief downtime)

Use this when the API itself is broken (startup failure, crash loop, regression).

**Prerequisites:** Docker registry access + previous image tag recorded before deployment.

**Steps:**

1. Identify the previous image tag (should be recorded in the deployment log):
   ```bash
   # Example: image was tagged with the git SHA before deployment
   PREVIOUS_TAG="sha-abc1234"
   ```

2. Stop the current API container:
   ```bash
   docker stop synaptix_backend_api
   docker rm synaptix_backend_api
   ```

3. Deploy the previous image:
   ```bash
   # Update docker/compose.yml backend-api image tag, or run directly:
   docker run -d \
     --name synaptix_backend_api \
     --network synaptix-net \
     -p 5000:5000 \
     [same environment variables as current deployment] \
     ghcr.io/devartblake/synaptix-backend:${PREVIOUS_TAG}
   ```

4. Verify health:
   ```bash
   curl https://api.synaptix.app/healthz
   curl https://api.synaptix.app/health/ready
   ```

5. Run minimal smoke test:
   ```bash
   curl https://api.synaptix.app/api/v1/app/config
   curl -X POST https://api.synaptix.app/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "<test-account>", "password": "<test-password>"}'
   ```

**Schema note:** API rollback does NOT roll back the database schema. If the previous API version is incompatible with the current schema, proceed to Level 3.

---

## Level 3 — Full Database Rollback (30–60 minutes, extended downtime)

Use this when a migration caused data corruption or schema incompatibility that cannot be resolved at the application level.

**For the detailed migration-level rollback steps and data risk assessment, see:**  
[`artifacts/migrations/rollback-notes.md`](../../artifacts/migrations/rollback-notes.md)

**High-level steps:**

1. **Stop all services** (API, migration service, sidecar, crypto service):
   ```bash
   docker compose -f docker/compose.yml stop backend-api migration sidecar crypto-service
   ```

2. **Restore from pre-deployment pg_dump backup:**
   ```bash
   # Backup must have been taken BEFORE running the migration container
   pg_restore \
     -h <postgres-host> \
     -U synaptix_user \
     -d synaptix_db \
     --clean \
     synaptix_backup_<timestamp>.dump
   ```

3. **Redeploy the previous API image** (follow Level 2 steps).

4. **Verify schema compatibility:**
   ```bash
   # The API's SchemaGate will block startup if schema is mismatched
   docker logs synaptix_backend_api | grep -i "schema\|migration"
   ```

5. **Run full smoke test:**
   ```bash
   # From Flutter project:
   flutter test test/integration/live_backend_smoke_test.dart
   ```

6. **Notify stakeholders** and document the incident.

---

## Pre-Deployment Checklist (run BEFORE deploying Alpha)

These steps should be completed before the migration container starts:

- [ ] Take a full database backup:
  ```bash
  pg_dump \
    -h <postgres-host> \
    -U synaptix_user \
    -d synaptix_db \
    -F c \
    -f synaptix_backup_$(date +%Y%m%d_%H%M%S).dump
  ```
- [ ] Record the current API image tag (pre-deployment)
- [ ] Confirm `artifacts/migrations/idempotent.sql` exists in CI artifacts for the release SHA
- [ ] Confirm `release-gate.yml` workflow passed on the release SHA
- [ ] Confirm on-call engineer is available for the deployment window

---

## Smoke Test After Rollback

Run the following in order to confirm the system is healthy:

```bash
# 1. Liveness
curl https://api.synaptix.app/healthz
# Expected: 200 {"status":"Healthy"}

# 2. Readiness
curl https://api.synaptix.app/health/ready
# Expected: 200 with all dependencies healthy

# 3. App config (unauthenticated)
curl https://api.synaptix.app/api/v1/app/config
# Expected: 200 with minimumClientVersion and feature flags

# 4. Auth flow
curl -X POST https://api.synaptix.app/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "<test-account>", "password": "<test-password>"}'
# Expected: 200 with accessToken

# 5. Full integration test (from Flutter project):
flutter test test/integration/live_backend_smoke_test.dart
```

All tests must pass before declaring the rollback successful.

---

## Escalation Contacts

| Role | Contact |
|------|---------|
| Backend Lead | — |
| On-Call Engineer | — |
| Database Admin | — |
| Platform / Infra | — |

Incident response SLA: acknowledge within 15 minutes of rollback trigger, restore within 60 minutes.
