# Operator Parallel-Run Evidence Pack — April 8, 2026

## Execution Status

- **Task:** Execute staging parallel-run with real operator accounts and collect sign-off evidence.
- **Current state:** Started, awaiting live staging access + operator availability.
- **Blocked by:** This workspace does not have interactive access to staging infrastructure or real operator credentials.

## Preparation Completed

- Parallel-run execution playbook created: `docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`.
- Workflow matrix and sign-off table prepared for live session.
- Required evidence checklist prepared.

## Live Session Inputs Required (to complete)

1. Staging environment revision and deployed image tags.
2. Named operators participating in validation.
3. Workflow-by-workflow parity outcomes.
4. Operator approvals (Y/N) and comments.
5. Follow-up defect links for any parity gaps.

## Sign-off Capture Table (fill during live run)

| Operator | Date (UTC) | Approved (Y/N) | Notes |
| --- | --- | --- | --- |
|  |  |  |  |
|  |  |  |  |

## Outcome

- **Completion state:** Not complete yet (requires live staging execution).
- **Next action:** Run the session in staging and update this file with completed evidence.

---

## May 2026 Active Evidence Section

Use this section for the May 14/15 completion pass described in
[`docs/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`](OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md).

### Environment Identifiers

| Item | Value / link |
|------|--------------|
| Staging environment | Pending live staging confirmation. Expected dashboard URL from runbook: `https://operator-staging.synaptix.internal/` |
| Django dashboard image tag | Pending GitHub Actions/deployment evidence from staging owner |
| Backend API image tag | Pending GitHub Actions/deployment evidence from staging owner |
| Migration service image tag | Pending GitHub Actions/deployment evidence from staging owner |
| Blazor fallback image tag / endpoint | Pending GitHub Actions/deployment evidence from staging owner; compose fallback service is `operator-dashboard-blazor` |
| Database target reference, non-secret | Pending DBA/DevOps staging target reference |
| Migration evidence link | Pending staging migration job log or DBA SQL transcript |
| Dashboard readiness log link | Pending staging `Tycoon.MigrationService` strict readiness log |
| Staging readiness JSON artifact | Pending `operator-cutover-readiness` staging artifact |
| Production readiness JSON artifact | |

### Task 1-2 Start Snapshot

| Item | Evidence |
|------|----------|
| Evidence source of truth | GitHub Actions / deployment evidence |
| Current repo commit | `c7600548a5884e3c886e4e638c634e7462e2cc31` (`main`) |
| Latest successful `trivia-tycoon-ci` run | https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251645 |
| Latest successful `alpha-p0-smoke` run | https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251640 |
| Latest `dotnet-ci` run | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251638 |
| Latest `compose-smoke` run | Failed: https://github.com/devartblake/TycoonTycoon_Backend/actions/runs/25843251635 |
| Compose readiness artifact | `compose-readiness-results`, artifact id `6987729716`, digest `sha256:b7ca39b94bfe9cd22e2c1a93223ad1b8527083d7065c2aa24fee2c7881e8a54c` |
| Compose readiness result | `overallStatus: fail`; backend health and Django health passed, admin login returned `401`, Django session login returned `500` |
| Compose release gate fields | `efMigrationsApplied: pass`, `strictReadiness: pass`, `parallelRun/signOff/cutover/blazorRollbackWindow: pending` |
| Staging migration status | Not complete from this workspace; requires live staging migration job or DBA SQL transcript |
| Staging gate status | Keep open until staging migration/readiness artifacts are attached |

### CI Readiness Automation

Use `.github/workflows/operator-cutover-readiness.yml` to generate read-only JSON/Markdown evidence after
migration/bootstrap and after cutover smoke checks. Attach the uploaded artifact links here; the workflow does
not approve sign-off or perform the route cutover.

**Preparation status:** Complete as of 2026-05-14. The workflow, JSON probe script, release artifact
slots, and evidence tables are in place. The runs below remain pending until staging/prod owners execute
the workflow and attach uploaded artifacts.

| Run | Environment | Overall status | JSON artifact link | Markdown summary link | Notes |
|-----|-------------|----------------|--------------------|-----------------------|-------|
| Pre-cutover readiness | Staging | Pending | | | |
| Pre-cutover readiness | Production | Pending | | | |
| Post-cutover smoke | Production | Pending | | | |

### Operator Accounts Used

Do not record passwords or secrets.

| Alias / role | Permission profile | Django login verified | Blazor login verified | Notes |
|--------------|--------------------|-----------------------|-----------------------|-------|
| Operator A | Full permissions | | | |
| Operator B | Limited/read-only or second full operator | | | |

### Workflow Results

| Workflow | Result | Evidence link | Defect link / notes |
|----------|--------|---------------|---------------------|
| Auth and permissions | Pending | | |
| Command center / health | Pending | | |
| Users triage and bulk guardrails | Pending | | |
| User investigation workbench | Pending | | Django-only supplemental |
| Moderation | Pending | | |
| Security audit | Pending | | |
| Questions queue | Pending | | |
| Economy player | Pending | | |
| Store flash sales / policies / analytics | Pending | | |
| Player stock override and bulk reset | Pending | | Django-only supplemental |
| Game events | Pending | | |
| Seasons | Pending | | |
| Anti-cheat flags | Pending | | |
| Notifications send/history/dead-letter | Pending | | |
| Notification schedule/template/channel admin | Pending | | Django-only supplemental |
| Event queue | Pending | | |
| Storage and media | Pending | | |
| Personalization overview/player/rules | Pending | | Django-only supplemental |
| Avatar purchase API path | Pending | | API-level supplemental |

### Sign-off

| Role | Name | Date (UTC) | Approved (Y/N) | Notes |
|------|------|------------|----------------|-------|
| QA Lead | | | | |
| Backend Lead | | | | |
| On-call Operator | | | | |
