# Verification Log

Records build, test, Docker, migration, and smoke-check results from Codex tasks.

| Timestamp | Task ID | Command | Result | Notes |
|---|---|---|---:|---|
| 2026-05-18 | alpha-beta-release-evidence | `dotnet build TycoonTycoon_Backend.slnx --configuration Release` | pass | Release docs record 0 errors; newer 2026-05-20 build also passed after KMS warning cleanup. |
| 2026-05-18 | alpha-beta-release-evidence | `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build` | pass | Release docs record 417 passed, 1 skipped, 0 failed. |
| 2026-05-18 | alpha-beta-release-evidence | `dotnet test Tycoon.Backend.Application.Tests/Tycoon.Backend.Application.Tests.csproj --configuration Release --no-build` | pass | Release docs record 198 passed, 0 failed. |
| 2026-05-18 | alpha-beta-release-evidence | `bash scripts/validate-ef-schema.sh` | pass | Release docs record no pending model changes. |
| 2026-05-18 | alpha-beta-release-evidence | `dotnet ef migrations script --idempotent` with `Tycoon.MigrationService` startup | pass | Local idempotent SQL generation recorded in Alpha/Beta release docs. |
| 2026-05-18 | alpha-beta-release-evidence | `bash scripts/compose-smoke.sh` | pass | Full local compose smoke recorded as passing after migration and smoke-script fixes. |
| 2026-05-14 | operator-cutover-readiness | `scripts/operator-cutover-readiness.py` against compose-smoke | pending | Required checks passed, but release gates `efMigrationsApplied`, `strictReadiness`, `parallelRun`, `signOff`, `cutover`, and `blazorRollbackWindow` remain pending. |
| 2026-05-20 | kms-warning-cleanup | `dotnet build TycoonTycoon_Backend.slnx --configuration Release` | pass | Targeted KMS/security warnings removed; unrelated backend API test nullable warnings remain. |
| 2026-05-20 | kms-warning-cleanup | `dotnet test Synaptix.Security.Kms.Tests/Synaptix.Security.Kms.Tests.csproj --configuration Release` | fail | Six secure-session tests fail on Windows due X25519 CNG `PlatformNotSupportedException` for OID `1.3.101.110`. |
| 2026-05-21 | kms-secure-session-cross-platform | `dotnet test Synaptix.Security.Kms.Tests/Synaptix.Security.Kms.Tests.csproj --configuration Release` | pass | 22 passed; secure-session tests now negotiate X25519 when supported and P-256 compatibility on Windows. |
| 2026-05-21 | kms-secure-session-cross-platform | `dotnet build TycoonTycoon_Backend.slnx --configuration Release` | pass | Full release build passed; remaining warnings are unrelated nullable warnings in backend API test projects. |
| 2026-05-20 | admin-media-minio | `python Tycoon.OperatorDashboard.Django/manage.py test dashboard.tests` | pass | 284 Django dashboard tests passed after MinIO diagnostics/media upload changes. |
| 2026-05-20 | admin-media-minio | `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --filter "AdminMedia"` | pass | 13 AdminMedia tests passed. |
| pending | alpha-live-staging | Staging EF migration application | pending | Requires live staging migration log or DBA transcript plus final `__EFMigrationsHistory` row. |
| pending | alpha-live-staging | Staging `GET /health/ready` | pending | Must prove PostgreSQL, Redis, RabbitMQ, and MinIO healthy. |
| pending | alpha-live-staging | Staging API golden-path smoke | pending | Auth, wallet, quiz completion idempotency, leaderboard, disabled endpoint `403 FeatureDisabled`. |
| pending | alpha-live-staging | Flutter live backend smoke | pending | `flutter test test/integration/live_backend_smoke_test.dart` against migrated staging. |
| pending | operator-cutover | Staging parallel-run runbook | pending | Runbook rows need result/evidence entries before cutover gate can pass. |
| pending | alpha-release-ops | Rollback drill and four-role sign-off | pending | Required before Alpha launch. |
| pending | alpha-release-ops | `release-gate.yml` on release SHA | pending | Required workflow artifact not attached yet. |
