# Changelog

All notable changes to this project.

---

## [2026-06-03] Synaptix.Setup Bug Fixes, Config Key Normalization, and Security Layer (Phase 1)

### Bug Fixes

- **`docker/compose.yml` — `unknown flag: --profile`** — `--profile dev` is a global `docker compose` flag; it must precede the subcommand. Fixed `bootstrap-local.ps1` to split compose args into `$globalArgs` (which includes `--profile dev`) and `$services`, then call `docker compose @globalArgs up -d @services`.
- **`docker/Dockerfile.setup` missing** — the `setup` service in `compose.yml` referenced a Dockerfile that didn't exist. Created `docker/Dockerfile.setup` modelled on `Dockerfile.migrate`; copies `Synaptix.MigrationService/seeds/` into `/app/seeds` so the container can upload bundled seeds to MinIO without a volume mount. Default `ENTRYPOINT` runs `provision-services`.
- **`provision-services` — all tasks failing with "connection string could not be constructed"** — `.NET`'s `AddEnvironmentVariables()` normalizes `__` to `:` when loading env vars (e.g. `MinIO__Endpoint` → `cfg["MinIO:Endpoint"]`), but `DotEnvConfigurationExtensions` was storing keys verbatim with `__`. Service tasks then used `__`-keyed lookups that worked locally but not in Docker containers where env vars were set via `compose.yml` using `MinIO__Endpoint` notation. Fixed in two layers:
  1. `DotEnvConfigurationSource.cs` — added `key.Replace("__", ":")` normalization so `.env` file keys match Docker env var behaviour.
  2. All six service tasks updated to use `:` separator: `cfg["MinIO:Endpoint"]`, `cfg["RabbitMQ:Password"]`, `cfg["ConnectionStrings:db"]`/`cfg.GetConnectionString("db")`, `cfg["Elastic:Url"]`, `cfg["SuperAdmin:Email"]`, `cfg["Setup:SeedsSourcePath"]`, etc.
- **Duplicate validation warnings** — `SecretValidationService` was emitting `Log.Warning` for each weak secret, then `ValidateCommand` emitted the same list again. Removed the inner log loop from `SecretValidationService`; `ValidateCommand` is now the sole emitter.

### Security Layer — Phase 1 (per `synaptix_setup_security_kms_reuse_recommendation.md`)

New `Synaptix.Setup/Security/` namespace implementing the Phase 1 abstractions from the KMS reuse recommendation:

| File | Purpose |
|------|---------|
| `SetupSecretOptions.cs` | Binding class for `SetupSecrets:ProtectionMode` — values: `PlaintextLocal` (default), `KmsPreferred`, `KmsRequired`, `ExternalOnly` |
| `ISetupSecretProtector.cs` | Interface: `ProtectAsync(name, plaintext)` / `UnprotectAsync(secret)` |
| `ProtectedSetupSecret.cs` | Record: name, ciphertext, provider, key name, key version, timestamp |
| `SetupSecretManifest.cs` | Serializable manifest written to `.local/bootstrap/bootstrap.secrets.enc.json` |
| `PlaintextLocalSetupSecretProtector.cs` | Phase 1 no-op implementation (Provider="Plaintext"); replace with `KmsSetupSecretProtector` in Phase 2 |
| `SetupSecretValidator.cs` | Wraps `SecretValidationService` + adds KMS availability check when `ProtectionMode` is `KmsRequired`/`KmsPreferred` |

- **`InitLocalCommand`** — now writes `.local/bootstrap/bootstrap.secrets.enc.json` after generating secrets, using the active `ISetupSecretProtector`.
- **`ValidateCommand`** — now reads `SetupSecrets:ProtectionMode` from config, constructs `SetupSecretValidator` with KMS options, and reports the active protection mode in the log.
- **`.gitignore`** — `bootstrap.secrets.enc.json` added (contains plaintext secrets in Phase 1).

### Responsibility Boundary (implemented)

```
Synaptix.Security.Kms  — encryption, key wrapping, Vault Transit, KMS API/client (unchanged)
Synaptix.Setup         — bootstrap workflow, env generation, service provisioning, seed validation
Synaptix.Setup/Security — bridges setup secrets to KMS; owns protection mode selection
```

KMS client integration (Phase 2 — `KmsSetupSecretProtector` backed by `Synaptix.Security.Kms.Client`) and secret rotation commands (Phase 3) are deferred to post-Alpha.

---

## [2026-06-03] Question Seeder Expansion, MinIO Asset Pipeline, and Bootstrap Setup System

### Seeder — Question Dataset Expansion

- **`MinioSeedOptions.QuestionDatasetKeys`** — new `List<string>` property on `MinioSeedOptions`; when non-empty it replaces `QuestionsKey` and all listed files are loaded concurrently and deduplicated on question text before seeding.
- **`MinioSeeder.ReadAllQuestionsAsync`** — new helper that merges multiple question JSON files into a single deduplicated list.
- **175 new seed questions** across 7 category JSON files in `Synaptix.MigrationService/seeds/questions/`: `general.json` (30), `science.json` (25), `history.json` (25), `geography.json` (25), `technology.json` (25), `sports.json` (25), `entertainment.json` (25). Difficulties span 1–4 (Easy–Expert).
- **`appsettings.json` updated** — `MinIO:Seeds:QuestionDatasetKeys` wired to all 7 category files; `MinIO:Seeds:AssetCatalogKey` added.

### Seeder — MinIO Asset Bootstrapping

- **`MinioSeeder.UploadBundledImagesAsync`** — scans `seeds/images/` and uploads `.png/.webp/.jpg` files to `questions/images/` in MinIO on each migration run (idempotent — skips existing keys).
- **`MinioSeeder.UploadBundledModelsAsync`** — scans `seeds/models/` and uploads `.glb/.gltf/.fbx` files to `models/` in MinIO (idempotent).
- **`AssetCatalogSeedModel`** — new seed model in `Synaptix.MigrationService/Seeding/SeedModels/` for non-purchasable 3D asset entries (id, type, name, key, thumbnailKey, version, sha256).
- **`seeds/asset-catalog.json`** — 8 placeholder entries (2 environments, 2 avatars, 2 gear, 2 effects) shipped as bundled seed.
- **`MinioSeeder.SeedAssetCatalogAsync`** — writes `frontend/assets/manifest.json` to MinIO after each seed run so the frontend can always fetch the latest asset list.

### API — Asset Manifest Endpoint

- **`GET /v1/assets/manifest`** — new public endpoint in `Synaptix.Backend.Api/Features/Assets/AssetManifestEndpoints.cs`. Reads `frontend/assets/manifest.json` from MinIO, generates 10-minute presigned download URLs for each asset's `.glb` and thumbnail, returns `AssetManifestResponse`. Falls back to `GetPublicUrl` when `IPresignedStorage` is unavailable (local storage mode).
- **`AssetDtos.cs`** — new file in `Synaptix.Shared.Contracts/Dtos/` containing `AssetManifestEntry` (record), `AssetManifestResponse`, and `AssetManifestItemDto`.

### API — Question Media URLs

- **`GameplayQuestionDto.MediaUrl`** — new nullable `string?` field added to `GameplayQuestionDto` in `QuestionDtos.cs`. `QuestionsEndpoints` injects `IObjectStorage` and resolves `q.MediaKey → storage.GetPublicUrl(q.MediaKey)` in `GET /questions/set` and `POST /questions/preview-set` responses. Clients no longer need to construct image URLs manually.

### Synaptix.Setup — New Bootstrap CLI Project

New standalone CLI tool `Synaptix.Setup` (added to solution and `.slnx`):

| Command | Description |
|---------|-------------|
| `init-local` | Reads `docker/.env.example`, generates strong random secrets for all placeholder fields, writes `docker/.env`, saves super admin credentials to `.local/bootstrap/super-admin.local.txt` |
| `validate` | Checks `docker/.env` for placeholder values, known weak defaults, and unsafe flags (`MIGRATION_RESET_DATABASE=true` outside local) |
| `provision-services` | Runs all service setup tasks in sequence + uploads bundled seeds to MinIO |
| `provision-minio` | Creates MinIO bucket if it doesn't exist |
| `upload-seeds` | Uploads all JSON files from `Synaptix.MigrationService/seeds/` to `seeds/` prefix in MinIO |
| `validate-seeds` | Checks that all required MinIO seed keys are present |
| `create-super-admin` | Creates super admin user + ACL entry directly via Npgsql |
| `rotate-super-admin-password` | Updates super admin password hash in PostgreSQL |
| `status` | Reads `.local/bootstrap/bootstrap-status.json` and prints a status report |

Service setup tasks implemented: `PostgresSetupTask`, `MongoSetupTask`, `RedisSetupTask`, `RabbitMqSetupTask` (uses management HTTP API), `MinioSetupTask`, `ElasticsearchSetupTask`, `SuperAdminSetupTask`.

Secret generation uses `System.Security.Cryptography.RandomNumberGenerator` — no external dependency.

### Docker and Infrastructure Hardening

- **`docker/.env.example`** — all 14 secrets replaced with `<generated-by-synaptix-setup>` placeholders. `MIGRATION_RESET_DATABASE` and `MIGRATION_ALLOW_ENSURE_CREATED` now default to `false`.
- **`docker/compose.yml`** — 12 secrets now use `:?required_message` syntax (fail loudly instead of silently using weak defaults): `POSTGRES_PASSWORD`, `MONGO_INITDB_ROOT_PASSWORD`, `MONGO_APP_PASSWORD`, `REDIS_PASSWORD`, `ELASTIC_PASSWORD`, `RABBITMQ_PASSWORD`, `MINIO_ROOT_PASSWORD`, `JWT_SECRET_KEY`, `GRAFANA_PASSWORD`, `PGADMIN_DEFAULT_PASSWORD`, `MONGO_EXPRESS_PASSWORD`, and backend connection string passwords. New `setup` service runs `provision-services` before `migration`; `migration` now `depends_on: setup: condition: service_completed_successfully`.
- **`.gitignore`** — added `.local/`, `docker/.env`, `docker/.env.local`, `docker/.env.local.generated`, `config/bootstrap/bootstrap.local.json`, `*.local.secret`, `*.generated.secret`.

### Bootstrap Scripts

- **`scripts/bootstrap-local.ps1`** — new PowerShell 5.1+ script for full local bootstrap: generates secrets → validates → starts infra → provisions services → runs migrations → starts API/dashboard. Supports `-SkipInfra`, `-SkipMigration`, `-Force`, `-DevTools` flags.
- **`scripts/bootstrap-local.sh`** — equivalent Bash script with `--skip-infra`, `--skip-migration`, `--force`, `--dev-tools` flags.

### Config Manifests

- **`config/bootstrap/bootstrap.example.json`** — declarative bootstrap manifest describing all service credentials, databases, queues, and seed configuration. `bootstrap.local.json` (gitignored) is the generated per-developer copy.
- **`config/seeds/seed-manifest.json`** — lists all relational, object-storage, Redis, and MongoDB seed operations with `required`, `mode` (upsert/insert-missing/ensure), and source path metadata.

### Bug Fixes (same PR, follow-up)

- **`docker/compose.yml` line 85** — `ELASTIC_PASSWORD` `:?` entry now double-quoted to prevent YAML parsing the colon-space in the error message as a map key-value separator (`unexpected type map[string]interface {}`).
- **`scripts/bootstrap-local.ps1`** — Unicode box-drawing characters (╔ ╝ ║ ═) and symbols (✓ ✗ ⚠) replaced with ASCII equivalents (`+`, `|`, `[OK]`, `[FAIL]`, `[!!]`). PowerShell 5.x on Windows interprets UTF-8 without BOM using the system ANSI code page, causing multi-byte sequences to be misread as string terminators.

---

## [2026-05-26] MediatR → Mediator (martinsson) Migration

### Completed

- **Replaced MediatR 14.x (Lucky Penny licensed) with Mediator 3.0.2 (martinsson, MIT)** — eliminates the production license warning and removes the commercial dependency entirely.
- **`Mediator.SourceGenerator 3.0.2` added to `Synaptix.Backend.Api.csproj`** — source generator is installed in the outermost project so it discovers all handlers across Application, Domain, Infrastructure, and Api at compile time; no reflection-based assembly scanning at runtime.
- **`MediatR` package removed from `Directory.Packages.props`** and from all four referencing projects (`Application`, `Infrastructure`, `Domain`, `MigrationService`); each now references `Mediator.Abstractions` instead.
- **`AddMediatR()` replaced with `AddMediator()`** — call moved to `Program.cs` in `Synaptix.Backend.Api`; `DependencyInjection.cs` in Application no longer references MediatR.
- **104 C# files updated** — `using MediatR` → `using Mediator`; all `IRequestHandler<,>` implementations updated from `Task<T>` to `ValueTask<T>`; all `INotificationHandler<>` implementations updated from `Task` to `ValueTask`; `Task.CompletedTask` / `Task.FromResult` in handler bodies updated to their `ValueTask` equivalents; test mock `IMediator` implementations updated to match the new interface contract.

---

## [2026-05-26] Merge, P0 Prep, and Release Doc Update

### Completed

