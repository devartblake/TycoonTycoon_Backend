# Operator Dashboard May Cutover Completion Guide

**Status date:** 2026-05-14  
**Target cutover date:** 2026-05-15  
**Rollback window:** 2026-05-15 through 2026-06-12  

This is the single completion guide for closing
[`docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`](OPERATOR_DASHBOARD_PARITY_CHECKLIST.md)
and the related Operator Dashboard cutover markdowns.

Repo-side Django parity is complete. The remaining work is operational: apply migrations, validate
strict dashboard readiness, run the staging parallel-run with real operator accounts, collect sign-off,
record cutover evidence, and keep Blazor warm through the rollback window.

## Completion Snapshot

As of 2026-05-14, these items can be treated as complete from repo evidence:

- Django code parity is complete, including personalization, player stock, notification upgrade, user investigation, Plotly charts, and prototype-inspired UI.
- Migration/seed bootstrap is documented and wired through `Tycoon.MigrationService`.
- Rollback drill evidence is published.
- Blazor soft-freeze is documented; Blazor is rollback fallback only after Django cutover.
- May evidence templates and release artifact placeholders exist.
- CI/readiness automation is prepared and emits read-only JSON/Markdown evidence.
- Repo-side verification baseline is recorded for the May cutover package.

Do not mark the operational release gates complete until live staging/prod evidence is attached.

## Repo-Evidence Tasks Completed

These tasks are complete from repository evidence and can be referenced during the May closeout:

| Task | Status | Evidence |
|------|--------|----------|
| CI/readiness automation prepared | Complete | `.github/workflows/dotnet-ci.yml`, `.github/workflows/trivia-tycoon-ci.yml`, `.github/workflows/operator-cutover-readiness.yml`, `scripts/operator-cutover-readiness.py` |
| Evidence-capture package prepared | Complete | May evidence section in `OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`, release artifact checklist, readiness JSON slots |
| Repo verification baseline recorded | Complete | `git diff --check`, workflow YAML parse, `python -m py_compile scripts/operator-cutover-readiness.py`, `bash -n scripts/run-health-pass.sh`, `python manage.py check`, `python manage.py test dashboard.tests`, `docker compose -f docker/compose.yml config`, `dotnet restore TycoonTycoon_Backend.slnx`, `dotnet build TycoonTycoon_Backend.slnx --configuration Release --no-restore` |

Known caveat: the full local `Tycoon.Backend.Api.Tests` suite is not accepted as cutover evidence from this workstation because it collided with a local Redis password mismatch. CI now provisions an explicit Redis service for API-hosted test jobs.

## Open Gates

| Gate | Owner | Required evidence | Update after completion |
|------|-------|-------------------|-------------------------|
| EF migrations applied in staging | DBA / DevOps | Migration job logs or SQL transcript, final `__EFMigrationsHistory` row | `OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`, evidence pack |
| EF migrations applied in production | DBA / DevOps | Production migration transcript and verification query | `OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`, release artifacts |
| Strict migration/seed readiness passes | Backend / DevOps | `Tycoon.MigrationService` logs showing readiness pass | evidence pack |
| Staging parallel-run completed | QA Lead / Operators | Completed workflow matrix, discrepancies, screenshots/log links | `STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`, evidence pack |
| Sign-off captured | QA Lead / Backend Lead / On-call Operator | Signed table with date and notes | parity checklist, evidence pack, release artifacts |
| Cutover executed | DevOps | Upstream/route flip timestamp, image tags, smoke-check results | release artifacts |
| Blazor rollback window tracked | On-call / DevOps | Blazor fallback endpoint remains warm through 2026-06-12 | remaining tasks |

## CI And Readiness Automation Evidence

CI supports the May cutover evidence package but does not perform cutover:

- `.github/workflows/dotnet-ci.yml` restores, builds, and tests `TycoonTycoon_Backend.slnx` with the SDK from `global.json`.
- `.github/workflows/trivia-tycoon-ci.yml` optionally checks out the separate `trivia_tycoon` repository from `TRIVIA_TYCOON_REPOSITORY` and runs Flutter analysis/tests.
- `.github/workflows/operator-cutover-readiness.yml` is a manual readiness probe for staging/production that emits JSON and Markdown artifacts.

Run the readiness workflow after migration/bootstrap and again after route cutover smoke checks. Store the uploaded
`operator-cutover-readiness.json` and Markdown summary links in the evidence pack and release artifacts file.
The readiness workflow is read-only: it probes health/login/admin endpoints and records supplied gate statuses, but
humans still approve sign-off and authorize any route/upstream cutover.

The JSON result must include `generatedAtUtc`, `environment`, `commit`, `overallStatus`, `checks`, `releaseGates`,
and `nextActions` so the team can compare staging and production runs without reading raw logs.

## Run Order

### 1. Freeze Inputs

Record the exact staging and production artifacts before running migrations:

