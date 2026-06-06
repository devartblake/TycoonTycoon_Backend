# Alpha/Beta Extension Readiness Audit

**Date:** 2026-06-05  
**Status:** Not launch-ready  
**Audience:** Engineering, QA, DevOps, Product, On-call  
**Purpose:** Release-focused go/no-go audit for the Alpha/Beta extension window.

## Current Overall Status

Alpha/Beta missed the original target window, but the release criteria should not be lowered to compensate. The correct path is to continue through the extension until the required evidence is captured or a new release timeframe is approved.

Repo-side and local Docker preparation are strong: setup provisioning, service wiring, read-only setup visibility, Mongo analytics persistence, Elasticsearch validation, pgAdmin startup, and the operator investigation ID path have recent local verification evidence. That evidence does not replace staging readiness, rollback, release-gate, Flutter live smoke, operator cutover, or release sign-off.

| Area | Current state | Launch impact |
|---|---|---|
| Repo/local backend setup | Mostly verified locally | Supports release prep, but does not prove staging readiness |
| Staging infrastructure | Evidence pending | Blocks Alpha |
| Golden-path API smoke | Evidence pending on staging | Blocks Alpha |
| Flutter live backend smoke | Evidence pending | Blocks Alpha |
| Operator dashboard cutover | Staging parallel-run evidence pending | Blocks operator cutover and Alpha sign-off |
| Rollback drill | Evidence pending | Blocks Alpha |
| Release gate workflow | Evidence pending on release SHA | Blocks Alpha |
| Four-role sign-off | Empty | Blocks Alpha |

## Evidence Reconciliation Rule

- Prefer the freshest `.codex/heartbeat/*` and `.codex/heartbeat/verification-log.md` entries over older planning documents.
- Treat unchecked staging, rollback, live smoke, release-gate, and sign-off rows as pending until evidence is attached.
- Treat local setup, MongoDB, Redis, Elasticsearch, pgAdmin, and operator-dashboard fixes as completed local evidence only.
- Keep intentionally deferred setup work out of Alpha blockers: `setup:write`, setup mutation UI/API, KMS-backed setup-secret protection, secret rotation, destructive operations, and infrastructure recreation.

## Completed Repo And Local Evidence

| Evidence | Status | Source |
|---|---:|---|
| Local release build and major backend/application test passes were recorded on 2026-05-18 | Complete | `.codex/heartbeat/verification-log.md`, `docs/releases/ALPHA_RELEASE_CRITERIA.md` |
| EF drift validation and local idempotent SQL generation were recorded as passing | Complete | `.codex/heartbeat/verification-log.md` |
| Local compose smoke was recorded as passing | Complete | `.codex/heartbeat/verification-log.md`, `docs/releases/ALPHA_RELEASE_CRITERIA.md` |
| Synaptix rename, JWT contract, production config template, and `store_purchases_enabled` flag prep are recorded as complete | Complete | `.codex/heartbeat/alpha-status.md`, `docs/releases/ALPHA_RELEASE_CRITERIA.md` |
| `Synaptix.Setup` MongoDB/Redis hardening tests passed | Complete | `.codex/heartbeat/verification-log.md` |
| Repeated local `provision-services` runs completed with 7 succeeded, 0 errors | Complete | `.codex/heartbeat/verification-log.md` |
| MongoDB app-user auth through `synaptix_analytics`, legacy-user warning/removal behavior, and Redis logical DB validation are recorded | Complete | `docs/setup/Synaptix_Setup_UI_CLI_Architecture_Handoff.md` |
| Read-only Backend `/admin/setup/*` and Django `/settings/setup/*` setup visibility are implemented with focused tests | Complete | `docs/setup/Synaptix_Setup_UI_CLI_Architecture_Handoff.md`, `.codex/heartbeat/verification-log.md` |
| Mongo analytics HTTP/gRPC persistence path and local smoke passed | Complete | `.codex/heartbeat/verification-log.md` |
| Elasticsearch credential alignment and local setup validation passed | Complete | `.codex/heartbeat/verification-log.md` |
| pgAdmin `.local` email startup loop was fixed locally with `admin@synaptix.app` | Complete | `.codex/heartbeat/verification-log.md` |
| Operator investigation `usr_<guid>` player-route 404 path was fixed and Django focused tests passed | Complete | `.codex/heartbeat/verification-log.md` |

## Remaining P0 Alpha Blockers

| ID | Blocker | Required evidence | Next action |
|---|---|---|---|
| P0-01 | Staging EF migrations not proven applied | Migration log or DBA transcript plus final `__EFMigrationsHistory` proof | Run `Synaptix.MigrationService` or DBA-approved SQL path against staging |
| P0-02 | Staging readiness not captured | `GET /health/ready` returns `200 OK` with PostgreSQL, Redis, RabbitMQ, and MinIO healthy | Probe migrated staging and attach sanitized output |
| P0-03 | Staging golden-path API smoke not attached | Auth/signup, wallet read, quiz completion idempotency, leaderboard update, and disabled endpoint `403 FeatureDisabled` evidence | Run the staging API smoke after migrations and readiness pass |
| P0-04 | Flutter live backend smoke not run against migrated staging | `flutter test test/integration/live_backend_smoke_test.dart` result or replacement signed smoke evidence | Run Flutter smoke against the same staging release candidate |
| P0-05 | Operator dashboard staging parallel-run incomplete | Populated result/evidence rows for the staging runbook and operator cutover readiness gates | Execute the runbook with real staging operator accounts |
| P0-06 | Rollback drill not tested | Non-production or staging rollback transcript with timing and outcome | Execute rollback plan and record restore/rollback evidence |
| P0-07 | Release gate workflow not proven | `release-gate.yml` artifact/log for the release SHA | Trigger the workflow and attach artifact reference |
| P0-08 | Alpha sign-off missing | Backend Lead, QA Lead, On-Call Engineer, and Product Owner dated approval | Hold sign-off meeting after all must-pass evidence is green |

