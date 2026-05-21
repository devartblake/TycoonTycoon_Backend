# Current Blockers

Last updated: `2026-05-21 America/New_York`

## Active blockers

| ID | Priority | Area | Blocker | Evidence | Required decision/action |
|---|---|---|---|---|---|
| ALPHA-P0-001 | P0 Alpha blocker | Staging migrations | All EF migrations have not been proven applied to staging PostgreSQL. | `docs/releases/ALPHA_RELEASE_CRITERIA.md` has staging migration item unchecked. | Run `Tycoon.MigrationService` or DBA fallback; attach migration log/SQL transcript and final `__EFMigrationsHistory` row. |
| ALPHA-P0-002 | P0 Alpha blocker | Staging readiness | Staging `GET /health/ready` dependency health is not recorded. | Alpha release criteria readiness item unchecked. | Capture staging `200 OK` readiness with PostgreSQL, Redis, RabbitMQ, and MinIO healthy. |
| ALPHA-P0-003 | P0 Alpha blocker | Golden path API smoke | Auth/signup, wallet read, quiz completion idempotency, leaderboard update, and disabled endpoint `403 FeatureDisabled` checks are not attached. | API Surface and Feature Flag Gates remain unchecked in `ALPHA_RELEASE_CRITERIA.md`. | Run staging API smoke after migrations and record request/response evidence without secrets. |
| ALPHA-P0-004 | P0 Alpha blocker | Flutter integration | Live Flutter backend smoke has not been run against migrated staging. | Integration section remains unchecked in `ALPHA_RELEASE_CRITERIA.md`. | Run `flutter test test/integration/live_backend_smoke_test.dart` against staging and attach result. |
| ALPHA-P0-005 | P0 Alpha blocker | Release gate workflow | `release-gate.yml` has not been proven on the release SHA/staging environment. | Operations section remains unchecked; no workflow artifact referenced. | Run release-gate workflow and link artifact/log evidence. |
| ALPHA-P0-006 | P0 Alpha blocker | Rollback drill | Rollback procedure has not been tested on non-production. | Migration Safety and Operations rollback items unchecked. | Complete rollback drill, record restore/rollback timing and outcome. |
| ALPHA-P0-007 | P0 Alpha blocker | Alpha sign-off | Backend Lead, QA Lead, On-Call Engineer, and Product Owner sign-offs are empty. | Sign-off table in `ALPHA_RELEASE_CRITERIA.md` is blank. | Collect dated approvals after Must Pass evidence is green. |
| ALPHA-P0-008 | P0 Alpha blocker | Operator cutover | Staging parallel-run/cutover gates are not evidence-complete. | Runbook has open result/evidence cells; `operator-cutover-readiness.*` release gates are all `pending`. | Complete runbook checks, attach evidence, and only then move release gates from pending to pass. |
| ALPHA-P1-001 | P1 Alpha important | Heartbeat status drift | Heartbeat board previously showed all items not-started despite newer release evidence. | Latest alpha review flags board drift. | Keep heartbeat files synchronized with release criteria after each verification pass. |
| ALPHA-P1-003 | P1 Alpha important | Migration concurrency proof | Advisory lock exists but two-container migration proof is not complete. | `ALPHA_KNOWN_ISSUES.md` KI-004 and release criteria concurrent migrator item. | Validate two migration containers on non-prod before production multi-run/blue-green patterns. |
| ALPHA-P1-004 | P1 Alpha important | Store purchase flag gap | Stripe/PayPal purchase flows are not Alpha scope and lack dedicated purchase flag. | `ALPHA_KNOWN_ISSUES.md` KI-005. | Keep providers unconfigured for Alpha; add `store_purchases_enabled` before public Beta. |

## Resolved blockers

| ID | Resolved date | Resolution |
|---|---|---|
| ALPHA-RES-001 | 2026-05-18 | Local release build, backend tests, application tests, EF drift validation, idempotent SQL generation, and compose smoke were recorded as passing in release docs. |
| ALPHA-RES-002 | 2026-05-18 | Feature flag enforcement gap closed for 14/14 Alpha/Beta gates with `403 FeatureDisabled` behavior. |
| ALPHA-RES-003 | 2026-05-20 | KMS warning cleanup removed targeted `CS8604`, `CS9113`, `CS0168`, and `NU1510` warnings from release build. |
| ALPHA-RES-004 | 2026-05-21 | Windows KMS secure-session failure resolved with capability-aware X25519/P-256 negotiation; `Synaptix.Security.Kms.Tests` passes locally. |
