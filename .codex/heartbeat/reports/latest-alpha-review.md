# Latest Alpha Heartbeat Review

Generated: `2026-05-26`

Status: `not-launch-ready`

## Summary

Alpha/Beta is not ready to ship yet. As of 2026-05-26, **repo-side preparation is 100% complete**. All remaining blockers require live staging or production infrastructure — there are no further code changes needed on the repo side before Alpha.

The strongest current evidence is in `docs/releases/ALPHA_RELEASE_CRITERIA.md`, the Alpha/Beta release plans, and `artifacts/operator-cutover/operator-cutover-readiness.*`. Local build, local backend tests, EF drift validation, idempotent SQL generation, and local compose smoke have been recorded as passing. Staging/prod migration evidence, staging smoke, release-gate workflow evidence, rollback drill proof, and final sign-off are still pending.

`docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` is an execution checklist, not completed evidence. Every row with a blank Result/Evidence column remains open until pass/fail evidence is captured against live staging.

## Repo-side completions (2026-05-26)

- BE Packet E complete: Elasticsearch, Docker, CI/telemetry, C# namespaces, non-C# services, JWT contracts all renamed to `Synaptix.*`/`synaptix-*`.
- `store_purchases_enabled` feature flag added: all payment endpoints return `403 FeatureDisabled` when off; flag defaults `false` for Alpha; visible in `GET /api/v1/app/config`.
- Missing `Designer.cs` for migration `20260512150000_AddQuestionStatusColumns` created.
- `appsettings.Production.example.json` production config template created.
- `docs/handoffs/FRONTEND_REBRAND_HANDOFF.md` created for Flutter team (JWT contract change, re-auth coordination).
- CHANGELOG updated through 2026-05-26.
- ALPHA_RELEASE_CRITERIA.md checked for all repo-verifiable items.

## P0 blockers (all staging-dependent)

| Area | Status | Evidence | Required next action |
|---|---:|---|---|
| Staging EF migrations | blocked/pending | `ALPHA_RELEASE_CRITERIA.md` still has staging PostgreSQL migration application unchecked | Run `Synaptix.MigrationService` or DBA fallback against staging; attach migration log plus final `__EFMigrationsHistory` row |
| Staging readiness | blocked/pending | `GET /health/ready` on staging remains unchecked | Verify staging dependencies (PostgreSQL, Redis, RabbitMQ, MinIO) and capture `200 OK` readiness evidence |
| Golden path API smoke | blocked/pending | Auth, wallet, quiz completion idempotency, leaderboard update, disabled endpoint `403 FeatureDisabled` checks remain unchecked | Run staging API smoke against migrated staging |
| Flutter live backend smoke | blocked/pending | Integration smoke items remain unchecked | Run `flutter test test/integration/live_backend_smoke_test.dart` against staging |
| Rollback drill/sign-off | blocked/pending | Rollback procedure and four-role sign-off remain unchecked | Complete non-prod rollback drill and collect Backend Lead, QA Lead, On-Call Engineer, Product Owner approval |
| Release-gate workflow | blocked/pending | `release-gate.yml` has not been run against staging/release SHA | Run release gate and attach workflow/artifact evidence |
| Operator cutover | blocked/pending | All six cutover release gates (`efMigrationsApplied`, `strictReadiness`, `parallelRun`, `signOff`, `cutover`, `blazorRollbackWindow`) remain pending | Complete staging parallel-run runbook and populate result/evidence entries |
| CI/CD `migration-artifacts` | blocked/pending | Idempotent SQL artifact not yet attached from a CI run on main | Trigger CI pipeline on release SHA and confirm `migration-artifacts` artifact is attached |

## P1 risks

| Area | Risk | Mitigation / next action |
|---|---|---|
| Heartbeat board drift | Board must stay synchronized after each staging verification pass | Update heartbeat status immediately whenever release criteria or runbook evidence changes |
| Migration concurrency proof | Advisory lock is implemented, but concurrent-container validation is not checked off | Validate two migration containers on non-prod before blue-green or production multi-run patterns |
| Operator cutover | Compose readiness passes locally, but all six cutover release gates are still `pending` | Treat operator dashboard cutover as external evidence pending, not repo-complete |

## Latest Verification Notes

- BE Packet E namespace rename is complete as of 2026-05-22. No `Tycoon.` identifiers remain in C# source.
- `store_purchases_enabled` flag is added (2026-05-26). Purchase flows return `403 FeatureDisabled` by default for Alpha.
- `appsettings.Production.example.json` now exists; all production config keys are documented.
- Windows KMS secure-session failure has been resolved. `Synaptix.Security.Kms.Tests` passes locally with capability-aware X25519/P-256 negotiation.
- `dotnet build TycoonTycoon_Backend.slnx --configuration Release` passes; remaining warnings are unrelated nullable warnings in backend API test projects.
- `.codex/heartbeat/verification-log.md` records all known passing, failing, and pending checks.

## Recommended Next Actions

1. Provide staging environment access to the backend team.
2. Run staging migration/readiness proof and attach logs to `ALPHA_RELEASE_CRITERIA.md`.
3. Execute the staging parallel-run runbook and populate result/evidence entries for every applicable row.
4. Run staging golden-path API smoke and Flutter live backend smoke.
5. Execute rollback drill and collect the four release sign-offs.
6. Run `release-gate.yml` against the release SHA and attach artifacts.

## What To Defer

- Advanced personalization tuning.
- Full dashboard parity beyond the cutover-required operator workflows.
- Deep analytics dashboards.
- Multi-region deployment.
- Automated economy balancing.
- Reward Reactor endpoints (`/arcade/reactor/spin`, `/arcade/reactor/claim`) — planning docs exist; implementation post-Alpha.
- Beta-only hardening for tournament flags and SignalR hub filters.

## Needs User Decision

- Confirm who owns staging credentials and the live evidence pack for migration, smoke, rollback, and sign-off.
