# Alpha/Beta Status Board

Last updated: `2026-05-20 America/New_York`

## Overall release status

Status: `not-launch-ready`

Repo-side preparation is mostly verified, but live staging/prod evidence is still missing. Treat `x` marks in `docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` as applicability markers only; a runbook row is not complete until it has a pass/fail result and evidence reference.

## P0 Alpha blockers

| Area | Status | Owner/Agent | Notes |
|---|---:|---|---|
| Local Docker startup | verified | devops-docker | Local compose smoke is recorded as passing in `docs/releases/ALPHA_RELEASE_CRITERIA.md` and `docs/alpha-beta/Synaptix_Alpha_Beta_Release_Plan.md`; staging compose/runtime evidence still separate. |
| PostgreSQL migrations | blocked | efcore-migration | Repo migrator, advisory lock, local SQL generation, and local compose migration are verified; staging PostgreSQL migration application and final `__EFMigrationsHistory` proof remain missing. |
| Auth/session identity | needs-review | dotnet-api | Backend admin and Django session login passed in compose readiness artifacts; staging player auth/signup/login smoke remains unchecked in Alpha release criteria. |
| `/users/me/wallet` authoritative read | blocked | wallet-economy | Backend exists, but staging authenticated wallet smoke remains unchecked. |
| Match submit / leaderboard idempotency | blocked | wallet-economy | `POST /quiz/complete` idempotency is repo-implemented; staging quiz completion duplicate-event and leaderboard update smoke remain unchecked. |
| Reward claim authority | blocked | wallet-economy | Server-side reward grant exists; staging golden-path reward proof remains pending. |
| Store catalog fallback | needs-review | backend-api | Store surfaces exist and avatar API path is in staging runbook; staging store catalog/avatar purchase evidence remains pending. |
| Admin endpoint protection | verified | security-kms | AdminOps/JWT protections and secure-channel support are in repo; KMS warning cleanup build passed, but Windows X25519 KMS tests still need platform decision. |
| Critical tests pass | needs-review | test-quality | 2026-05-18 release docs record backend/application test passes; latest local KMS test run fails on Windows X25519 CNG support, so full security test confidence is not green locally. |
| Staging operator parallel-run | blocked | operator-dashboard | Runbook rows remain open until result/evidence columns are populated; all six cutover release gates are pending in `artifacts/operator-cutover/operator-cutover-readiness.*`. |
| Rollback drill and sign-off | blocked | release-ops | Rollback procedure/sign-off rows remain unchecked in `docs/releases/ALPHA_RELEASE_CRITERIA.md`; four-role sign-off required before Alpha. |

## P1 Alpha important

| Area | Status | Owner/Agent | Notes |
|---|---:|---|---|
| Feature flags for non-essential modules | verified | repo-hygiene | Release docs record 14/14 feature gates enforced with `403 FeatureDisabled`; staging disabled-endpoint smoke still pending. |
| MinIO seed/storage loading | needs-review | devops-docker | MinIO diagnostics/media upload repo work exists; staging storage/media runbook checks are still pending. |
| Health/readiness checks | blocked | observability | Local health/readiness smoke passed; staging `GET /health/ready` with dependencies healthy remains unchecked. |
| Structured logs/correlation | needs-review | observability | Not marked as a launch blocker in latest release docs, but staging smoke should still inspect logs for `ERROR`/`CRITICAL`. |
| Sidecar fallback behavior | needs-review | personalization-sidecar | Non-essential Alpha modules are gated; keep sidecar/advanced personalization out of Alpha critical path. |
| CI build/test path | needs-review | test-quality | CI/helper migration startup drift is fixed; `release-gate.yml` still needs release-SHA staging evidence. |
| Operator dashboard cutover gates | blocked | operator-dashboard | `efMigrationsApplied`, `strictReadiness`, `parallelRun`, `signOff`, `cutover`, and `blazorRollbackWindow` all remain pending until live evidence exists. |
| Windows X25519 KMS test path | needs-review | security-kms | `Synaptix.Security.Kms.Tests` fails locally on Windows due CNG X25519 OID support; decide fix now vs CI/Linux-only verification. |

## Post-Alpha deferred

| Area | Reason deferred | Revisit trigger |
|---|---|---|
| Advanced personalization tuning | Not required for Alpha stability | After core loop telemetry is stable |
| Full dashboard parity beyond required operator workflows | Large surface area; Django-only checks are cutover validation, not full parity expansion | After API contract freeze and cutover gates close |
| Deep analytics dashboards | Not release-blocking | After event schema stabilizes |
| Multi-region deployment | Infrastructure maturity task | After Beta feedback |
| Automated economy balancing | Requires real gameplay data | Beta telemetry |
| Tournament/advanced-season dedicated flags | Beta hardening; Alpha controlled indirectly by existing gates | Before enabling matchmaking/tournaments in Beta |
| Store purchase provider flag | Purchases are not Alpha scope | Before public Beta or payment sandbox exposure |
