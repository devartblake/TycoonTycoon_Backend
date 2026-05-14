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
