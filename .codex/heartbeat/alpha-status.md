# Alpha/Beta Status Board

Last updated: `2026-06-22 UTC`

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
| Admin endpoint protection | verified | security-kms | AdminOps/JWT protections and secure-channel support are in repo; KMS warning cleanup, Windows suite negotiation, and secure-channel replay/AAD hardening are locally verified. |
| Critical tests pass | needs-review | test-quality | 2026-05-18 release docs record backend/application test passes; 2026-05-21 local KMS tests and secure-channel filter tests pass. Live staging/release-gate evidence remains pending. |
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
| Windows X25519 KMS test path | verified | security-kms | Resolved with explicit `P256-HKDF-SHA256-AES256GCM` compatibility suite and capability-aware key exchange. `Synaptix.Security.Kms.Tests` passes locally on Windows. |

## Repo-side preparation (2026-06-22)

| Item | Status | Notes |
|---|---:|---|
| BE Packet E — Elasticsearch/Docker/CI/telemetry rename | verified | Completed 2026-05-09; all `tycoon-*` identifiers renamed to `synaptix-*` |
| BE Packet E — C# namespace rename (`Tycoon.*` → `Synaptix.*`) | verified | Completed 2026-05-22; 987 namespace declarations, 1,491 using statements updated |
| BE Packet E — Non-C# service rename | verified | `CryptoService`, `OperatorDashboard.Django/Vue`, `Sidecar` renamed 2026-05-22 |
| JWT issuer/audience update | verified | `SynaptixBackendApi` / `SynaptixFrontendApp`; handoff doc created |
| `store_purchases_enabled` feature flag | verified | Returns `403 FeatureDisabled`; defaults false; exposed in `/api/v1/app/config` |
| Missing Designer.cs for 20260512 migration | verified | Stub created consistent with existing project pattern |
| Production config template | verified | `appsettings.Production.example.json` created with all keys and `<REPLACE>` placeholders |
| Feature flag gates (14/14 Alpha endpoints) | verified | `403 FeatureDisabled` on all disabled endpoints confirmed in repo |
| KMS / Secure Channel | verified | X25519/P-256 cross-platform negotiation; AAD/replay/subject hardening; 15 endpoints protected |
| Apple IAP non-consumable restore (`POST /store/restore`) | verified | `FetchAppleReceiptProductsAsync` with prod→sandbox fallback; idempotent per `OriginalTransactionId`; requires `Iap:AppleSharedSecret` env var |
| Google Play RTDN webhook (`POST /store/iap/google/rtdn`) | verified | Full subscription lifecycle dispatch (grant/expiry/grace/revoke); OTP grant; Pub/Sub idempotency; always 200; requires `Iap:GoogleRtdnSubscriptionName` env var |
| `ItemKind` enum + `StoreItem` classification | verified | Migration `20260622000000_AddStoreItemKind`; back-fills Consumable/NonConsumable/Subscription from existing data |
| `IEntitlementService.UpdateExpiryAsync` | verified | Used by RTDN webhook and grace period handlers to update `expires_at_utc` without re-granting |
| `EntitlementExpiryJob` — Hangfire sweeper | verified | Runs every 15 min; bulk-expires lapsed entitlements via `ExecuteUpdateAsync`; zero row-by-row overhead |
| Subscription grace period lock (Stripe + PayPal) | verified | `past_due`/`SUSPENDED` → +3 days; `active`/`ACTIVATED` → true period end; consistent with Google RTDN grace window |

## Post-Alpha deferred

| Area | Reason deferred | Revisit trigger |
|---|---|---|
| Advanced personalization tuning | Not required for Alpha stability | After core loop telemetry is stable |
| Full dashboard parity beyond required operator workflows | Large surface area; Django-only checks are cutover validation, not full parity expansion | After API contract freeze and cutover gates close |
| Deep analytics dashboards | Not release-blocking | After event schema stabilizes |
| Multi-region deployment | Infrastructure maturity task | After Beta feedback |
| Automated economy balancing | Requires real gameplay data | Beta telemetry |
| Tournament/advanced-season dedicated flags | Beta hardening; Alpha controlled indirectly by existing gates | Before enabling matchmaking/tournaments in Beta |
| Reward Reactor endpoints (`/arcade/reactor/spin`, `/arcade/reactor/claim`) | Planning docs exist; implementation not yet built | Post-Alpha; legacy `/arcade/spin/claim` serves Alpha |
