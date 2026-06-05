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
| 2026-05-21 | secure-channel-replay-aad | `dotnet test Synaptix.Security.Kms.Tests/Synaptix.Security.Kms.Tests.csproj --configuration Release` | pass | 28 passed; payload tests now cover AAD binding, replayed sequence/nonce rejection, timestamp skew, and subject binding. |
| 2026-05-21 | secure-channel-replay-aad | `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --filter SecureChannel` | pass | 10 passed; middleware requires secure sequence/replay nonce headers and passes derived AAD/replay metadata to KMS. |
| 2026-05-20 | admin-media-minio | `python Tycoon.OperatorDashboard.Django/manage.py test dashboard.tests` | pass | 284 Django dashboard tests passed after MinIO diagnostics/media upload changes. |
| 2026-05-20 | admin-media-minio | `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --filter "AdminMedia"` | pass | 13 AdminMedia tests passed. |
| 2026-05-22 | packet-e-rename | C# namespace rename (`Tycoon.*` → `Synaptix.*`) across 1,001 files | pass | 987 namespace declarations + 1,491 using statements updated; solution file and all .csproj ProjectReferences updated. |
| 2026-05-22 | packet-e-rename | Non-C# service rename (CryptoService, OperatorDashboard.Django/Vue, Sidecar) | pass | Directory renames via `git mv`; Dockerfiles, service names, and compose references updated. |
| 2026-05-22 | packet-e-rename | JWT issuer/audience update (`TycoonBackendApi`/`TycoonFrontendApp` → `SynaptixBackendApi`/`SynaptixFrontendApp`) | pass | `JwtSettings.cs` defaults updated; `FRONTEND_REBRAND_HANDOFF.md` created for Flutter team. |
| 2026-05-26 | merge-and-p0-prep | `git merge --no-ff origin/main` (14 commits, PRs #383–#385) | pass | Clean merge, no conflicts. |
| 2026-05-26 | merge-and-p0-prep | Created `20260512150000_AddQuestionStatusColumns.Designer.cs` | pass | Missing stub added; matches empty `BuildTargetModel` pattern of all other Designer files. |
| 2026-05-26 | merge-and-p0-prep | Created `appsettings.Production.example.json` | pass | All required production keys documented; all secrets are `<REPLACE>` placeholders. |
| 2026-05-26 | merge-and-p0-prep | Added `store_purchases_enabled` flag to `StoreSystemStatusSupport.cs` and `StoreEndpoints.cs` | pass | `EnsurePaymentsEnabledAsync` returns `403 FeatureDisabled` when flag is off; default `false`. |
| 2026-05-26 | merge-and-p0-prep | Added `storePurchasesEnabled` to `AppConfigEndpoints.cs` feature flag response | pass | Flag now visible to clients in `GET /api/v1/app/config`. |
| 2026-06-03 | synaptix-setup-provisioning | `dotnet test Synaptix.Setup.Tests/Synaptix.Setup.Tests.csproj --no-restore` | pass | 9 passed, 0 failed; covers MongoDB admin connection/auth DB behavior and Redis structured/raw configuration parsing. |
| 2026-06-03 | synaptix-setup-provisioning | `dotnet build TycoonTycoon_Backend.slnx --no-restore` | pass | Full solution build completed with 0 errors; existing unrelated warnings remained. |
| 2026-06-03 | synaptix-setup-provisioning | repeated `docker compose ... run --rm setup` | pass | Repeated runs completed with 7 tasks succeeded and 0 errors; MongoDB app-user auth and Redis logical databases validated. |
| 2026-06-03 | synaptix-setup-provisioning | setup run with `Setup__Mongo__RemoveLegacyAdminAppUser=true` | pass | Correct auth-database user validated before the legacy same-named `admin` user was removed; subsequent default run had no legacy warning. |
| 2026-06-04 | synaptix-setup-doc-sync | setup documentation contract/reference audit and `git diff --check` | pass | Setup docs synchronized to `688e35b0`; proposed UI/API surfaces clearly marked as not implemented; staging claims unchanged. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --no-restore --filter AdminSetupEndpointsTests` | pass | 8 focused Backend setup contract, permission-profile, and secret-leak tests passed. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `python manage.py test dashboard.tests.test_admin_setup_client dashboard.tests.test_setup_views` | pass | 7 Django setup client, BFF, permission, and page tests passed. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `python manage.py check` | pass | Django system check reported no issues. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `python manage.py test dashboard.tests` | pass | Full Django dashboard suite passed: 357 tests. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `dotnet build TycoonTycoon_Backend.slnx --no-restore` | pass | Full solution build completed with 0 warnings and 0 errors. |
| 2026-06-04 | synaptix-setup-readonly-visibility | rebuilt/restarted `backend-api` and `operator-dashboard`; unauthenticated setup route smoke | partial | Containers healthy; all Django setup UI/BFF routes redirected unauthenticated callers and Backend setup rejected ops-key-only access with 403. Existing local super-admin credentials did not authenticate, so authenticated live-page smoke remains outstanding. |
| 2026-06-04 | synaptix-setup-readonly-visibility | `dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --no-restore` | fail | Broad suite: 480 passed, 1 skipped, 28 failed outside the new AdminSetup tests, primarily existing auth/store/match expectations. The focused AdminSetup suite remains 8/8 passing. |
| 2026-06-04 | synaptix-setup-roadmap-history | `dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --no-restore --filter AdminSetupEndpointsTests` | pass | 11 focused Backend setup status/history contract, permission-profile, and secret-leak tests passed. |
| 2026-06-04 | synaptix-setup-roadmap-history | `python manage.py test dashboard.tests.test_admin_setup_client dashboard.tests.test_setup_views` | pass | 9 Django setup client, history, BFF, permission, and page tests passed. |
| 2026-06-04 | synaptix-setup-roadmap-history | `python manage.py check` | pass | Django system check reported no issues. |
| 2026-06-04 | synaptix-setup-roadmap-history | `dotnet build TycoonTycoon_Backend.slnx --no-restore` | pass | Full solution build completed with 0 warnings and 0 errors. |
| 2026-06-04 | synaptix-setup-roadmap-history | `docker compose --env-file docker/.env -f docker/compose.yml config --quiet` | pass | Compose rendered successfully with current setup wiring. |
| 2026-06-04 | synaptix-setup-roadmap-history | `git diff --check` | pass | No whitespace errors; Git reported existing LF/CRLF normalization warnings only. |
| 2026-06-04 | mongo-analytics-write-path | `dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --no-restore --filter "AnalyticsCompatibilityEndpointsTests|SidecarGrpcServiceTests"` | pass | 17 focused HTTP/gRPC analytics tests passed; covers raw event persistence, rollups, duplicate idempotency, unsupported-event behavior, and warning-only rollup indexing failures. |
| 2026-06-04 | mongo-analytics-write-path | `dotnet test Synaptix.Setup.Tests/Synaptix.Setup.Tests.csproj --no-restore` | pass | 9 setup tests passed after Mongo setup index hardening. |
| 2026-06-04 | mongo-analytics-write-path | `docker compose --env-file docker/.env -f docker/compose.yml run --rm setup` | pass | Provision-services completed with 7 succeeded, 0 errors; legacy Mongo `Id` indexes were dropped because event IDs serialize as `_id`. |
| 2026-06-04 | mongo-analytics-write-path | Docker live analytics smoke via `POST /analytics/track` | pass | Backend returned accepted=1/skipped=0; Mongo confirmed raw event=1, daily rollup=1, player daily rollup=1 for the smoke event. |
| 2026-06-04 | mongo-analytics-write-path | `dotnet build TycoonTycoon_Backend.slnx --no-restore` | pass | Full solution build completed with 0 warnings and 0 errors. |
| 2026-06-04 | mongo-analytics-write-path | `git diff --check` | pass | No whitespace errors; Git reported LF/CRLF normalization warnings only. |
| 2026-06-04 | elasticsearch-credential-alignment | authenticated Elasticsearch health probe | pass | Local Elasticsearch accepted the Compose-resolved `ELASTIC_PASSWORD`; cluster reported yellow for the single-node local stack. |
| 2026-06-04 | elasticsearch-credential-alignment | `docker compose --env-file docker/.env -f docker/compose.yml run --rm setup` | pass | Fresh setup image completed with 7 succeeded, 0 errors, 0 warnings, including Elasticsearch validation. |
| 2026-06-04 | elasticsearch-credential-alignment | live `/analytics/track` smoke plus Elasticsearch document lookup | pass | Backend returned accepted=1/skipped=0; Elasticsearch found daily and player rollup docs in `synaptix-daily-rollups-write` and `synaptix-player-daily-rollups-write`. |
| 2026-06-05 | pgadmin-email-alignment | `docker compose --env-file docker/.env -f docker/compose.yml --profile dev up -d --force-recreate pgadmin` | pass | pgAdmin recreated with `admin@synaptix.app`; invalid `.local` email validation loop disappeared and `http://localhost:5050/` returned 200. |
| pending | alpha-live-staging | Staging EF migration application | pending | Requires live staging migration log or DBA transcript plus final `__EFMigrationsHistory` row. |
| pending | alpha-live-staging | Staging `GET /health/ready` | pending | Must prove PostgreSQL, Redis, RabbitMQ, and MinIO healthy. |
| pending | alpha-live-staging | Staging API golden-path smoke | pending | Auth, wallet, quiz completion idempotency, leaderboard, disabled endpoint `403 FeatureDisabled`. |
| pending | alpha-live-staging | Flutter live backend smoke | pending | `flutter test test/integration/live_backend_smoke_test.dart` against migrated staging. |
| pending | operator-cutover | Staging parallel-run runbook | pending | Runbook rows need result/evidence entries before cutover gate can pass. |
| pending | alpha-release-ops | Rollback drill and four-role sign-off | pending | Required before Alpha launch. |
| pending | alpha-release-ops | `release-gate.yml` on release SHA | pending | Required workflow artifact not attached yet. |