- **Merged main (PRs #383–#385, 14 commits)** — Django full-user-flow tests (58), Reward Reactor endpoints, player lookup, Mongo admin, crypto auth, account rewards routes, user system roles, admin dashboard features, MongoDB diagnostics, and installer tooling all integrated into branch.
- **Designer.cs gap fixed** — `20260512150000_AddQuestionStatusColumns.Designer.cs` created; all 24 migrations now have corresponding snapshot files.
- **`appsettings.Production.example.json` added** — Production deploy checklist template covering all required config keys (JWT, DB, Redis, MinIO, Elastic, Observability, KMS, Vault, Hangfire). No secrets committed.
- **`store_purchases_enabled` feature flag added** — Payment endpoints (`POST /store/purchase`, `/store/payments/*`, `/store/subscription/*`, `/store/iap/validate`) now return `403 FeatureDisabled` when the flag is off (default false for Alpha), replacing the previous `503 PAYMENTS_DISABLED` on unconfigured providers.
- **Heartbeat files synced** — Alpha status, blockers, verification log, and current task updated to reflect complete Packet E rename, JWT contract update, and P0 prep work.
- **ALPHA_RELEASE_CRITERIA.md updated** — Repo-verified items checked; 8 staging-dependent P0 blockers remain documented.
- **PROJECT_STATUS updated** — Packet E marked complete; BE Packet E progress updated from ~60% to 100%.

### Still External (P0 blockers — require staging infrastructure)

- Apply EF migrations to staging PostgreSQL
- Capture staging `GET /health/ready` with all dependencies healthy
- Run `scripts/alpha-p0-smoke.sh` against live staging
- Flutter `test/integration/live_backend_smoke_test.dart` against staging
- Execute `.github/workflows/release-gate.yml` on release SHA
- Complete rollback drill (3 levels) on non-production
- Operator parallel-run runbook (`docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`)
- Four-role sign-off (Backend Lead, QA Lead, On-Call, Product Owner)

---

## [2026-05-25] MongoDB Diagnostics and Installer Tooling

### Completed

- **MongoDB diagnostics expanded** — Admin MongoDB health endpoint now includes collection-level stats, index info, and slow-query diagnostics details.
- **Backend installer tooling added** — `scripts/install-backend-assets.ps1` and `docs/infrastructure/backend-installer.md` added to streamline local/staging environment bootstrap.
- **MongoDB usage audit documented** — `docs/infrastructure/mongodb-usage-audit.md` captures analytics and crypto collection schemas.

---

## [2026-05-24] Player Lookup, Mongo Admin, and Crypto Auth

### Completed

- **Player lookup codes** — Admin player lookup by invite/referral code added; SQL migration `docs/migrations/player_lookup_codes_2026-05-25.sql` provided.
- **Mongo admin endpoints** — Admin MongoDB collection browser and diagnostics wired into operator dashboard.
- **Crypto service auth** — Crypto service JWT auth integrated; service-to-service token validation added.

---

## [2026-05-23] Account Rewards Routes and Players/Me Fix

### Completed

- **Account rewards routes added** — `GET /account/rewards/status` and `POST /account/rewards/claim` added to complement Reward Reactor flow.
- **`GET /players/me` fixed** — 404 error resolved; endpoint now correctly resolves authenticated player profile.

---

## [2026-05-22] Reward Reactor Feature Endpoints

### Completed

- **Reward Reactor implemented** — `POST /arcade/reactor/spin` and `POST /arcade/reactor/claim` added; backend-authoritative RNG replaces client-provided segment IDs.
- **Study Hub and Crypto docs finalised** — Reward Reactor, Study Hub, and Crypto endpoint handoff docs updated to reflect final API contracts.
- **User system role added** — `SystemRole` field added to user entity for platform-level permissions distinct from RBAC.
- **Admin dashboard features** — Additional operator dashboard admin features added for user management and system role assignment.

---

## [2026-05-22] Packet E — Full Synaptix Rename Complete

### Completed

- **C# project and namespace rename** — All 20 C# projects renamed from `Tycoon.*` to `Synaptix.*`; 987 namespace declarations and 1,491 using statements updated across 1,001 files.
- **Non-C# service rename** — `Tycoon.CryptoService`, `Tycoon.OperatorDashboard.Django`, `Tycoon.OperatorDashboard.Vue`, `Tycoon.Sidecar` renamed to `Synaptix.*` equivalents; Dockerfiles, CI, and docs updated.
- **JWT issuer/audience updated** — `TycoonBackendApi` → `SynaptixBackendApi`, `TycoonFrontendApp` → `SynaptixFrontendApp` across all appsettings and `JwtSettings.cs`.
- **Frontend handoff doc created** — `docs/handoffs/FRONTEND_REBRAND_HANDOFF.md` guides Flutter team on JWT claim update, re-auth coordination, and what to search in their codebase.
- **Django full-user-flow tests** — 58 new tests covering complete user lifecycle in Django operator dashboard (PR #383).

---

## [2026-05-22] Packet E — Elasticsearch, Docker, CI, Telemetry Rename

### Completed

- **Elasticsearch aliases renamed** — `tycoon-qa-*` → `synaptix-*` in `ElasticOptions.cs`, `ElasticAdmin.cs`, and all appsettings environments.
- **Docker compose renamed** — Project name, 20+ container names (`tycoon_*` → `synaptix_*`), network (`tycoon-net` → `synaptix-net`), all credential/bucket/database defaults renamed.
- **Telemetry ServiceName updated** — `Tycoon.Backend.Api` → `Synaptix.Backend.Api`, `Tycoon.MigrationService` → `Synaptix.MigrationService`.
- **Scripts, CI, monitoring updated** — `setup-dev.sh`, `setup-dev.ps1`, `compose-smoke.sh`, `prometheus.yml`, `.github/workflows/compose-smoke.yml` all updated.
- **`docs/PROJECT_STATUS_2026-05-09.md` created** — Full project completion snapshot.

---

## [2026-05-21] Secure Channel Replay And AAD Hardening

### Completed

- **Replay metadata enforced** - Secure-channel protected endpoints now require `X-Syn-Sec-Seq` and `X-Syn-Sec-Nonce` before decrypting request bodies.
- **KMS replay protection wired** - Payload decrypt now rejects reused sequence/replay nonce pairs, stale timestamps, and authenticated subject mismatches.
- **AES-GCM AAD binding added** - The backend derives request-context AAD and passes it through KMS decrypt/encrypt so encrypted payloads are bound to method, path, session, sequence, subject, and timestamp context.
- **Tests expanded** - KMS payload tests cover AAD, replay, timestamp, and subject failure paths; backend secure-channel tests cover header enforcement and KMS metadata pass-through.

### Still External

- Flutter secure-channel integration still needs to send the new sequence/replay headers and encrypt with the same AAD contract.

---

## [2026-05-18] Operator Dashboard Detail Drilldowns

### Completed

- **Django detail pages added** - Users, questions, moderation players/logs, and security audit events now have first-class operator drilldowns from their list pages.
- **Narrow backend detail APIs added** - Moderation logs and security audit events now expose single-record admin detail endpoints for Django review pages.
- **Cutover runbook updated** - Staging parallel-run checks now include the new detail/edit drilldowns as repo-side hardening validation.

### Still External

- These pages do not close May cutover gates by themselves; staging/prod evidence, sign-off, route cutover, and Blazor rollback-window completion remain external gates.

---

## [2026-05-18] Operator May Cutover Gate Ledger

### Completed

- **Final gate ledger documented** - May cutover docs now map each readiness gate to the exact evidence required before it can move from `pending` to `pass`.
- **Evidence boundaries tightened** - release artifacts, parity checklist, parallel-run evidence pack, and staging runbook now state that local compose evidence does not close staging/prod gates.
- **Blazor rollback-window status clarified** - `blazorRollbackWindow` remains open through 2026-06-12 unless leadership records an approved policy exception.

### Still External

- Staging/prod migration evidence, strict readiness logs, staging parallel-run execution, QA/backend/on-call sign-off, production route cutover evidence, post-cutover smoke results, and rollback-window completion remain pending until live evidence is attached.

---

## [2026-05-18] Alpha/Beta Release Evidence Alignment

### Completed

- **Repo-side Alpha/Beta gates re-verified** — Release build passed with 0 errors; API tests passed 417/418 with 1 documented skip; Application tests passed 198/198; EF schema drift validation reported no pending model changes.
- **Migration artifact generation corrected** — local `dotnet ef migrations script --idempotent` now uses `Tycoon.MigrationService` as the startup project, matching the deployed migration runner and successfully generating `artifacts/migrations/idempotent.sql`.
- **MigrationService compose blocker fixed** — `MigrationWorker` now clears tracked seed entities before daily/weekly mission reset, preventing stale question option updates from causing EF concurrency failures during compose migration.
- **Compose smoke hardened and verified** — `scripts/compose-smoke.sh` no longer passes `GET` as an extra curl URL for bodyless requests; full compose smoke now passes locally.
- **CI/helper drift fixed** — `dotnet-ci.yml`, `scripts/migrate.sh`, and `scripts/alpha-now-complete.sh` now use `Tycoon.MigrationService` for EF startup instead of `Tycoon.Backend.Api`.
- **Release docs refreshed** — Alpha/Beta release plan, migration implementation plan, release criteria, and known issues now separate repo-proven evidence from staging-only gates.

### Still External

- Staging migration application, `release-gate.yml` execution against staging, Flutter live backend smoke test, rollback drill, and four-role Alpha sign-off remain open until live environment evidence is attached.

---

## [2026-05-14] Operator Dashboard May Cutover Evidence Readiness

### Completed

- **CI/readiness automation prepared** — backend CI now targets `TycoonTycoon_Backend.slnx` with the SDK from `global.json`, keeps Django checks in CI, provisions Redis for API-hosted test jobs, and adds optional separate-repo `trivia_tycoon` Flutter CI.
- **Read-only cutover evidence workflow** — added `operator-cutover-readiness` automation that probes backend/dashboard health, admin login/profile/dashboard readiness, and supplied release-gate statuses, then emits JSON and Markdown artifacts without flipping traffic.
- **Evidence package prepared** — May cutover guide, parity checklist, release artifacts, staging runbook, evidence pack, and risk assessment now distinguish repo-complete preparation from external staging/prod gates.
- **Repo verification baseline recorded** — Django checks/tests, compose config, workflow YAML parse, readiness script compile, diff check, and .NET restore/build are captured as repo-side evidence.

### Still External

- Staging/prod EF migration application, strict migration/seed readiness runs, staging parallel-run, QA/backend/on-call sign-off, production route cutover, and Blazor rollback-window completion remain open until live evidence is attached.

---

## [2026-05-03] Synaptix Rebrand — Backend Completion (Packets A–D)

### Domain

- **`Tier.UpdateDefinition()`** — new method on `Tycoon.Backend.Domain.Entities.Tier` allowing name and score-band updates without replacing the entity; required for seeder upsert and future admin tooling.

### Seeder / MigrationService

- **Tier names** (`Tycoon.MigrationService/Seeding/AppSeeder.cs`) — replaced Bronze/Silver/Gold/Platinum/Diamond with the six Synaptix ladder tiers: **Neural Initiate → Synapse Adept → Cortex Strategist → Mind Architect → Neural Overlord → Synaptix Prime**. Seeder now upserts by `Order` so existing databases are renamed on next migration run, not just fresh installs.
- **Mission titles and descriptions** — all 10 seed missions previously had the placeholder title "Daily win" and empty descriptions. Each mission now carries a distinct Synaptix-flavoured title and flavour-text description aligned with the neural/cognitive brand voice (e.g. "First Signal", "Synapse Chain", "Arena Ascendant", "Flawless Circuit"). Seeder upserts by `Key` — existing missions with the old placeholder titles are not duplicated; new keys are inserted.

### Application

- **`LeaderboardRecalculator.SeedDefaultTiersAsync`** — fallback tier names updated to match the Synaptix ladder (was Bronze…Titan; now Neural Initiate…Synaptix Prime). Overflow tiers beyond the six named ones still generate as `"Tier N"`.

### Configuration

- **`JwtSettings:Issuer`** — `TycoonBackendApi` → `SynaptixApi` in both `appsettings.json` and `appsettings.Development.json`.
- **`JwtSettings:Audience`** — `TycoonFrontendApp` → `SynaptixApp` in both files.
- **`Authentication:Schemes:Bearer:ValidAudiences`** — `TycoonClient` → `SynaptixApp` in `appsettings.Development.json`.
- **`PayPal:BrandName`** — `"Tycoon"` → `"Synaptix"` in both files.
- **`StorePremium:AdFree:Subtitle`** — "uninterrupted Tycoon experience" → "uninterrupted Synaptix experience" in both files.

### Deferred (Packet E — post soft-launch)

- Backend namespace rename `Tycoon.*` → `Synaptix.*`
- Elasticsearch daily-rollup alias rename (`tycoon-qa-*`)
- `Observability:ServiceName` rename
- `Iap:GooglePackageName` app-id change (`com.tycoon.app.*`)

---

## [2026-05-02] Synaptix Security KMS — Full Implementation

### New projects

- **`Synaptix.Security.Kms.Contracts`** — shared model class library (`EncryptedPayload`, `SecureSession`, `WrappedDataKey`, `SecureSuites` constants `ClassicalV1` / `HybridPqV1`). Converted from console exe to class library; `Program.cs` removed.
- **`Synaptix.Security.Kms.Application`** — domain services class library.
  - `SecureSessionService` — X25519 ECDH key exchange, HKDF-SHA256 key derivation (context labels `synaptix:c2s:v1` / `synaptix:s2c:v1`), `CryptographicOperations.ZeroMemory` for forward secrecy.
  - `SecurePayloadService` — AES-256-GCM encryption (96-bit nonce, 128-bit auth tag) with HMAC-SHA256 server signature.
  - `KeyRotationService` — triggers Vault Transit key rotation via `IKeyWrappingService`.
- **`Synaptix.Security.Kms.Infrastructure`** — infrastructure adapters class library.
  - `VaultTransitClient` — direct HTTP REST client to HashiCorp Vault Transit API (no VaultSharp dependency); `GenerateDataKeyAsync`, `DecryptDataKeyAsync`, `GetLatestKeyVersionAsync`.
  - `VaultEnvelopeEncryptionService` — envelope encryption: Vault wraps/unwraps data keys, local AES-GCM encrypts data.
  - `NullKeyWrappingService` — dev fallback (random bytes, no Vault required).
  - `RedisSessionStore` — `IDistributedCache`-backed session persistence with AOT-safe `System.Text.Json` source generation.
  - `RedisReplayProtectionStore` — Redis keys `synsec:session:{id}:seq:{n}` and `synsec:session:{id}:nonce:{n}` for replay/sequence protection.
  - `InMemoryReplayProtectionStore` — dev fallback when Redis unavailable.
  - `DependencyInjection.AddKmsInfrastructure()` — conditionally registers Vault services (only when `Vault` config section present); falls back to in-memory cache when `ConnectionStrings:cache` is empty.
- **`Tycoon.Security.Kms.Client`** — typed HTTP client library for consuming the KMS API.
  - Interfaces: `IKmsSessionClient`, `IKmsPayloadClient`, `IKmsKeyClient`, `IKmsInternalClient`.
  - Implementations in `Http/` — all use `PostAsJsonAsync` + `ReadFromJsonAsync`; throw `KmsClientException` / `KmsUnavailableException` on failure.
  - `KmsClientOptions` bound from `appsettings.json` section `"KmsClient"`.
  - `ServiceCollectionExtensions.AddKmsClient()` — one-call DI registration with `AddStandardResilienceHandler()` (retry + circuit-breaker).

### Updated projects

- **`Synaptix.Security.Kms.Api`** — converted from `Microsoft.NET.Sdk` console to `Microsoft.NET.Sdk.Web`.
  - JWT bearer authentication supporting OIDC authority, symmetric secret, or dev-only passthrough.
  - Minimal API endpoints: `SessionEndpoints` (`/security/sessions/start|renew|revoke`), `PayloadEndpoints` (`/security/payload/encrypt|decrypt`), `KeyEndpoints` (`/security/keys/rotate`), `InternalEndpoints` (`/internal/security/datakey|encrypt|decrypt`).
  - `ServiceTokenFilter` — `IEndpointFilter` enforcing `X-Service-Token` header on key and internal routes.
  - `/health` and `/alive` mapped unconditionally (not gated behind `IsDevelopment`).
  - `appsettings.json` and `appsettings.Development.json` with full config skeleton (Vault, JWT, Redis, KmsApi sections).
- **`Directory.Packages.props`** — added `Microsoft.Extensions.Caching.Memory` 10.0.7.
- **`TycoonTycoon_Backend.slnx`** — added `Synaptix.Security.Kms.Contracts` and `Tycoon.Security.Kms.Client` to the `/Services/Synaptix.Security.Kms/` solution folder.

### Docker / infrastructure

- **`docker/compose.security.yml`** (new) — standalone security stack: HashiCorp Vault (dev mode), `vault-init` one-shot Transit key provisioner (4 keys: `synaptix-session-wrap`, `synaptix-payload-wrap`, `synaptix-refresh-token-wrap`, `synaptix-data-protection-wrap`), `kms-api` service.
  - Ports: Vault `8210:8200`, KMS API `5060:5050`.
  - `VAULT_DISABLE_MLOCK=true` — allows Vault dev mode to start without `CAP_SETFCAP` capability.
- **`Synaptix.Security.Kms.Api/Dockerfile`** — `curl` installed in the runtime stage (required for Docker Compose healthcheck; absent from `dotnet/aspnet:10.0` slim image).
- **`Synaptix.Security.Kms.Api/Properties/launchSettings.json`** — `applicationUrl: http://localhost:5050` added; `ASPNETCORE_ENVIRONMENT: Development` set on both profiles.
- **`Synaptix.Security.Kms.Api/appsettings.Development.json`** — Redis connection string cleared (empty string); in-memory cache fallback used when running without Redis.

### Docs

- `docs/Synaptix_Backend_Security_KMS_Handoff.md` — updated with implementation status.
- `docs/Tycoon_Security_Kms_Client_Implementation_Plan.md` — marked as completed.
- `Docker.md` — Security stack section added.
- `docs/SYNAPTIX_SECURITY_RUNNING_GUIDE.md` (new) — full local and Docker run instructions.

---

## [2026-05-01] Personalization Alignment Audit — Gap Fixes

### Completed this session

- **Reason / Explainability (audit gap #1)** — Added `Reason` field to `PersonalizationRecommendation` domain model and `PersonalizationRecommendationConfiguration` EF config; added `Reason` to `PlayerRecommendationDto` (frontend now receives explainability text); `PersonalizationService` populates it from the sidecar candidate's `Reason`; migration `20260501090000_AddReasonToPersonalizationRecommendation` adds the column.
- **Recommendation persistence fix (audit gaps #2 & #8)** — `_db.PersonalizationRecommendations.Add()` moved inside the guardrail-allowed branch. Blocked candidates now write to the audit log only; the `personalization_recommendations` table contains only approved recommendations. Audit always runs unconditionally before the branch.
- **Ownership validation (audit gap #6)** — All 7 `PersonalizationEndpoints` routes and both `CoachEndpoints` routes now enforce an `IsOwner()` check that compares the JWT `NameIdentifier`/`sub` claim to the route `playerId`, returning `403 Forbidden` on mismatch. Prevents cross-player data access.
- **Config-driven sidecar timeout (audit gap #4)** — Replaced hardcoded `TimeSpan.FromSeconds(5)` with `configuration.GetValue<int>("SidecarPersonalization:TimeoutSeconds", 5)` in `Program.cs`.

### Skipped / N/A

- Gap #3 (Sidecar FastAPI routes) — routes `POST /personalization/score-player` and `POST /personalization/recommendation-candidates` already exist in `Synaptix.Sidecar/app/routers/personalization.py`.
- Gap #5 (DB migrations) — migration files exist; `dotnet ef database update` must be run against staging/production.
- Gap #7 (`.WithOpenApi()`) — intentionally removed in the .NET 10 upgrade session; deprecated (`ASPDEPR002`) in ASP.NET Core 10.

---

## [2026-05-01] Personalization Hardening — Audit Trail, Feature Flags, Configurable Guardrails

### Completed this session

- **`PersonalizationAuditLog` domain model** — New entity in `Tycoon.Backend.Domain/Personalization/PersonalizationAuditLog.cs` capturing `DecisionType`, `Source`, `Reason`, and four JSONB fields: `InputSignalsJson`, `CandidateJson`, `GuardrailsAppliedJson`, `FinalDecisionJson`.
- **`PersonalizationAuditLogConfiguration`** — EF config with `jsonb` columns, `(player_id, created_at)` composite index, `decision_type` index, `recommendation_id` index.
- **`PersonalizationAuditLogs` DbSet** — Added to `AppDb` and `IAppDb`.
- **`PersonalizationOptions`** — New feature-flag class (`Enabled`, `UseSidecar`, `AdaptiveQuestions`, `AdaptiveMissions`, `AdaptiveStore`, `AdaptiveNotifications`, `CoachEnabled`) with configurable thresholds (`FrustrationPaidOfferSuppressionThreshold`, `NotificationFatigueThreshold`).
- **`IPersonalizationAuditService` + `PersonalizationAuditService`** — Writes one structured audit record per recommendation decision (allowed or blocked) with full input/output snapshot.
- **`PersonalizationGuardrailService` updated** — Now takes `IOptions<PersonalizationOptions>`; suppression thresholds are runtime-configurable rather than hardcoded.
- **`PersonalizationService` updated** — Injects `IPersonalizationAuditService`; calls `LogDecisionAsync` for every guardrail evaluation.
- **Admin debug endpoint** — `GET /admin/personalization/debug/{playerId}` returns profile + last-25 behavior events + last-25 audit entries.
- **DI registration** — `IPersonalizationAuditService` registered in `Application/DependencyInjection.cs`; `PersonalizationOptions` configured via `Configure<PersonalizationOptions>` in `Program.cs`.
- **Migration `20260430120000_AddPersonalizationAuditLog`** — Creates `personalization_audit_logs` table.
- **`appsettings.Development.json`** — `Personalization` and `SidecarPersonalization` sections added.

---

## [2026-05-01] .NET 10 Upgrade + Docker Fixes

### Completed this session

- **Runtime upgrade** — All 20 projects migrated from `net9.0` → `net10.0`; `global.json` SDK pinned to `10.0.100`.
- **Package upgrades** — `Directory.Packages.props` bumped: EF Core `10.0.7`, MassTransit `9.1.0`, Elastic.Clients.Elasticsearch `9.3.6`, MinIO `7.0.0`, Stripe.net `51.1.0`, Asp.Versioning `10.0.0`, Swashbuckle.AspNetCore `10.1.7`, Testcontainers `4.11.0`, WireMock.Net `2.4.0`, SonarAnalyzer `10.25.0`, HotChocolate `15.1.16`. FluentAssertions held at `7.0.0` (license review pending).
- **Microsoft.OpenApi 2.0 breaking changes** — All 10 files in `Tycoon.Shared/OpenApi/` updated: `Microsoft.OpenApi.Models` → `Microsoft.OpenApi` namespace; `OpenApiAnyFactory.CreateFromJson` → `JsonNode.Parse`; `new OpenApiString(...)` → `JsonValue.Create(...)`; `ISchemaFilter.Apply` parameter updated to `IOpenApiSchema`; `OpenApiSecurityRequirement` key type changed to `OpenApiSecuritySchemeReference`; `AddSecurityRequirement` wrapped in lambda.
- **Swashbuckle 10** — `IsDeprecated()` extension removed; inlined as `EndpointMetadata.OfType<ObsoleteAttribute>().Any()`. All 74 `.WithOpenApi()` call sites removed (ASPDEPR002 deprecated in ASP.NET Core 10).
- **Asp.Versioning 10** — `EnableApiVersionBinding()` removed.
- **MinIO 7** — `.Build()` removed from `AddMinio()` chain.
- **Stripe.net 51** — `Events` → `EventTypes`; `Subscription.CurrentPeriodEnd` moved to `SubscriptionItem.CurrentPeriodEnd`.
- **OpenTelemetry EF Core** — `SetDbStatementForText` removed; collapsed to no-argument `AddEntityFrameworkCoreInstrumentation()`.
- **Hangfire PostgreSql** — `UsePostgreSqlStorage(string, options)` obsolete overload replaced with `UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connStr), options)`.
- **Docker** — `Dockerfile.api`, `Dockerfile.dashboard`, `Dockerfile.migrate`, `Dockerfile.migration-service` updated from `mcr.microsoft.com/dotnet/sdk:9.0` / `aspnet:9.0` → `sdk:10.0` / `aspnet:10.0`.



### Completed this session

- **Admin Personalization Endpoints (Issue 13)** — 8 routes under `GET|POST /admin/personalization/*`:
  - `GET /admin/personalization/summary` — total profiles, archetype distribution, high churn/frustration counts
  - `GET /admin/personalization/archetypes` — grouped counts ordered by frequency
  - `GET /admin/personalization/recommendations/performance` — acceptance/dismissal rates per recommendation type
  - `GET /admin/personalization/player/{playerId}` — full profile snapshot
  - `POST /admin/personalization/player/{playerId}/recalculate` — trigger sidecar recalculation
  - `POST /admin/personalization/player/{playerId}/reset` — reset all scores to safe defaults
  - `GET /admin/personalization/rules` — list all guardrail rules
  - `PUT /admin/personalization/rules/{ruleKey}` — upsert a guardrail rule (enable/disable, update payload)
- **Question-answered hook (Issue 9)** — `QuestionAnsweredMissionJob.RunAsync` now records a `question_answered` behavior event (category, difficulty, mode, correct, answerTimeMs) into `PlayerBehaviorEvents` after every answer, enabling real-time ML scoring.
- **Learning-module hook (Issue 9)** — `CompleteModuleHandler.Handle` records a `learning_module_completed` event (category, difficulty, moduleId, title) on first successful completion.
- **Match-completed hook (Issue 10)** — `MissionProgressService.ApplyMatchCompletedAsync` records a `match_completed` event (isWin, correctAnswers, totalQuestions, durationSeconds) after every match.
- **Store guardrail + purchase hook (Issue 11)** — `GET /store/special-offers` suppresses offers when `frustrationRiskScore >= 0.75` (guardrail: `suppress_paid_offers_when_frustrated`); `POST /store/purchase` records a `store_item_purchased` event (sku, quantity, currency, totalPrice) on successful purchase.
- **Notification hooks (Issue 12)** — `PlayerInboxService.MarkReadAsync` records `notification_opened`; `DeleteAsync` records `notification_dismissed` — feeds notification fatigue score accumulation in the sidecar.
- All behavior event hooks are wrapped in `try/catch` — personalization failures never affect gameplay, purchases, or notifications.

### Architecture
- `IPlayerMindProfileService` injected as optional (`= null`) in all job/service constructors — zero risk of DI failure in existing tests.
- `IPlayerMindProfileService` injected as required parameter in `StoreEndpoints.Purchase` — always available since it's registered in `DependencyInjection.cs`.
- Guardrail check in `GetSpecialOffers` uses a direct scalar projection (`Select(p => (decimal?)p.FrustrationRiskScore)`) — one lightweight DB round-trip, no full profile hydration.

---

## [2026-04-29] Full Session Summary

### Completed this session

- **DefaultPermissions fix (.NET)** — `AdminAuthEndpoints.cs` now grants all 12 permission scopes (`users`, `questions`, `events`, `store`, `economy`, `anticheat`, `notifications`, `seasons`, `eventqueue` — read + write) to every operator on next login. Resolves 403s on all Wave B/C surfaces.
- **Django Wave B — Questions** — `questions_queue_view`, `questions_approve`, `questions_reject` views + URL patterns + `questions_queue.html` template already in place; service client `admin_questions_client.py` complete.
- **Django Wave B — Game Events** — `admin_game_events_client.py`, views (`game_events_view`, `game_event_open`, `game_event_start`, `game_event_close`), URL patterns, `game_events.html` template (status filter, lifecycle action buttons).
- **Django Wave C — Economy** — `admin_economy_client.py`, `economy_player_view` / `economy_grant` views, URL patterns, `economy_player.html` template already in place.
- **Django Wave C — Anti-Cheat** — `admin_anticheat_client.py`, views (`anticheat_flags_view`, `anticheat_flag_review`), URL patterns, `anticheat_flags.html` template (severity/playerId/unreviewedOnly filters, inline review form).
- **Django Wave C — Seasons** — `admin_seasons_client.py`, views (list, activate, close, recompute, leaderboard), URL patterns, `seasons.html` template.
- **Django Wave C — Notifications** — `admin_notifications_client.py`, views (list, send, dead-letter/replay), URL patterns, `notifications.html` template.
- **Django Wave C — Event Queue** — `admin_event_queue_client.py`, views (view, reprocess), URL patterns, `event_queue.html` template (intentional: upload endpoint not exposed — API-only card added).
- **Django base.html nav** — Events, Security, Notifications, Event Queue groups added to sidebar.
- **Vue/Web deprecated** — `DEPRECATED.md` added to both `Synaptix.OperatorDashboard.Vue` and `Tycoon.OperatorDashboard.Web`.
- **Avatar handler tests** — 18 xUnit tests in `Tycoon.Backend.Application.Tests/Avatars/AvatarHandlerTests.cs` covering `GetAvatarCatalog` (7), `PurchaseAvatar` (6), `GetAvatarAsset` (5). Correct seeding via `EconomyService.ApplyAsync` and `PlayerTransaction.MarkApplied()`.
- **Pending migrations SQL** — `docs/pending_migrations_2026-04-29.sql`: idempotent PostgreSQL DDL for all 6 pending EF migrations (`AddStoreItemAvatarFields`, `AddSeasonRewardRules`, `AddStoreStockSystem`, `AddFlashSale`, `AddRewardClaimRule`, `AddEffectiveMaxQuantity`). Wrapped in `BEGIN/COMMIT`. Safe to re-run.
- **Staging parallel-run runbook** — `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`: 14 surface checklists (Django vs Blazor), avatar API curl tests, pass/fail/rollback criteria, sign-off table.

### Remaining (operational — no code changes needed)

- Execute staging parallel-run (May 8–14) using `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` with real operator accounts.
- Apply `docs/pending_migrations_2026-04-29.sql` to staging and production databases.
- Obtain QA Lead + Backend Lead + On-call Operator sign-off before May 15 cutover.

---

## [2026-04-26] Full Session Summary

### Completed this session
- Store Stock System P2 — Admin Stock Management (12 new endpoints, 2 new entities, 2 migrations)
- Swagger duplicate middleware fix (`UseSwagger`/`UseSwaggerUI` called twice — removed bare pair)
- Full task audit across all markdown files — 27 backend items complete, 10 in progress, 18 not started
- `REMAINING_TASKS.md` summary table updated to reflect current state
- Frontend handoff doc created: `docs/frontend_handoff_2026-04-26.md`
  - Admin store P2 endpoint contracts
  - Crypto economy endpoints (all 10 — previously undocumented)
  - Wallet endpoint clarification (`GET /users/me/wallet`)
  - Full matrix of frontend-pending vs backend-pending items
  - Pending migrations action list for DevOps

### Verified already complete (no code change needed)
- Synaptix branding: Swagger title = "Synaptix API", dashboards = "Synaptix Command" ✅
- Auth `/auth/signup` vs `/auth/register` distinction documented for frontend ✅

### Remaining backend work identified
- Operator Dashboard migration Wave B (Questions, Events, Seasons) — not started
- Operator Dashboard migration Wave C (Moderation, Notifications, Economy, Anti-cheat) — not started
- `GET /v1/assets/audio/{category}/{filename}` — not started (P2 future)
- Game balance migration apply — requires `dotnet ef database update` in live environment
- gRPC integration tests — pending tooling environment

---

## [2026-04-26] Store Stock System P2 — Admin Stock Management

### New endpoints (`X-Admin-Ops-Key` required)

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/admin/store/stock-policies` | List all stock policies (`?activeOnly`, `?sku`) |
| `PUT` | `/admin/store/stock-policies/{sku}` | Upsert policy — creates or updates `maxQuantityPerUser`, `resetInterval` |
| `POST` | `/admin/store/stock-policies/bulk-reset` | Reset `QuantityUsed=0` for all players on given SKU list |
| `GET` | `/admin/store/player-stock/{playerId}` | View player's per-SKU stock states including override |
| `POST` | `/admin/store/player-stock/{playerId}/override` | Set `EffectiveMaxQuantity` for a player+SKU (null clears) |
| `GET` | `/admin/store/flash-sales` | List active + scheduled flash sales |
| `POST` | `/admin/store/flash-sales` | Create flash sale (overlap guard, SKU existence check) |
| `DELETE` | `/admin/store/flash-sales/{id}` | Soft-cancel a flash sale (`IsActive=false`) |
| `GET` | `/admin/store/reward-limits/{rewardId}` | Get reward claim rule |
| `PUT` | `/admin/store/reward-limits/{rewardId}` | Upsert reward claim rule (`maxClaimsPerInterval`, `resetInterval`) |
| `GET` | `/admin/store/analytics/purchases` | Aggregate purchase stats — `totalPurchases`, `totalCoinsSpent`, `topSkus` with optional date + SKU filters |
| `GET` | `/admin/store/analytics/stock-resets` | Paginated reset history from `PlayerStoreStockState.LastResetAtUtc` |

### New entities

- **`RewardClaimRule`** — per-reward claim-frequency cap (`RewardId`, `MaxClaimsPerInterval`, `ResetInterval`, `IsActive`)
- **`EffectiveMaxQuantity`** added to `PlayerStoreStockState` — admin-controlled per-player ceiling that overrides the policy default; `null` = use policy default

### New migrations

- `20260426100000_AddRewardClaimRule` — creates `reward_claim_rules` table with unique index on `reward_id`
- `20260426110000_AddEffectiveMaxQuantity` — adds nullable `effective_max_quantity` column to `player_store_stock_states`

### Domain changes

- `PlayerStoreStockState.SetOverride(int?)` — sets/clears `EffectiveMaxQuantity`
- `PlayerStoreStockState.BulkReset(policy, now)` — admin bulk reset, respects `ResetInterval == "none"`
- `PlayerStoreStockState.GetRemaining()` — now uses `EffectiveMaxQuantity` when set, falling back to policy default
- `StoreStockPolicy.Update(maxQty, interval, isActive?)` — mutable update method for upsert path

---

## [2026-04-25] Store Stock System P0 + P1

### P0 — Daily Store + Stock Enforcement

- Added `StoreStockPolicy` domain entity (per-SKU rules: `MaxQuantityPerUser`, `ResetInterval` = "daily"/"weekly"/"none").
- Added `PlayerStoreStockState` domain entity (per-player tracking with lazy reset on `ConsumeStockAsync`).
- Added EF configurations and migration `20260425130000_AddStoreStockSystem` creating `store_stock_policies` and `player_store_stock_states` tables.
- Added `IStoreStockService` / `StoreStockService` in `Tycoon.Backend.Application/Store`:
  - `CheckStockAsync` — read-only quota check; returns `"store_item_out_of_stock"` or null.
  - `ConsumeStockAsync` — lazy reset + increment; no-ops when no policy.
  - `GetDailyItemsAsync` — returns policy-backed items enriched with player stock state.
- Added `GET /store/daily` (auth required) — daily rotating store items with remaining qty, reset time, sold-out flag.
- Extended `POST /store/purchase` — stock check before transaction; consume on success; `409 store_item_out_of_stock` when quota exhausted.
- Added `DailyStoreItemDto` + `DailyStoreResponseDto` to `Tycoon.Shared.Contracts`.

### P1 — Player-Specific Catalog + Hub + Special Offers

- Added `FlashSale` domain entity (Sku, DiscountPercent, StartsAtUtc, EndsAtUtc, IsActive, Reason).
- Added `FlashSaleConfiguration` and migration `20260425140000_AddFlashSale` creating `flash_sales` table.
- Extended `IStoreStockService` with three new methods:
  - `GetCatalogForPlayerAsync` — full catalog resolved per-player with stock state, ownership, availability state (`available`/`sold_out`/`already_owned`), stock state (`in_stock`/`low_stock`/`out_of_stock`/`unlimited`), and flash-sale discounts. All auxiliary data (policies, states, sales, ownership) loaded concurrently via `Task.WhenAll`.
  - `GetHubAsync` — featured items + daily items + category list; reuses existing methods.
  - `GetSpecialOffersAsync` — active flash sales joined with catalog items; returns sale price, original price, discount, end time.
- Added three new endpoints:
  - `GET /store/catalog/{playerId:guid}` (auth required, self-only) — player-specific catalog with optional `?itemType=` and `?category=` filters.
  - `GET /store/hub` (auth required) — store hub surface.
  - `GET /store/special-offers` (auth required) — active flash sales.
- Added `PlayerStoreCatalogItemDto`, `PlayerStoreCatalogResponseDto`, `StoreHubResponseDto`, `SpecialOfferDto`, `SpecialOffersResponseDto` to `Tycoon.Shared.Contracts`.

### Documentation

- Added `docs/store_stock_frontend_handoff_2026-04-25.md` — full frontend integration guide for all P0 + P1 store stock endpoints.
- Updated `docs/REMAINING_TASKS.md` — P0 and P1 marked complete; P2 backlog unchanged.

---

## [2026-04-23] MinIO Catalog Seeders + Store/Admin Store Handoff

### MinIO-backed database seeders

- Added `MinioSeeder` to `Tycoon.MigrationService` — reads JSON seed files from MinIO at startup and upserts them idempotently into the database.
- Supports four seed files: `seeds/store-items.json`, `seeds/skill-nodes.json`, `seeds/season-rewards.json`, `seeds/questions.json`.
- Missing seed files are silently skipped — only upload the files you want to seed.
- All four entity seeders follow a bulk-fetch pattern (single `WHERE IN` query per type, no N+1 loops).
- MinIO reads for all four files are issued concurrently via `Task.WhenAll` before any DB writes.
- `GetAsync(string key, CancellationToken ct)` added to `IObjectStorage` and implemented in both `MinioObjectStorage` (with `MemoryStream` callback + precise `ObjectNotFoundException`/`NoSuchKey` error guard) and `LocalObjectStorage` (file-stream from `wwwroot`).
- `EnsureBucketExistsAsync` result is cached on the singleton via a `_bucketEnsured` flag to avoid repeated network round-trips.
- `MinioSeeder` is registered as `Transient` in `MigrationService/Program.cs` and called from `MigrationWorker` with a non-fatal try/catch (MinIO unavailability is logged as a warning, not a crash).

### SeasonRewardRule persistence

- Added `DbSet<SeasonRewardRule> SeasonRewardRules` to `IAppDb` and `AppDb`.
- Added `SeasonRewardRuleConfiguration` EF entity configuration with a unique composite index on `(Tier, MaxTierRank)`.
- **Action required:** run `dotnet ef migrations add AddSeasonRewardRules --project Tycoon.Backend.Migrations --startup-project Tycoon.Backend.Api` before next deploy.

### Simplify cleanup (seeder)

- Eliminated all N+1 query patterns: bulk-fetch existing records into `Dictionary<key, entity>` before looping.
- Extracted `ApplyStoreItemFields` and `ApplyQuestionRelations` static helpers to remove field-mapping duplication.
- Questions load existing rows with `Include(q => q.Options).Include(q => q.Tags)` in a single query.
- SeasonRewardRules use a `HashSet<(int Tier, int MaxTierRank)>` for O(1) existence checks.
- Replaced stringly-typed exception filter with `IsObjectNotFoundError()` static helper (precise type-name check + stable S3 `NoSuchKey` error code).
- Added `Tycoon.Shared.Contracts` as an explicit `ProjectReference` in `Tycoon.MigrationService.csproj`.

### Documentation

- Added `docs/minio-seed-data-format.md` — JSON schema examples for all four seed file types with field notes and `mc` upload commands.
- Added `docs/store_admin_backend_handoff_2026-04-23.md` — full API contract for P0/P1/P2 store stock system and admin store endpoints.
- Updated `docs/REMAINING_TASKS.md` — avatar purchase path and MinIO seeders marked complete; SeasonRewardRule migration and store stock P0/P1/P2 backlog added as section 11.

---

## [2026-04-21] 3D Avatar Purchase Path

### New endpoints

- `GET /store/catalog?category=avatar` — avatar-specific catalog with `owned` flag per item (anonymous returns `owned: false`).
- `POST /store/avatars/{avatarId}/purchase` — buy avatar with coins; returns `{success, avatarId, coinsDeducted, newBalance}`; JWT player must match request body.
- `GET /v1/assets/avatars/{avatarId}` — returns a presigned MinIO GET URL for the `.zip` archive; owner-only.

### Storage

- Added `GetPresignedGetUrlAsync(string key, TimeSpan expiry, CancellationToken ct)` to `IPresignedStorage` and implemented in `MinioObjectStorage` with the same internal→public URL rewriting as the PUT method.

### Domain

- Added `ThumbnailUrl` (string?, max 500), `IsFeatured` (bool, default false), and `Version` (string?, max 20) to `StoreItem`.
- Added EF column mappings in `StoreItemConfig`.

### Application handlers

- `GetAvatarCatalog(Guid? PlayerId)` — queries active avatar items, optionally resolves ownership via `PlayerTransaction` aggregation.
- `PurchaseAvatar(Guid PlayerId, string AvatarId)` — validates ownership, balance, and delegates to `PlayerTransactionService`; returns structured error codes (`avatar_not_found`, `already_owned`, `insufficient_funds`).
- `GetAvatarAsset(Guid PlayerId, string AvatarId)` — validates ownership, generates 15-minute presigned GET URL for the archive.

### Shared DTOs

- Added `AvatarCatalogItemDto`, `AvatarCatalogDto`, `PurchaseAvatarRequest`, `PurchaseAvatarResultDto`, `AvatarAssetResponseDto` to `Tycoon.Shared.Contracts`.

---

## [2026-04-21] Store Catalog Premium Compatibility

### Store catalog compatibility
- Updated `GET /store/catalog` so the general catalog remains available to frontend clients even when `StoreItem` rows are not seeded.
- The endpoint still returns the existing `StoreCatalogDto` envelope.
- Active DB-backed `StoreItem` rows remain primary.
- Premium subscription fallback rows are appended from `StorePremiumOptions.AdFree.Plans` when their SKUs are not already present.
- Supported premium fallback filters now include:
  - omitted `itemType`
  - `itemType=premium`
  - `itemType=premium-subscription`
  - `itemType=subscription`
  - `itemType=ad-free`
- Unrelated catalog filters such as `itemType=powerup` do not include premium fallback rows.

### Verification
- Added Premium Store endpoint tests covering `/store/catalog` fallback behavior.
- Verified with:
  - `dotnet build Tycoon.Backend.Api\Tycoon.Backend.Api.csproj`
  - `dotnet build Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-restore`
  - `dotnet test Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-build --no-restore --filter PremiumStoreEndpointsTests`
- Result: `Passed (12/12)`

---

## [2026-04-20] Player Notifications + Direct Messaging v1

### Player notifications backend
- Added authenticated player inbox endpoints:
  - `GET /notifications/inbox`
  - `GET /notifications/unread-count`
  - `POST /notifications/{notificationId}/read`
  - `POST /notifications/read-all`
  - `DELETE /notifications/{notificationId}`
- Added a dedicated `PlayerNotification` persistence model instead of reusing admin notification history.
- Added `PlayerInboxService`-driven inbox query and mutation flow with JSON-backed payload storage.
- Wired friend-request received and friend-request accepted flows into the new player inbox.
- Added a simple system notification source by creating an inbox entry after onboarding reward claim.

### Direct messaging backend
- Added direct messaging endpoints:
  - `GET /messages/conversations`
  - `POST /messages/conversations/direct`
  - `GET /messages/conversations/{conversationId}/messages`
  - `POST /messages/conversations/{conversationId}/messages`
  - `POST /messages/conversations/{conversationId}/read`
  - `GET /messages/unread-count`
- Added dedicated persistence for:
  - `DirectMessageConversation`
  - `DirectMessageConversationParticipant`
  - `DirectMessage`
- Added `DirectMessagingService` with:
  - idempotent direct-conversation creation
  - sender-scoped `clientMessageId` idempotency
  - unread-count derivation from participant read state
  - membership enforcement for read/send/history operations

### Lightweight realtime refresh
- Extended the existing `/ws/notify` SignalR contract with refresh-style player events:
  - `NotificationInboxUpdated`
  - `DirectMessagesUpdated`
- Added SignalR notifier implementations so inbox and DM updates can trigger client refresh without introducing full live-chat transport.

### Contract hardening and test coverage
- Added focused backend tests:
  - `Tycoon.Backend.Api.Tests/Notifications/PlayerNotificationsEndpointsTests.cs`
  - `Tycoon.Backend.Api.Tests/Messaging/MessagesEndpointsTests.cs`
- Verified the new notifications and messaging slice on **2026-04-20** with:
  - `dotnet test Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-build --no-restore --filter "PlayerNotificationsEndpointsTests|MessagesEndpointsTests"`
- Fixed two integration bugs uncovered during test pass:
  - optional pagination defaults for inbox/conversation list endpoints
  - friend-accept transaction handling under the in-memory test provider

### Documentation updates
- Rewrote `docs/notifications_backend_handoff_2026-04-20.md` to reflect the live player-inbox contract, current source integrations, realtime refresh behavior, and backend-standard error envelope.
- Rewrote `docs/messaging_backend_handoff_2026-04-20.md` to reflect the live DM contract, idempotency behavior, refresh events, and current v1 limits.
- Added and verified the relational migration for notifications and messaging:
  - `20260420231724_AddNotificationMessageing`
- Confirmed EF reports no pending model changes after the notifications/messaging migration.
- Re-ran focused backend tests:
  - `PlayerNotificationsEndpointsTests|MessagesEndpointsTests` passed `8/8`
  - `PremiumStoreEndpointsTests` passed `9/9`

## [2026-04-19] Premium Store Backend Fast-Track + Growth Planning

### Premium store backend baseline
- Added authenticated premium store endpoints:
  - `GET /store/premium`
  - `GET /store/rewards/{playerId}`
  - `POST /store/rewards/{playerId}/claim/{rewardId}`
- Premium catalog is now served from config-backed `StorePremiumOptions` with a short-lived in-memory cache.
- Reward claiming now reuses existing `PlayerTransactionService` and `PlayerWallet` infrastructure instead of introducing new persistence in v1.
- Implemented UTC-based claim windows for:
  - `daily-checkin`
  - `watch-ad`

### Premium store contract coverage
- Added typed premium store DTOs to `Tycoon.Shared.Contracts/Dtos/StoreDtos.cs`.
- Added focused contract tests in `Tycoon.Backend.Api.Tests/Store/PremiumStoreEndpointsTests.cs` covering:
  - auth requirements
  - self-only reward access
  - reward-state defaults
  - daily-checkin duplicate prevention
  - watch-ad cap enforcement

### Frontend coordination docs
- Added `docs/premium_store_growth_plan_2026-04-19.md` with a multi-phase long-term growth plan for premium catalog, rewards, entitlements, analytics, and admin tooling.
- Updated `docs/premium_store_backend_handoff_2026-04-20.md` with:
  - current implementation status
  - an explicit verified route matrix for the implemented premium store endpoints
  - actual shipped DTO field names and example payloads
  - correction that the shipped error envelope is the nested backend-standard `error.code` / `error.message` shape
- Updated `docs/premium_store_growth_plan_2026-04-19.md` with a verified backend-baseline section covering the currently implemented routes and supporting subscription routes.

### Endpoint verification
- Re-ran `PremiumStoreEndpointsTests` on 2026-04-20 and confirmed the premium endpoint slice is passing:
  - `GET /store/premium`
  - `GET /store/rewards/{playerId}`
  - `POST /store/rewards/{playerId}/claim/{rewardId}`
- Re-analysis of premium-store/frontend alignment confirmed one remaining transitional gap:
  - the current frontend purchase CTA path can still request `GET /store/offers`
  - this route is not part of the implemented premium-store backend baseline

### Premium purchase-routing guidance
- Expanded premium-store documentation to give frontend a concrete backend-supported replacement for `/store/offers`:
  - `POST /store/subscription/checkout/session`
  - `POST /store/subscription/paypal/create`
  - `GET /store/subscription/status/{playerId}`
  - `POST /store/subscription/portal/session`
- Documented the current premium plan mapping for frontend routing:
  - `premium-monthly` / `sub:premium:monthly` → `tier=premium`, `billingPeriod=monthly`
  - `premium-seasonal` / `sub:premium:seasonal` → `tier=premium`, `billingPeriod=seasonal`

---

## [2026-04-18] Study Surface Deepening + Frontend Handoff

### Backend Study API expansion
- Added a dedicated Study frontend/backend handoff:
  - `docs/study_frontend_backend_handoff_2026-04-18.md`
- Added a lightweight Study status snapshot:
  - `docs/study_frontend_backend_status_2026-04-18.md`

### Study contract status
- Study route family now includes:
  - `GET /study-sets`
  - `GET /study-sets/{id}`
  - `GET /study-sets/recommended`
  - `POST /study-sets`
  - `PATCH /study-sets/{id}`
  - `POST /study-sets/favorites/{questionId}`
  - `DELETE /study-sets/favorites/{questionId}`
  - `POST /study-sessions`
  - `POST /study-sessions/{id}/progress`
  - `GET /study-sessions/{id}/summary`
- Study discovery now supports:
  - generated category sets
  - weak-area sets
  - favorites sets
  - due-review sets
  - custom saved study sets

### Session and review deepening
- Study sessions now persist:
  - `Flashcard` vs `SelfTest` mode
  - ordered session question snapshots
  - explicit flashcard interaction state
  - per-question reveal/confidence/action data for resume flows
- Added persisted `StudyCardState` to support due-review recommendations beyond same-day weak-area rollups.

### Documentation/status updates
- Updated:
  - `docs/trivia_tycoon_quiz_question_learning_migration_plan.md`
  - `docs/trivia_tycoon_migration_patch_order.md`
  - `docs/question_flow_frontend_backend_handoff_2026-04-15.md`
  - `docs/question_flow_compatibility_architecture_handoff_2026-04-15.md`
  - `docs/REMAINING_TASKS.md`

---

## [2026-04-15] Staging Rollback Drill Artifacts Published

### Operator dashboard rollback drill completion
- Completed quarterly live staging rollback drill for operator dashboard failover:
  - Django `operator-dashboard` ➜ Blazor `operator-dashboard-blazor` fallback ➜ Django restore
- Captured and published drill timeline, failover metrics, workflow continuity results, and remediation ownership:
  - `docs/OPERATOR_ROLLBACK_DRILL_STAGING_2026-Q2.md`
  - `docs/OPERATOR_ROLLBACK_DRILL_REPORT_2026-04-08.md`
  - `docs/OPERATOR_RELEASE_ARTIFACTS_2026-04.md`

---

## [2026-04-04] Alpha 6.1 Readiness Tooling + Frontend Handoff

### Deployment readiness tooling (6.1 follow-through)
- Enhanced `scripts/alpha-p0-smoke.sh` live mode:
  - Added auto-signup bootstrap (`AUTO_SIGNUP=true` default in live mode path)
  - Added dynamic `userId` extraction for authenticated follow-up calls
  - Expanded request flow checks to include:
    - questions set/check
    - store catalog
    - IAP validate
    - purchase contract check (accepting expected non-2xx contract statuses)
    - crypto history
    - leaderboard read
- Enhanced `scripts/alpha-p0-smoke.ps1` with equivalent improvements:
  - `-AutoSignup` option
  - Expanded request flow pathing to mirror bash script behavior

### Documentation updates
- Updated `docs/synaptix_remaining_work.md` 6.1 section to reflect:
  - live helper scripts now cover auto-auth + fuller end-to-end path shape
  - explicit remaining requirement to run against a live API and archive evidence
- Added `docs/alpha_release_priority_2026-04-04.md` with an updated alpha-priority status and a concrete completion checklist for unresolved 6.1 tasks.
- Added `docs/frontend_backend_handoff_alpha_2026-04-04.md` for frontend integration planning aligned to completed backend capabilities and remaining gaps.

### Backend ML scorer baseline
- Added authenticated ML scoring endpoints:
  - `POST /ml/churn-risk`
  - `POST /ml/match-quality`
- Both endpoints support deployed model invocation (config-driven URL + optional bearer key) with deterministic heuristic fallback when unavailable.

---

## [2026-03-31] Synaptix BE Packet B — Profile Support

### BE-B1: PlayerPreferences Entity
- Created `PlayerPreferences` domain entity (`Tycoon.Backend.Domain/Entities/PlayerPreferences.cs`)
- Fields: `SynaptixMode` (kids/teen/adult), `PreferredSurface` (hub/arena/labs/pathways/journey/circles/command), `ReducedMotion` (bool), `TonePreference` (playful/balanced/competitive)
- One row per player, created on first PUT
- Sensible defaults: adult mode, hub surface, no reduced motion, balanced tone

### BE-B2: EF Core Persistence
- Created `PlayerPreferencesConfiguration` with unique index on `PlayerId` and max-length constraints
- Added `DbSet<PlayerPreferences>` to `AppDb` and `IAppDb`

### BE-B3: DTOs
- Created `PlayerPreferencesDto` (read) and `UpdatePlayerPreferencesRequest` (write) records
- Update request uses nullable fields — only provided fields are changed (partial update)

### BE-B4: API Endpoints
- `GET /users/me/preferences` — returns current preferences (defaults if none set)
- `PUT /users/me/preferences` — upserts preferences with input validation
- Validates allowed values for mode, surface, and tone
- Requires authorization (uses `ClaimTypes.NameIdentifier`)
- Registered in `Program.cs`

**What was NOT changed (by design)**:
- No existing profile fields or endpoints modified
- No existing profile endpoint paths changed
- No new migrations generated (requires `dotnet ef` in a build environment)

---

## [2026-03-31] Synaptix BE Packet D — Analytics & Stabilization (continued)

### BE-D1: Remaining Analytics Dimensions
- Added `EntryPoint` and `BrandVersion` nullable fields to all 3 analytics models
- Added JSON extraction for `entryPoint`, `brandVersion` in `AnalyticsEndpoints.cs`
- Added `.Keyword("entryPoint")`, `.Keyword("brandVersion")` to both Elasticsearch index templates
- Bumped both template versions to 3
- All 5 planned Synaptix dimensions now complete: `SynaptixMode`, `Surface`, `AudienceSegment`, `EntryPoint`, `BrandVersion`

### BE-C5: Analytics/Admin Terminology Alignment

**Blazor Operator Dashboard** (`Tycoon.OperatorDashboard/`):
- `Events.razor`: "Entry Fee (coins)" → "(Credits)", "Revive Cost (gems)" → "(Synapse Shards)"
- `Economy.razor`: Wallet labels "Coins" → "Credits", "XP" → "Neural XP"
- `Economy.razor`: Grant currency dropdown "Coins" → "Credits", "XP" → "Neural XP"

**Vue Operator Dashboard** (`Synaptix.OperatorDashboard.Vue/`):
- `economy.vue`: Table headers "XP" → "Neural XP", "Coins" → "Credits"
- `economy.vue`: Form labels "XP Delta" → "Neural XP Delta", "Coins Delta" → "Credits Delta"
- `economy.vue`: Transaction/rollback balance messages updated
- `users/[id].vue`: Currency labels "XP" → "Neural XP", "Coins" → "Credits"

**Web/React Operator Dashboard** (`Tycoon.OperatorDashboard.Web/`):
- `EconomyView.tsx`: Currency label map "XP" → "Neural XP", "Coins" → "Credits"
- `EconomyView.tsx`: Form field labels and balance messages updated
- `UserDetailView.tsx`: Currency labels "XP" → "Neural XP", "Coins" → "Credits"

**What was NOT changed (by design)**:
- "Social Accounts" in account-settings — refers to OAuth integrations (Google, Twitter), not Synaptix Circles
- "Diamonds" — kept as-is per terminology reference (no Synaptix rename defined)
- API property names (`balanceCoins`, `balanceXp`, etc.) — contract stability
- Enum values (`CurrencyType.Coins`, etc.) — technical identifiers

---

## [2026-03-29] Synaptix BE Packet D — Analytics & Stabilization

### BE-D1: Analytics Dimensions (Phase 6)

**Analytics Models** (`Tycoon.Backend.Application/Analytics/Models/`):
- Added nullable Synaptix dimensions to `QuestionAnsweredAnalyticsEvent.cs`: `SynaptixMode`, `Surface`, `AudienceSegment`
- Added same dimensions to `QuestionAnsweredDailyRollup.cs` and `QuestionAnsweredPlayerDailyRollup.cs`
- `UpdateFrom()` already copies the new fields during upsert operations

**Analytics Endpoint** (`Tycoon.Backend.Api/Features/Analytics/AnalyticsEndpoints.cs`):
- Added JSON extraction for `synaptixMode`, `surface`, `audienceSegment` in `TryMapQuestionAnsweredEvent()`
- Fields are optional — omitting them leaves null values (backward compatible)

**Elasticsearch Templates** (`Tycoon.Backend.Infrastructure/Analytics/Elastic/ElasticAdmin.cs`):
- Added `.Keyword("synaptixMode")`, `.Keyword("surface")`, `.Keyword("audienceSegment")` to daily rollup template
- Added same fields to player daily rollup template
- Bumped both template versions to 2

**Writers (no changes needed)**:
- `PostgresAnalyticsEventWriter` uses EF Core `SetValues()`/`UpdateFrom()` — handles new fields automatically
- `MongoAnalyticsEventWriter` uses BSON serialization — handles new nullable fields automatically

### BE-D2: Stabilization (Phase 7)
- Verified Swagger titles/descriptions read as "Synaptix API"
- Verified all 3 operator dashboards (Blazor, Vue, Web) display "Synaptix Command"
- Verified no remaining "Tycoon Ops" or "Trivia Tycoon" in operator-visible UI text
- Technical identifiers (`Tycoon.Backend.*` namespaces, cookie keys, container labels) intentionally preserved — deferred to Packet E

---

## [2026-03-29] Synaptix BE Packet C — Product-Language Alignment

### BE-C1: Swagger/OpenAPI
- Swagger tags (Leaderboards, Skills, Matches, etc.) are conventional API labels — kept as-is per plan
- Swagger title and descriptions were already updated in Packet A

### BE-C2: Blazor Operator Dashboard
- `_Host.cshtml` title: "Tycoon Operator Dashboard" → "Synaptix Command"
- `Login.cshtml` title: "Sign In — Tycoon Ops" → "Sign In — Synaptix Command"
- `Login.cshtml` brand title: "Tycoon Ops" → "Synaptix Command"
- `Login.cshtml` logo mark: "T" → "S"
- `Login.cshtml` footer: "Tycoon Backend" → "Synaptix Backend"
- `Login.cshtml` placeholder: "admin@tycoon.local" → "admin@synaptix.local"
- `app.css` header comment: "Tycoon Operator Dashboard" → "Synaptix Command"

### BE-C3: Vue Operator Dashboard
- `dashboard.vue` subtitle: "Tycoon Operator Dashboard" → "Synaptix Command Dashboard"
- `login.vue` brand: "Tycoon Ops" → "Synaptix Command"
- `Footer.vue` copyright: "Tycoon Ops Dashboard" → "Synaptix Command"

### BE-C4: Web/React Operator Dashboard
- `FooterContent.tsx` copyright: "Tycoon Ops Dashboard" → "Synaptix Command"

### BE-C5: Backend Documentation
- `docs/FLUTTER_INTEGRATION.md` heading and description: "Trivia Tycoon" → "Synaptix"
- `README.md` overview: "TycoonTycoon Backend" → "Synaptix Backend"

### What was NOT changed (by design)
- Dashboard inner page headings (Users, Anti-Cheat, Economy, etc.) — conventional admin labels kept per plan
- Swagger endpoint tags — kept as technical API labels
- Code comments referencing `Tycoon.Shared.Contracts` namespace paths — deferred to Packet E
- `admin.ts` type file comments referencing DTO namespaces — deferred to Packet E
- `settingsCookieName: 'tycoon-ops-dashboard'` — persistence key, deferred to Packet E

---

## [2026-03-28] Synaptix BE Packet A — Audit + Brand Surface Reframe

### BE-A1: Backend Surface Inventory (Phase 0)
- Created `docs/backend_surface_inventory.md` — complete audit of all product-visible strings, including Swagger config, dashboard titles, code comments, and documentation headings
- Created risk register documenting items that must NOT be renamed (namespaces, JWT config, DB names, endpoints, DTOs, CI/CD)
- Created deferred technical rename list for Packet E

### BE-A2: Brand Surface Reframe (Phase 1)

**Swagger/OpenAPI** (`Tycoon.Backend.Api/Program.cs`):
- Title: "Tycoon Backend API" → "Synaptix API"
- Description: "Trivia Tycoon Game Backend - Multiplayer Quiz Game API" → "Platform API for Synaptix gameplay, progression, live competition, and player systems."
- Contact: "Tycoon Development Team" → "Synaptix Development Team"
- SwaggerUI endpoint label: "Tycoon Trivia Backend API v1" → "Synaptix API v1"
- DocumentTitle: "Tycoon API Documentation" → "Synaptix API Documentation"

**Blazor Operator Dashboard** (`Tycoon.OperatorDashboard/`):
- App title: "Tycoon Operator Dashboard" → "Synaptix Command"
- Sidebar brand: "Tycoon Ops" → "Synaptix Command"
- Dashboard API-unreachable banner: "tycoon-api" → "synaptix-api"

**Vue Operator Dashboard** (`Synaptix.OperatorDashboard.Vue/`):
- HTML title: "Materio - Vuetify Vuejs Admin Template" → "Synaptix Command"
- Nav header: "Tycoon Ops" → "Synaptix Command"

**Web/React Operator Dashboard** (`Tycoon.OperatorDashboard.Web/`):
- Layout metadata title: "Tycoon Operator Dashboard" → "Synaptix Command"
- Layout metadata description: "managing the Tycoon platform" → "managing the Synaptix platform"
- themeConfig templateName: "Tycoon Ops" → "Synaptix Command"

**Backend Code Comments**:
- `AppDb.cs` XML doc: "Trivia Tycoon" → "Synaptix"

**Documentation**:
- `README.md` heading: "TycoonTycoon Backend" → "Synaptix Backend"
- `README.md` description: updated to reference Synaptix platform

### Separated Migration Plans
- Created `docs/synaptix_frontend_plan.md` — self-contained Flutter app migration plan (FE Packets A–E)
- Created `docs/synaptix_backend_plan.md` — self-contained backend migration plan (BE Packets A–E)

### What was NOT changed (by design)
- `Tycoon.Backend.*` / `Tycoon.Shared.*` namespaces
- Project names and .csproj files
- Endpoint paths and DTO field names
- JWT issuer/audience (`TycoonBackendApi` / `TycoonFrontendApp`)
- Database names, table names, migration identifiers
- gRPC proto package (`tycoon.sidecar`)
- Observability service name (`Tycoon.Backend.Api`)
- CI/CD pipeline names, Docker image names
- Cookie/persistence keys (`tycoon-ops-dashboard`)

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.
- Added Vue dashboard `npm run build:icons` placeholder script and documented it in the Vue README so legacy Docker build flows that invoke this script no longer fail with "Missing script: build:icons".

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.
- Added Vue dashboard `npm run build:icons` placeholder script and documented it in the Vue README so legacy Docker build flows that invoke this script no longer fail with "Missing script: build:icons".
- Fixed `AdminListQuestions` list-item DTO mapping by passing `MediaKey` explicitly and aligning `QuestionListItemDto` contract shape with downstream compile expectations (`UpdatedAtUtc` remains the final required argument).

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.
- Added Vue dashboard `npm run build:icons` placeholder script and documented it in the Vue README so legacy Docker build flows that invoke this script no longer fail with "Missing script: build:icons".
- Fixed `AdminListQuestions` list-item DTO mapping by passing `MediaKey` explicitly and aligning `QuestionListItemDto` contract shape with downstream compile expectations (`UpdatedAtUtc` remains the final required argument).
- Added explicit `builder.Services.AddHttpClient()` in backend API startup to ensure `IHttpClientFactory` can be inferred as a service dependency for minimal-API handlers and prevent startup failure on unresolved `httpClientFactory` parameters.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-04-04] Operator Dashboard Migration Progress (Wave A Foundations)

- Added Vue Wave A shared API client conventions:
  - `Synaptix.OperatorDashboard.Vue/src/lib/apiClient.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/dashboard.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/auditLog.js`
  - `Synaptix.OperatorDashboard.Vue/src/api/users.js`
- Upgraded Vue Wave A views (`DashboardView`, `AuditLogView`, `UsersView`) from static placeholders to API-backed loading/error/data states.
- Wired Vue router guard to `/api/me` session bootstrap (`src/lib/session.js`) instead of hardcoded permissions.
- Expanded Web BFF bootstrap with:
  - Typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
  - Header-based session/bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`) for migration environments.
  - Structured upstream error normalization for non-JSON backend errors and timeout/unreachable handling in `ProxyToBackend`.
- Updated migration status docs (`docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`, Vue README, Web README) with a dated status snapshot and next-step checklist.
- Replaced Wave A JSON `<pre>` placeholders for Audit Log and Users with API-backed table + paging views.
- Replaced Dashboard Wave A JSON placeholder with KPI-style metric cards bound to overview payload values.
- Added initial Wave A query/filter wiring: Audit Log status filter and Users search/isBanned filters.
- Added initial Wave A user action workflow: Users ban/unban actions wired from Vue to Web BFF routes.
- Added initial Wave A action UX polish: operator-provided ban reason and per-row in-flight action state in Users view.
- Re-ran `scripts/run-health-pass.sh` and refreshed `docs/PROJECT_HEALTH_REPORT.md` for 2026-04-04; current environment still blocks `dotnet` and `docker` commands while error-envelope check passes.
- Fixed `scripts/run-health-pass.sh` report generation so the "setup prerequisites" command is rendered as literal markdown instead of being executed during report creation.
- Added Vue dashboard `npm run build:icons` placeholder script and documented it in the Vue README so legacy Docker build flows that invoke this script no longer fail with "Missing script: build:icons".
- Fixed `AdminListQuestions` list-item DTO mapping by passing `MediaKey` explicitly and aligning `QuestionListItemDto` contract shape with downstream compile expectations (`UpdatedAtUtc` remains the final required argument).
- Added explicit `builder.Services.AddHttpClient()` in backend API startup to ensure `IHttpClientFactory` can be inferred as a service dependency for minimal-API handlers and prevent startup failure on unresolved `httpClientFactory` parameters.
- Sidecar Elasticsearch compatibility hardening: pinned `elasticsearch[async]` to `<9.0` and added explicit compatibility headers (`compatible-with=8` by default) so rebalance metrics sink indexing does not fail against Elasticsearch 8 clusters with media-type version mismatch.

---

## [2026-03-28] Durable Sidecar Inference Path in Compose

- Updated `docker/compose.yml` backend service to mount persistent volume `sidecar_inference_data` at `/var/lib/tycoon-sidecar`.
- Added `SIDECAR_INFERENCE_STORE_PATH=/var/lib/tycoon-sidecar/inference-store.jsonl` so the file-backed inference store persists across backend container restarts.
- Updated README sidecar gRPC status section to reflect that file-backed inference storage is now the default baseline.

---

## [2026-03-28] Health Pass Automation Script

- Added `scripts/run-health-pass.sh` to execute the SEQ-5 command checklist and regenerate `docs/PROJECT_HEALTH_REPORT.md` in a consistent format.
- Added fallback execution mode for dotnet-dependent commands using `mcr.microsoft.com/dotnet/sdk:9.0` when local `dotnet` is unavailable but Docker is installed.
- Added CI workflow job `health-pass-report` to execute `scripts/run-health-pass.sh` and upload `docs/PROJECT_HEALTH_REPORT.md` as a workflow artifact.
- Added health-pass command log output under `artifacts/health-pass/` and CI upload of that folder as `project-health-pass-logs`.
- Improved health-pass blocker notes to surface the most actionable missing-tool error line (instead of generic script preamble lines).
- Added CI job `grpc-streaming-tests` to run the new Sidecar/Mobile gRPC-focused test suites as a dedicated validation stage.
- Ran the script in this environment:
  - `check-error-envelope-hardening` passed
  - `dotnet`/`docker` dependent checks remained blocked due to missing local tooling

---

## [2026-03-28] Admin Questions 500 Follow-up + Plan Status Refresh

### Backend query hardening
- Updated `AdminListQuestions` paging query to avoid nested tag-list materialization inside the SQL projection path.
- Switched to two-step retrieval (paged rows + page-scoped tag dictionary) to reduce provider translation/runtime fragility that manifested as repeated dashboard 500 retries on `/admin/questions`.

### Planning/status updates
- Refreshed checklist and health-report status to reflect current SEQ-5 state:
  - `check-error-envelope-hardening` re-run and passing
  - EF schema validation still blocked by missing `dotnet` CLI in this environment
  - Health report exists and tracks blockers plus follow-up actions

---

## [2026-03-28] gRPC Checklist Progress + Health Report Refresh

### gRPC debt-tracking docs
- Marked SEQ-3 and SEQ-4 issue checklists as complete in `docs/GITHUB_ISSUES_CHECKLIST.md` after adding gRPC coverage for sidecar and mobile streaming behavior.
- Added explicit progress note for `MobileMatchGrpcServiceTests` covering answer-result/running-score and live leaderboard stream behavior.
- Updated `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` to reflect expanded test coverage and clarify that execution is pending environment/tool availability.

### Health pass refresh
- Refreshed `docs/PROJECT_HEALTH_REPORT.md` date and latest run notes.
- Re-ran shell health checks:
  - `bash scripts/check-error-envelope-hardening.sh` ✅ pass
  - `bash scripts/validate-ef-schema.sh` ❌ blocked (`dotnet: command not found`)

---

## [2026-03-27] Sidecar gRPC Wiring + Dashboard Build Path Clarification

### Sidecar gRPC
- `SidecarGrpcService` now wires concrete paths for:
  - `ReportAnalyticsEvent` / `StreamAnalyticsEvents` (supports `question_answered` payload mapping + persistence via `IAnalyticsEventWriter`)
  - `SubmitInferenceResult` (stores through `ISidecarInferenceStore`)
  - `TriggerBackendAction` (supports `admin_event_queue_reprocess` via MediatR command dispatch with deterministic errors for unsupported/invalid actions)
- Added `ISidecarInferenceStore` + `InMemorySidecarInferenceStore` and DI registration in API startup.

### Mobile gRPC
- `MobileMatchGrpcService.WatchLeaderboard` now builds live snapshots via MediatR (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshot generation.
- `MobileMatchGrpcService.PlayMatch` now evaluates submitted answers against persisted question answer keys and emits live running score / correct-count updates per participant.
- Added `EvaluateMatchAnswer` MediatR handler in application layer and initial `MatchSession` tests for score progression + stream fan-out behavior.

### Dashboard build source-of-truth
- Blazor operator dashboard remains authoritative in compose (`docker/Dockerfile.dashboard`).
- Alternate Next.js dashboard Dockerfiles are preserved as archived `.txt` artifacts to avoid accidental default build-path drift.

### Docs / planning updates
- README includes sidecar gRPC “current status” contract notes.
- `docs/GITHUB_ISSUES_CHECKLIST.md` marks SEQ-1/SEQ-2 complete and SEQ-3 in progress.
- `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` now tracks Workstream 1 subtasks with completion state.

---

## [2026-03-17] Operator Dashboard — Full Feature Expansion

Expanded `Tycoon.OperatorDashboard` from 7 foundation pages to a complete ops control panel with 12 pages, 46 AdminApiClient methods, and grouped navigation.

### New Pages

#### `Questions.razor`
- Full question bank management: list with search + category filter, paginated results
- Create/edit question form (body, option A–D, correct answer, category, difficulty)
- Delete individual questions, bulk JSON import via textarea paste
- `AdminApiClient`: `ListQuestionsAsync`, `CreateQuestionAsync`, `UpdateQuestionAsync`, `DeleteQuestionAsync`, `BulkImportQuestionsAsync`

#### `Notifications.razor`
- Tabbed interface: **Send**, **Scheduled**, **Dead-Letter Queue**, **Templates**, **History**
- Send immediate notification to any channel with title/body/metadata JSON
- Schedule future notifications with ISO-8601 send-at time, cancel scheduled items
- Replay failed dead-letter deliveries
- Template CRUD (create with key/title/body, delete)
- History search by channel key and delivery status
- `AdminApiClient`: `ListChannelsAsync`, `SendNotificationAsync`, `ScheduleNotificationAsync`, `ListScheduledAsync`, `CancelScheduledAsync`, `GetDeadLetterAsync`, `ReplayDeadLetterAsync`, `ListTemplatesAsync`, `CreateTemplateAsync`, `DeleteTemplateAsync`, `GetNotificationHistoryAsync`

#### `AntiCheat.razor`
- Tabbed interface: **Player Flags**, **Party Flags**, **Analytics**
- Review queue with severity filter; reviewer name + note fields; mark flags reviewed
- Party exploit flags with same review workflow
- Analytics summary for configurable window (6 / 12 / 24 / 48 h)
- `AdminApiClient`: `ListAnticheatFlagsAsync`, `ReviewAnticheatFlagAsync`, `GetAnticheatSummaryAsync`, `ListPartyFlagsAsync`, `ReviewPartyFlagAsync`

#### `AuditLog.razor`
- Security audit log with date-range + status filters
- Paginated results: timestamp, admin, action, IP, status
- `AdminApiClient`: `GetSecurityAuditAsync`

#### `Matches.razor`
- Paginated list of recent matches: ID, mode, host, status, player count, timestamps
- `AdminApiClient`: `ListMatchesAsync`

### Enhanced Pages

#### `Moderation.razor` — expanded
- Added **Player Profile** tab: look up by UUID, display current status/reason/expiry
- Added **Set Status** form: Mute / Warning / Suspended / Banned + reason + notes + optional expiry timestamp
- Added **Action Logs** tab: search historical moderation actions by player ID
- `AdminApiClient`: `GetModerationProfileAsync`, `SetModerationStatusAsync`, `GetModerationLogsAsync`

#### `Economy.razor` — expanded
- Added **Player Wallet** tab: look up wallet balances by player ID
- Added **Grant** tab: grant coins / XP / premium currency to any player
- Added **Transaction History** tab: paginated ledger lookup by player ID
- `AdminApiClient`: `GetPlayerEconomyHistoryAsync`, `CreateTransactionAsync`

#### `Seasons.razor` — expanded
- Added **Reward Claims** tab: search reward claims by season ID and/or player ID
- Added **Recompute Tiers** button on active seasons (purple accent)
- `AdminApiClient`: `GetRewardClaimsAsync`, `ForceRecomputeAsync`

### Navigation Overhaul (`MainLayout.razor`)
Grouped sidebar navigation with four sections:
- **Content** — Questions, Notifications
- **Operations** — Seasons, Game Events, Feature Flags
- **Players** — Users, Moderation, Economy, Anti-Cheat
- **Audit** — Matches, Security Log

### AdminApiClient Completion
All 46 typed methods now implemented. Full coverage of every admin API domain:
Auth (2), Config (2), Seasons (3+2 rewards), Event Queue (3), Users (3), Moderation (4), Economy (3), Questions (5), Notifications (11), Anti-Cheat (5), Matches (1), Security Audit (1).

---

## [2026-03-17] Operator Dashboard + FastAPI Sidecar

Adds two new services to the solution:

### `Tycoon.OperatorDashboard` — Blazor Server operator control panel
- Browser UI for the ops team — no more Swagger/Postman for day-to-day operations
- **Pages:** Dashboard, Seasons (activate/close), Game Events (open/close), Feature Flags (toggle on/off), Users (ban/unban), Moderation (escalation list), Economy (overview)
- Authenticates against `/admin/auth/login`; JWT stored server-side (`TokenStore`) — never sent to the browser
- Typed `AdminApiClient` wraps all admin REST endpoints; attaches `Authorization: Bearer` + `X-Admin-Ops-Key` header
- Registered in `Tycoon.AppHost` with Aspire service discovery (`WithReference(api)`)
- Docker: `docker/Dockerfile.dashboard`, exposed on port `8200`

### `Synaptix.Sidecar` — FastAPI Python service
- `/ml` — match quality scoring, churn risk prediction, question difficulty estimation
- `/analytics` — season KPIs, event funnel, D1/D7/D30 retention (wire up Motor/Elasticsearch)
- `/webhooks` — Stripe payments, generic signed webhooks, push notification proxy
- `/utilities` — season snapshot to MongoDB, bulk question import, backend health probe
- Registered in `Tycoon.AppHost` via `AddExecutable("tycoon-sidecar", "uvicorn", ...)` on port `8100`
- Docker: `docker/Dockerfile.sidecar`, exposed on port `8100`

### Infrastructure changes
- `Tycoon.AppHost/Program.cs` — registers `tycoon-dashboard` and `tycoon-sidecar`
- `docker/compose.yml` — adds `sidecar` and `operator-dashboard` services
- `TycoonTycoon_Backend.slnx` — `Tycoon.OperatorDashboard` added under `/Hosting/`

---

## [2026-03-16] Feature Flag Activation Controls (Part A)

Adds runtime on/off toggles for the three game modes (Game Events, Guardians, Territory) without requiring Hangfire dashboard access or redeployment.

### New Service: `FeatureFlagService`
- **File:** `Tycoon.Backend.Application/Config/FeatureFlagService.cs`
- Scoped per-request; lazy-loads `AdminAppConfig.FeatureFlagsJson` once per scope
- Missing keys default to `true` (safe for zero-downtime rollouts)
- Constants: `GameEventsEnabled = "game_events_enabled"`, `GuardianEnabled = "guardian_enabled"`, `TerritoryEnabled = "territory_enabled"`

### Guards Added
| Component | Flag checked | Early-return status |
|---|---|---|
| `GameEventSchedulerJob` (Hangfire) | `game_events_enabled` | Skips entire job |
| `EnterGameEvent` handler | `game_events_enabled` | `"FeatureDisabled"` → HTTP 503 |
| `ChallengeGuardian` handler | `guardian_enabled` | `"FeatureDisabled"` → HTTP 503 |
| `GuardianAssignmentJob` (Hangfire) | `guardian_enabled` | Skips entire job |
| `StartTerritoryDuel` handler | `territory_enabled` | `"FeatureDisabled"` → HTTP 503 |

### API Layer Changes
- `GameEventsEndpoints` — `"FeatureDisabled"` mapped to HTTP 503
- `GuardiansEndpoints` — `"FeatureDisabled"` mapped to HTTP 503
- `TerritoryEndpoints` — `"FeatureDisabled"` mapped to HTTP 503

### Admin Config Defaults
- `AdminConfigEndpoints.GetOrCreate()` now seeds `game_events_enabled=true`, `guardian_enabled=true`, `territory_enabled=true` on first startup

### How to toggle
```http
PATCH /admin/config
Content-Type: application/json

{ "featureFlags": { "game_events_enabled": false } }
```

---

## [2026-03-16] Flutter Frontend Integration Guide

- Created `docs/FLUTTER_INTEGRATION.md` — authoritative Flutter client reference covering:
  - Project setup (recommended packages: `dio`, `signalr_netcore`, `flutter_secure_storage`)
  - Full authentication flow: signup, login, token refresh, secure storage, Dio interceptor
  - Complete REST API reference for all non-admin endpoints grouped by feature
  - Real-time (SignalR) hub setup, group subscriptions, and all 6 server-push event payloads with payload shapes
  - End-to-end feature flows: Game Event battle royale, Guardian Challenge, Territory Capture, Ranked Match
  - Event system activation controls (how seasons and game events are turned on/off, including the gap: no global toggle)
  - Error handling patterns: HTTP codes, domain status strings, recommended Dart error model
  - Rate limit reference table

---

## [2026-03-16] Game Event Tracking System

Adds a separate event analytics layer covering the GameEvent, Guardian, and Territory game modes. Deliberately **not** mixed with the ranked-ladder (`PlayerSeasonProfile` / `LeaderboardEntry`) so tier assignment is never distorted.

### New Entity: `PlayerEventStats`
- **File:** `Tycoon.Backend.Domain/Entities/PlayerEventStats.cs`
- One row per player-season; updated incrementally (no batch recompute job needed)
- Fields:
  - **GameEvent** — `EventsEntered`, `EventsTop20`, `EventsWon`, `TotalEventXpEarned`, `TotalEventCoinsEarned`, `ChampionBattleEliminations`
  - **Guardian** — `GuardianPromotions`, `GuardianDefencesWon`, `GuardianDefencesLost`, `GuardianDaysTotal`
  - **Territory** — `TilesEverCaptured`, `CurrentTilesOwned`, `PeakXpMultiplierBps`

### EF Infrastructure
- **`PlayerEventStatsConfiguration`** — `player_event_stats` table; unique index on `(SeasonId, PlayerId)`; composite indexes on `(SeasonId, EventsWon)`, `(SeasonId, GuardianDefencesWon)`, `(SeasonId, CurrentTilesOwned)`
- **`IAppDb` / `AppDb`** — `DbSet<PlayerEventStats> PlayerEventStats`
- **Migration `20260315000000_AddPlayerEventStats`** — `CREATE TABLE player_event_stats` with all columns and indexes

### New Service: `PlayerEventStatsService`
- **File:** `Tycoon.Backend.Application/EventStats/PlayerEventStatsService.cs`
- `GetOrCreateAsync(seasonId, playerId, ct)` — upsert helper used by all hooks

### New Query Handlers (`Application/EventStats/`)
| Handler | What it answers |
|---|---|
| `GetGameEventLeaderboard(GameEventId, Page, PageSize)` | Ranked participant list for a closed event, ordered by `FinalRank`, with prize amounts |
| `GetPlayerEventHistory(PlayerId, SeasonId?, Page, PageSize)` | All game events a player entered (optionally filtered to a season), with rank and prize outcomes |
| `GetEventSeasonLeaderboard(SeasonId, SortBy, Page, PageSize)` | Season-wide event standings; `SortBy` = `event_wins` (default), `events_entered`, `guardian_defences`, `tiles_owned` |
| `GetTerritoryDominanceLeaderboard(SeasonId, TierNumber, Top)` | Live top-N tile owners in a tier, aggregated from `TerritoryTile` (no extra table) |

### New DTOs (`Tycoon.Shared.Contracts/Dtos/EventStatsDtos.cs`)
- `EventLeaderboardEntryDto(PlayerId, FinalRank, AwardedXp, AwardedCoins, EliminatedAt?)`
- `PlayerEventHistoryDto(GameEventId, Kind, FinalRank?, AwardedXp, AwardedCoins, EnteredAt)`
- `EventSeasonLeaderboardEntryDto(PlayerId, EventsWon, EventsTop20, EventsEntered, GuardianDefencesWon, GuardianDaysTotal, CurrentTilesOwned, PeakXpMultiplierBps)`
- `TerritoryDominanceDto(PlayerId, TilesOwned, TotalXpMultiplierBps)`

### New API Endpoints (`GameEventStatsEndpoints`)
```
GET /game-events/{gameEventId}/leaderboard?page&pageSize
GET /game-events/players/{playerId}/event-history?seasonId&page&pageSize   [Authorized]
GET /game-events/season-leaderboard?seasonId&sortBy&page&pageSize
GET /territory/{seasonId}/{tierNumber}/dominance?top
```

### Incremental Hooks Added to Existing Handlers
| Handler | Stats updated |
|---|---|
| `EnterGameEvent` | `EventsEntered++` |
| `CloseGameEventAndDistributePrizes` | `EventsTop20++`, `EventsWon++` (rank 1), `TotalEventXpEarned +=`, `TotalEventCoinsEarned +=` |
| `ResolveGuardianChallenge` (challenger wins) | `GuardianPromotions++` (challenger), `GuardianDefencesLost++` (deposed guardian) |
| `ResolveGuardianChallenge` (guardian wins) | `GuardianDefencesWon++` |
| `ResolveTerritoryDuel` (challenger wins) | `TilesEverCaptured++`, `CurrentTilesOwned` refresh, `PeakXpMultiplierBps` high-water mark |
| `GuardianAssignmentJob` (daily) | `GuardianDaysTotal++` per active guardian (idempotent — only increments when economy txn is newly applied) |

### DI Registration
- `PlayerEventStatsService` registered as `AddScoped` in `Application/DependencyInjection.cs`

---

## [2026-03-15] MinIO Backend Integration

### `IObjectStorage` Abstraction
- Added `Tycoon.Backend.Application/Abstractions/IObjectStorage.cs`
  - `Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct)`
  - `string GetPublicUrl(string key)`

### Storage Implementations
- **`MinioObjectStorage`** — Minio SDK v6.0.5, auto-creates bucket on first upload, returns configurable public URL
- **`LocalObjectStorage`** — zero-config fallback writing to `wwwroot/`; used in local dev and tests

### `MinioOptions` POCO
Fields: `Endpoint`, `AccessKey`, `SecretKey`, `Bucket` (default `tycoon-assets`), `UseSSL`, `PublicEndpoint`

### Dependency Injection
- `DependencyInjection.cs` selects implementation at startup:
  - MinIO: when `MinIO:Endpoint` is configured
  - Local: fallback (including in-memory/test branch)

### `AdminMediaEndpoints` Refactor
- Upload handler now injects `IObjectStorage` instead of inlining file-write logic

### Config
- `appsettings.Docker.json` — added `MinIO` section (`minio:9000`, `tycoon-assets`, `PublicEndpoint: localhost:9000`)

### NuGet
- `Directory.Packages.props` — `Minio` v6.0.5
- `Tycoon.Backend.Infrastructure.csproj` — `<PackageReference Include="Minio" />`

### Test
- `AdminMediaTests.Upload_Stores_File_And_Returns_AssetKey_And_Url`
  Chains `/admin/media/intent` → `/admin/media/upload/{assetKey}` multipart POST, asserts `200 OK`, correct `assetKey`, non-empty `url`

---

## [2026-03-15] Realtime File Layout Fix

- Moved `ConnectionRegistry.cs` and `SignalRMatchmakingNotifier.cs` from `Features/Realtime/` → `Realtime/` to match their declared namespace `Tycoon.Backend.Api.Realtime`

---

## [2026-03-15] Vote Feature — SignalR Broadcast

- `VoteCastMessage` shared contract (`Topic`, `Option`, `Counts` dictionary)
- `INotificationClient.VoteCast(VoteCastMessage)` method added
- `NotificationHub.JoinTopic(string topic)` — lets clients subscribe to a topic group (`topic:{topic}`)
- `VoteCastEventHandler` — domain event handler that broadcasts to the topic group on every vote

**Frontend integration:** connect to `/ws/notify`, call `JoinTopic(topic)`, listen for `"VoteCast"` events to update live tallies.

---

## [2026-03-15] Vote Feature — Expanded Options

- Valid vote options extended to: `!A`, `!B`, `!C`, `!D`, `!True`, `!False`
  (supports 3-choice, 4-choice, and true/false poll formats)

---

## [2026-03-15] Vote Feature — Domain, Handlers & API

### Domain
- `Vote` aggregate: `PlayerId`, `Option`, `Topic`, `TimestampUtc`; raises `VoteCastEvent`
- `VoteCastEvent` domain event

### Infrastructure
- `VoteConfiguration`: table `votes`, unique index on `(PlayerId, Topic)`, index on `Topic`
- `AppDb` / `IAppDb`: `DbSet<Vote> Votes`

### Application
- `CastVote` handler: validates option, enforces one-vote-per-player-per-topic, returns `Recorded / DuplicateVote / InvalidOption`
- `GetVoteResults` handler: groups by option, returns counts + percentages ordered by count

### API
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/votes` | Required | Cast a vote |
| `GET`  | `/votes/{topic}/results` | Public | Fetch tally |

### Shared Contracts
- `CastVoteRequest`, `CastVoteResponse`, `VoteOptionResult`, `VoteResultsResponse`

> **Note:** run an EF migration to add the `votes` table before deploying.

---

## [2026-03-15] Unit Test Coverage Expansion

### `Tycoon.Backend.Infrastructure.Tests`
| File | What's covered |
|------|---------------|
| `EfRepositoryTests` | `GetAsync` (found / null), Add staging and persistence, multi-entity |
| `IdempotencyIndexModelTests` | Unique index on `EventId` for `ProcessedGameplayEvent`, `EconomyTransaction`, `SeasonPointTransaction`; composite indexes |
| `AppDbDomainEventCollectionTests` | Domain event collection/clearing in `SaveChangesAsync`, multi-aggregate clearing, `MatchCompletedEvent` raised by `Finish()` |

### `Tycoon.Backend.Application.Tests` — Domain
| File | What's covered |
|------|---------------|
| `PlayerTests` | XP/leveling, score clamping, tier idempotency, match result application |
| `MissionClaimTests` | Progress clamping, completion, `MarkClaimed` guard/idempotency, `Reset` |

### `Tycoon.Backend.Application.Tests` — Handlers / Services
| File | What's covered |
|------|---------------|
| `StartMatchHandlerTests` | Match creation, mode defaulting, multi-host isolation |
| `ClaimMissionHandlerTests` | `NotFound / NotCompleted / AlreadyClaimed / Claimed` flows, type filter |
| `ListMissionsHandlerTests` | Active filter, type filter, ordering, DTO mapping |
| `MissionProgressServiceTests` | All mission keys, win/loss branching, multi-mission updates |
| `EconomyServiceTests` | Applied/Duplicate/Invalid/InsufficientFunds, wallet creation, balance accumulation, transaction persistence, history pagination/clamping |
| `SeasonPointsServiceTests` | Applied/Duplicate, profile creation, point accumulation, zero-clamp on negative delta, transaction persistence, `GetActiveSeasonAsync` scenarios |

---

## [2026-03-12] MinIO Docker Setup

### Docker Compose
- `compose.yml` — `minio` service: API port `9000`, console port `9001`, healthcheck, persistent volume, `tycoon-net`
- `compose.prod.yml` — hides ports in production, enforces required password

### Environment Variables
`.env` / `.env.example`:
```
MINIO_ROOT_USER
MINIO_ROOT_PASSWORD
MINIO_PORT        (default 9000)
MINIO_CONSOLE_PORT (default 9001)
```

### Tooling
- `MakeFile` — MinIO added to health check output; new `shell-minio` target
- `Docker.md` — MinIO documented in services table, connection strings, dev URLs, health check reference

---

## [2026-03-12] MinIO Bucket Setup Guide

- Added `docs/minio-setup.md` (278 lines)
  Covers: console access, bucket creation (UI / `mc` CLI / AWS CLI), naming conventions, access policies, upload/download, presigned URLs, .NET SDK connection examples, health check reference

---

## Pending

- ✅ Vote schema migration is already present in `20260319000000_AddGameEventTables` (`votes` table + indexes).
- Operator Dashboard Priority 4 pages: Media upload, Powerups, Skills seeding (planned)