## Remaining P1 Risks

| Risk | Why it matters | Disposition |
|---|---|---|
| Migration advisory-lock concurrent-container proof is not complete | Advisory lock exists, but two-migrator behavior is not evidence-backed | P1; required before blue-green or multi-run production patterns |
| Operator cutover gates remain evidence-pending | Django is canonical, but staging parallel-run and cutover proof remain open | P1/P0 depending on whether operator cutover is tied to Alpha ship |
| Broad Backend API suite previously had unrelated failures outside focused setup tests | Focused setup tests passed, but broad suite confidence needs refreshed evidence or triage | P1; refresh before final release confidence |
| Older docs still contain stale launch-time or code-complete claims | Stale optimism can hide missing staging evidence | P1; use this audit and heartbeat files for release decisions |

## Beta And Post-Alpha Deferred Work

These items should remain visible but must not block the Alpha extension unless Product changes scope.

| Deferred area | Reason |
|---|---|
| KMS-backed setup-secret protection | `PlaintextLocal` is active for local bootstrap; KMS setup protector remains deferred |
| `setup:write`, setup mutation UI/API, secret rotation, and destructive operations | CLI-only or future audited design scope |
| Advanced personalization tuning and deep analytics dashboards | Requires stable player behavior data |
| Multi-region deployment and automated economy balancing | Platform maturity tasks after single-region stability and telemetry |
| Tournament-specific and advanced-season hardening | Beta scope; Alpha uses disabled or indirect gates |
| SignalR hub-method defense-in-depth | Current path gate is Alpha-sufficient; hub filters are Beta hardening |
| Flutter package-root/store identifier rename | Awaiting store/legal plan |

## Stale Or Superseded Claims To Ignore For Go/No-Go

| Claim pattern | Current interpretation |
|---|---|
| "Repo-side preparation is 100% complete" | Useful historical summary, but newer setup/operator/Mongo work changed local evidence. It still does not prove staging launch readiness. |
| "No further code changes needed before Alpha" | Too broad. Recent fixes after 2026-05-26 show code/doc changes were still needed locally; final go/no-go must follow current evidence. |
| "Closed Beta / Soft Launch ready" in older backlog docs | Superseded by release criteria and heartbeat blockers requiring staging proof. |
| Older dashboard ownership references to Blazor as authoritative | Superseded by Django as canonical dashboard; Blazor remains rollback/comparison only. |
| KMS setup protection implied as implemented | Incorrect for setup secrets. Phase 1 abstractions exist; `KmsSetupSecretProtector` is deferred. |
| Local setup success used as staging readiness proof | Incorrect. Local Docker success is necessary evidence, not sufficient launch evidence. |

## Recommended Next Execution Order

1. Freeze the extension scope around the Alpha golden path and documented must-pass criteria.
2. Apply or prove all EF migrations on staging.
3. Capture staging `GET /health/ready` after migrations.
4. Run staging API golden-path smoke and feature-disabled checks.
5. Run Flutter live backend smoke against the same staging release candidate.
6. Execute the operator dashboard staging parallel-run and populate evidence rows.
7. Run rollback drill and record restore/rollback timing.
8. Trigger `release-gate.yml` on the release SHA and attach the artifact.
9. Triage or refresh evidence for the broad Backend API suite before final confidence review.
10. Collect four-role sign-off only after every must-pass item is green.

## Source Files Reviewed

- `.codex/heartbeat/alpha-status.md`
- `.codex/heartbeat/current-blockers.md`
- `.codex/heartbeat/deferred-post-alpha.md`
- `.codex/heartbeat/verification-log.md`
- `.codex/heartbeat/reports/latest-alpha-review.md`
- `docs/releases/ALPHA_RELEASE_CRITERIA.md`
- `docs/releases/ALPHA_KNOWN_ISSUES.md`
- `docs/releases/ALPHA_DISABLED_FEATURES.md`
- `docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`
- `docs/setup/Synaptix_Setup_UI_CLI_Architecture_Handoff.md`
- `docs/alpha-beta/REMAINING_TASKS.md`
- `docs/alpha-beta/synaptix_remaining_work.md`
- `docs/alpha-beta/Synaptix_Alpha_Beta_Release_Plan.md`
- `Synaptix.OperatorDashboard.Django/README.md`

## Final Go/No-Go Statement

Alpha/Beta remains **No-Go** on 2026-06-05. The extension should continue with the existing release gates intact. The next release decision should be based on staging migration/readiness proof, golden-path API and Flutter smoke evidence, operator cutover evidence, rollback drill proof, `release-gate.yml` artifact, and four-role sign-off.
