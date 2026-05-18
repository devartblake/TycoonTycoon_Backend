# Operator Dashboard Release Artifacts — April 2026

## Cutover Evidence Bundle

### May 2026 Cutover Completion

- Completion guide: `docs/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`
- Parity checklist: `docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`
- Staging parallel-run runbook: `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`
- Active evidence pack: `docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md` (May evidence section)
- Migration/seed bootstrap guide: `docs/OPERATOR_DASHBOARD_MIGRATION_SEED_BOOTSTRAP.md`
- Manual DBA fallback SQL: `docs/pending_migrations_2026-04-29.sql`
- CI readiness JSON: GitHub Actions artifact from `operator-cutover-readiness`
- CI readiness summary: GitHub Actions Markdown summary from `operator-cutover-readiness`

### Current Completion State

- [x] Repo-side Django parity completed.
- [x] Migration/seed bootstrap guide created.
- [x] Rollback drill evidence published.
- [x] Blazor soft-freeze documented.
- [x] May evidence templates created.
- [x] CI/readiness automation prepared.
- [x] Repo verification baseline recorded.
- [ ] Live staging/prod cutover gates completed with attached evidence.

### May 18 Final Gate Ledger

The May cutover remains open. Local compose evidence is useful for confidence, but
it is not a substitute for staging/production migration logs, route evidence, or
human sign-off.

| Gate | Status | Evidence still required |
|------|--------|-------------------------|
| `efMigrationsApplied` | Pending | Staging and production migration job logs or DBA SQL transcripts, plus final `__EFMigrationsHistory` verification |
| `strictReadiness` | Pending | Strict `Tycoon.MigrationService` readiness logs proving seed/readiness checks passed |
| `parallelRun` | Pending | Completed staging parallel-run matrix with real operators, evidence links, and discrepancy notes |
| `signOff` | Pending | QA Lead, Backend Lead, and On-call Operator approval rows populated |
| `cutover` | Pending | Production route/upstream flip timestamp, owner, active image tags, and smoke-check results |
| `blazorRollbackWindow` | Pending | Blazor fallback health tracked through 2026-06-12, or an approved policy exception |

### May 14 Task 1-2 Start Evidence

GitHub Actions is the source of truth for deployment and readiness evidence.

| Artifact | Status / link |
|----------|---------------|
| Source commit | `c7600548a5884e3c886e4e638c634e7462e2cc31` (`main`) |
| `trivia-tycoon-ci` | Success: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251645 |
| `alpha-p0-smoke` | Success: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251640 |
| `dotnet-ci` | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251638 |
| `compose-smoke` | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251635 |
| `compose-smoke` readiness artifact | `compose-readiness-results`, artifact id `6987729716`, digest `sha256:b7ca39b94bfe9cd22e2c1a93223ad1b8527083d7065c2aa24fee2c7881e8a54c` |
| Staging deployment image tags | Pending live GitHub Actions/deployment evidence from staging owner |
| Staging migration evidence | Pending live `Tycoon.MigrationService` job log or DBA SQL transcript |
| Staging readiness artifact | Pending `operator-cutover-readiness` staging artifact |

The compose readiness artifact is not staging evidence. It reported backend and Django health passing, but overall readiness failed because backend admin login returned `401` and Django session login returned `500`.

### May 14 Retry Outcome

GitHub retry evidence:

| Artifact | Status / link |
|----------|---------------|
| Retry source commit | `34b0a2bcd640d785ae87af1707eb3979e8668c79` (`main`) |
| `dotnet-ci` retry | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25870936043 |
| `compose-smoke` retry | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25870936027 |
| `compose-smoke` readiness artifact | `compose-readiness-results`, artifact id `6998930743`, digest `sha256:537e6f37f546d64dea4596980b400799e7dea76307c8d2601328ddabeac36e6e` |
| Gate decision | Hold before staging Task 3 until CI/smoke pass and live Task 1-2 evidence is attached |

Local remediation evidence:

- `compose-smoke` now passes locally against the compose stack after smoke-script fixes.
- `operator-cutover-readiness.py` passes locally against compose with backend health, Django health, backend admin login/profile, optional backend dashboard skip, and Django session login all passing.
- EF pending-model check passes locally with no model changes since the last migration.
- Full local `Tycoon.Backend.Api.Tests` remains blocked: 357 passed, 61 failed. Treat this as the current `dotnet-ci` blocker before moving to staging login readiness or staging parallel-run.

### May Publication Checklist

- [ ] Staging EF migration/readiness evidence attached.
- [ ] Production EF migration/readiness evidence attached.
- [ ] Staging `operator-cutover-readiness.json` attached.
- [ ] Production `operator-cutover-readiness.json` attached.
- [ ] Staging parallel-run workflow matrix completed.
- [ ] QA Lead, Backend Lead, and On-call Operator sign-off captured.
- [ ] Cutover timestamp and route/upstream owner recorded.
- [ ] Post-cutover smoke-check results recorded.
- [ ] Blazor fallback confirmed warm through 2026-06-12.
- [ ] Final readiness artifacts regenerated with all six release gates set to `pass` after evidence is attached.

### Parallel-Run

- Plan: `docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`
- Evidence pack: `docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`

### Rollback Drill

- Plan: `docs/OPERATOR_ROLLBACK_DRILL_STAGING_2026-Q2.md`
- Drill report: `docs/OPERATOR_ROLLBACK_DRILL_REPORT_2026-04-08.md`
- **Drill completion timestamp (UTC):** 2026-04-15 14:25:00 UTC
- **Release-note artifact link:** `docs/CHANGELOG.md` — entry `[2026-04-15] Staging Rollback Drill Artifacts Published`

### Rollback Drill Metrics Snapshot (2026-04-15)

- Failover activation: **36s**
- Primary restore: **54s**
- Workflow continuity (critical paths): **5/5 pass**
- Data loss events: **0**

## Publication Checklist

- [ ] Parallel-run evidence populated with real operator sign-offs.
- [ ] May cutover evidence populated before closing `docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`.
- [x] CI/readiness automation prepared for JSON/Markdown cutover evidence.
- [x] May release artifact placeholders and evidence slots created.
- [x] Repo verification baseline recorded in the May completion guide.
- [x] Rollback drill report populated with live timestamps and outcomes.
- [x] Any remediation tasks created and linked.
- [x] This artifact file linked from release notes.
- [x] Rollback drill completion timestamp and release-note artifact link recorded in migration docs and release checklist.
