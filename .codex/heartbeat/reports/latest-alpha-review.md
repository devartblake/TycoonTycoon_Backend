# Latest Alpha Heartbeat Review

Generated: `2026-05-20`

Status: `not-launch-ready`

## Summary

Alpha/Beta is not ready to ship yet. Repo-side release preparation is now reflected in `.codex/heartbeat/alpha-status.md`, but live environment evidence is still missing.

The strongest current evidence is in `docs/releases/ALPHA_RELEASE_CRITERIA.md`, the Alpha/Beta release plans, and `artifacts/operator-cutover/operator-cutover-readiness.*`. Local build, local backend tests, EF drift validation, idempotent SQL generation, and local compose smoke have been recorded as passing. Staging/prod migration evidence, staging smoke, release-gate workflow evidence, rollback drill proof, and final sign-off are still pending.

`docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` is an execution checklist, not completed evidence. Its Django/Blazor `x` marks show applicability or expected surface coverage; every row with blank Result/Evidence remains open until pass/fail evidence is captured.

## P0 blockers

| Area | Status | Evidence | Required next action |
|---|---:|---|---|
| Staging EF migrations | blocked/pending | `ALPHA_RELEASE_CRITERIA.md` still has staging PostgreSQL migration application unchecked | Run `Tycoon.MigrationService` or DBA fallback against staging and attach migration log plus final `__EFMigrationsHistory` row |
| Staging readiness | blocked/pending | `GET /health/ready` on staging remains unchecked | Verify staging dependencies and capture `200 OK` readiness evidence |
| Golden path API smoke | blocked/pending | Auth, wallet, quiz completion idempotency, leaderboard update, disabled endpoint `403 FeatureDisabled` checks remain unchecked | Run staging API smoke against migrated staging |
| Flutter live backend smoke | blocked/pending | Integration smoke items remain unchecked | Run `flutter test test/integration/live_backend_smoke_test.dart` against staging |
| Rollback drill/sign-off | blocked/pending | Rollback procedure and four-role sign-off remain unchecked | Complete non-prod rollback drill and collect Backend Lead, QA Lead, On-Call Engineer, Product Owner approval |
| Release-gate workflow | blocked/pending | `release-gate.yml` has not been run against staging/release SHA in this workspace | Run release gate and attach workflow/artifact evidence |

## P1 risks

| Area | Risk | Mitigation / next action |
|---|---|---|
| Heartbeat board drift | Previously stale heartbeat board has been reconciled, but it must stay synchronized after each verification pass | Update heartbeat status immediately whenever release criteria or runbook evidence changes |
| Feature flags | Known P1 issues remain for tournaments/advanced seasons without dedicated flags, legacy crypto `503` dead-code checks, and SignalR hub method defense-in-depth | Keep Alpha flags locked down; defer dedicated Beta hardening per `ALPHA_KNOWN_ISSUES.md` |
| Migration concurrency proof | Advisory lock is implemented, but concurrent-container validation is not checked off | Validate two migration containers on non-prod before blue-green or production multi-run patterns |
| Store purchases | Stripe/PayPal flows exist but are not Alpha scope and are not behind a dedicated purchase flag | Keep providers unconfigured for Alpha; add `store_purchases_enabled` before public Beta |
| Operator cutover | Compose readiness passes, but all six cutover release gates are still `pending` | Treat operator dashboard cutover as external evidence pending, not repo-complete |

## Latest Verification Failures

- `dotnet test Synaptix.Security.Kms.Tests/Synaptix.Security.Kms.Tests.csproj --configuration Release` failed on Windows because six secure-session tests try to create X25519 keys via CNG and receive `PlatformNotSupportedException` for OID `1.3.101.110`.
- This failure appears platform/test-environment specific and separate from the KMS warning cleanup. It should be resolved or marked CI-platform-specific before treating the full KMS test suite as green.
- `.codex/heartbeat/verification-log.md` now records the known passing, failing, and pending checks; keep it updated as live staging evidence arrives.

## Recommended Next Actions

1. Resolve or isolate the Windows X25519 KMS test failure so `Synaptix.Security.Kms.Tests` has a reliable local verification path.
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
- Beta-only hardening for tournament flags, SignalR hub filters, and payment-provider purchase gates.

## Needs User Decision

- Decide whether to fix the X25519 secure-session test for Windows now, or treat it as CI/Linux-only verification for Alpha.
- Confirm who owns staging credentials and the live evidence pack for migration, smoke, rollback, and sign-off.