- Django dashboard image tag.
- Backend API image tag.
- Migration service image tag.
- Database target names or non-secret environment identifiers.
- Operator accounts used for staging validation, by role or email alias only.

Add these to [`docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`](OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md)
under the May evidence section.

### 2. Apply Migrations With Readiness Validation

Preferred path for staging and production is the migration service, not direct dashboard execution:

```bash
MIGRATION_MODE=MigrateAndSeed
MIGRATION_SEED_SOURCE=Auto
MIGRATION_DASHBOARD_READINESS_ENABLED=true
MIGRATION_DASHBOARD_READINESS_STRICT=true
MIGRATION_RESET_DATABASE=false
MIGRATION_ALLOW_ENSURE_CREATED=false
```

Expected readiness checks:

- EF migrations are applied.
- tiers, missions, questions, store items, skill nodes, and season rewards exist.
- configured super admin exists.
- configured super admin has an `Allow` / `SuperAdmin` `AdminEmailAcl` row.

Manual DBA fallback is [`docs/pending_migrations_2026-04-29.sql`](pending_migrations_2026-04-29.sql).
If the fallback script is used, verify:

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
```

Expected last committed migration after the May 15 schema-sync update:
`20260515102821_AddMayCutoverSchemaSync`.

If the manual DBA fallback script is used, it still ends at
`20260501090000_AddReasonToPersonalizationRecommendation`; apply the committed
May schema-sync migration afterward before marking the staging migration gate
ready.

### 3. Confirm Dashboard Login Readiness

In staging, confirm:

- Django `/login` accepts a real operator account.
- Sidebar profile email renders.
- `/api/operator/health` returns healthy/degraded service data and the expected permission scopes.
- A read-only or limited-permission account receives expected 403s for write operations.

Record the result in the evidence pack.

Optional automation:

```bash
python scripts/operator-cutover-readiness.py \
  --environment staging \
  --api-url "$API_URL" \
  --dashboard-url "$DASHBOARD_URL" \
  --operator-email "$OPERATOR_EMAIL"
```

Set `OPERATOR_PASSWORD_ENV` to the name of the environment variable containing the operator password.
Attach `artifacts/operator-cutover/operator-cutover-readiness.json` if the local script is used outside GitHub Actions.

### 4. Execute Staging Parallel-Run

Use [`docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`](STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md).

Minimum pass criteria:

- All required Django checks pass.
- No data-altering workflow produces a different result from Blazor.
- Django login succeeds for both staging operator accounts.
- No golden-path Django page returns 500.
- Supplemental Django-only checks pass for user investigation, personalization, player stock, notification scheduling/templates, and notification channel management.

Any failure must be linked to a defect before sign-off.

### 5. Capture Sign-Off

Required signers:

- QA Lead.
- Backend Lead.
- On-call Operator.

Sign-off may be recorded in the runbook, the evidence pack, or both. The parity checklist should only
mark sign-off complete after the evidence table is populated.

### 6. Cut Over

After sign-off:

- Flip the operator dashboard route/upstream to Django.
- Keep `operator-dashboard-blazor` warm through 2026-06-12.
- Smoke-check `/login`, `/`, `/users`, `/operations/notifications`, `/personalization`, and `/store/player-stock`.
- Record cutover timestamp, route owner, and smoke-check result in
  [`docs/OPERATOR_RELEASE_ARTIFACTS_2026-04.md`](OPERATOR_RELEASE_ARTIFACTS_2026-04.md).

### 7. Close Docs

Only after evidence exists:

- Mark release gates complete in `OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`.
- Mark publication checklist complete in `OPERATOR_RELEASE_ARTIFACTS_2026-04.md`.
- Keep `REMAINING_TASKS.md` Wave D open only for Blazor decommission until the rollback window ends.

## Hold Criteria

Hold cutover and keep Blazor primary if any of these occur:

- migration/readiness validation fails in strict mode.
- Django login fails for a real staging operator account.
- any data-altering workflow diverges from Blazor.
- any required Django page returns 500 on the golden path.
- QA Lead, Backend Lead, or On-call Operator withholds sign-off.

## Related Documents

- [`OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`](OPERATOR_DASHBOARD_PARITY_CHECKLIST.md)
- [`STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`](STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md)
- [`OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`](OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md)
- [`OPERATOR_DASHBOARD_MIGRATION_SEED_BOOTSTRAP.md`](OPERATOR_DASHBOARD_MIGRATION_SEED_BOOTSTRAP.md)
- [`pending_migrations_2026-04-29.sql`](pending_migrations_2026-04-29.sql)
- [`OPERATOR_RELEASE_ARTIFACTS_2026-04.md`](OPERATOR_RELEASE_ARTIFACTS_2026-04.md)
- [`REMAINING_TASKS.md`](REMAINING_TASKS.md)
- GitHub Actions artifacts from `operator-cutover-readiness`
