# Branch Summary: `claude/add-minio-docker-QBFUx`

Summary of all changes made on this branch against `main`.

---

## Overview

This branch introduced first-class **MinIO object storage** support (Aspire + Docker Compose),
fixed several **admin endpoint routing and security bugs**, expanded **operator dashboard**
pages to align with the real API contracts, and added comprehensive **integration test coverage**
across multiple admin endpoint groups.

---

## Commits (chronological)

### 1. `bf342c7` — Add MinIO to Aspire AppHost and docker-compose (`Tycoon.Hosting.Minio`)

**New project: `Tycoon.Hosting.Minio`**

| File | Description |
|---|---|
| `MinioResource.cs` | `ContainerResource` + `IResourceWithConnectionString`; exposes `ApiEndpoint` (9000) and `ConsoleEndpoint` (9001) |
| `MinioBuilderExtensions.cs` | `AddMinio()` — adds the `minio/minio` container with `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD` env vars and volume support; `WithDataVolume()` / `WithDataBindMount()` helpers |
| `MinioContainerImageTags.cs` | Pinned image tag constant |
| `Tycoon.Hosting.Minio.csproj` | New project targeting `net9.0`, references `Aspire.Hosting` |

**`WithMinioConnection<T>()`** — convenience extension that injects `MinIO__Endpoint`, `MinIO__AccessKey`, `MinIO__SecretKey`, `MinIO__Bucket`, `MinIO__UseSSL` into a dependent Aspire resource automatically.

**`Tycoon.AppHost/Program.cs`**
- Declares `minio-user` / `minio-password` Aspire parameters
- Adds MinIO container with persistent data volume and fixed ports 9000/9001
- Calls `.WithMinioConnection(minio)` on `tycoon-api` so settings flow in on `dotnet run`

**`docker/compose.yml`**
- Adds `MinIO__*` environment variables to the `backend-api` service (`Endpoint=minio:9000`, `PublicEndpoint` defaulting to `localhost:9000`)
- Adds `minio: service_healthy` to the API's `depends_on` block

**`TycoonTycoon_Backend.slnx`** — new project registered in solution.

---

### 2. `1186f6c` — Fix API-unreachable banner: make `ApiBaseUrl` configurable for standalone dev

**Root cause:** `AdminApiClient` base address was hardcoded to `http://tycoon-api`, which only resolves inside Aspire or Docker Compose. Running the dashboard standalone (`dotnet run`) left the HTTP client unable to connect.

| File | Change |
|---|---|
| `Tycoon.OperatorDashboard/Program.cs` | Reads `ApiBaseUrl` from `IConfiguration`; falls back to `"http://tycoon-api"` so Aspire/Compose usage is unchanged |
| `appsettings.Development.json` | Sets `ApiBaseUrl=http://localhost:5100` for standalone dev; adds `DataProtection.KeysPath` and `AdminOps.Key` placeholder |
| `Properties/launchSettings.json` | Adds a **"Standalone"** launch profile that sets `ApiBaseUrl` via env var for IDE use |
| `Tycoon.Backend.Api/Properties/launchSettings.json` | Fixes the API to bind on port **5100** for standalone dev so it matches the dashboard default |

---

### 3. `6ccb2d2` — Fix media upload intent flow (`GET→POST`) and redesign Media page

**Root cause:** `AdminApiClient.GetMediaIntentAsync` was using `GET` but the endpoint requires a `POST` with a `{fileName, contentType, sizeBytes}` body.

| File | Change |
|---|---|
| `AdminApiClient.cs` | `GetMediaIntentAsync` → `POST` with request body; `UploadMediaAsync` updated to accept the `uploadUrl` returned by the intent |
| `Media.razor` | Redesigned to use the proper two-step **intent → upload** flow: selecting a file auto-populates the intent form; Upload button calls intent first, then POSTs to the returned `uploadUrl`; shows `assetKey` + public URL on success with a loading spinner |

---

### 4. `89a4973` — Add integration test coverage for `AdminMatches`, `AdminAudit`, and `AdminModeration`

Three admin endpoint groups had no dedicated test files:

| Test file | Coverage |
|---|---|
| `AdminMatchesEndpointsTests.cs` | Security contracts (no key / wrong key); pagination, page clamping, zero-page defaulting |
| `AdminAuditEndpointsTests.cs` | Security contracts; paged response shape, date-range filter, status filter |
| `AdminModerationEndpointsTests.cs` | Security contracts; default profile for unknown player; ban + profile fetch round-trip; log entry creation after `set-status`; paginated log list; expiry date persistence |

---

### 5. `2b61479` — Fix security: wire `AdminGameEvents` into protected admin group; add lifecycle tests

**Root cause:** `AdminGameEventsEndpoints.Map(WebApplication)` was mapping directly to `app`, causing `POST /admin/game-events/*` to **bypass** the `RequireAdminOpsKey()` filter on the admin `RouteGroupBuilder`.

| File | Change |
|---|---|
| `AdminGameEventsEndpoints.cs` | Signature changed to `Map(RouteGroupBuilder admin)` with group path `/game-events`; all four lifecycle routes (create, open, start, close) now inherit ops-key protection |
| `Program.cs` | `AdminGameEventsEndpoints.Map(admin)` instead of `Map(app)` |
| `AdminGameEventsEndpointsTests.cs` | Security contracts for all four endpoints; create returns `Scheduled` status; lifecycle: create→open→start state transitions; 404 for unknown event IDs |

---

### 6. `67e508d` — Fix routing + add `rebuild-elastic-rollups` tests

**Root cause:** `AdminAnalyticsEndpoints.Map(RouteGroupBuilder admin)` called `admin.MapGroup("/admin/analytics")`, producing an inaccessible **double-prefix** `/admin/admin/analytics/*`.

| File | Change |
|---|---|
| `AdminAnalyticsEndpoints.cs` | Changed to `admin.MapGroup("/analytics")` → resolves correctly to `/admin/analytics/*`; removed redundant `.RequireAuthorization` and `.WithMetadata` already enforced at group level |
| `AdminAnalyticsEndpointsTests.cs` | Security contracts; no-date-range call returns 200 with null dates; with-date-range echoes provided dates; spy verifies `RebuildElasticFromMongoAsync` is called with correct arguments |

---

### 7. `1b24be5` — Fix `AdminMedia` double-prefix routing + add MinIO presigned PUT URLs

**Routing fix:** `AdminMediaEndpoints` was calling `admin.MapGroup("/admin/media")` (double-prefix `/admin/admin/media/*`). Fixed to `admin.MapGroup("/media")`.

**New: `IPresignedStorage` interface** (`Tycoon.Backend.Application/Abstractions/`)
- `GetPresignedPutUrlAsync(key, contentType, expiry, ct)` — implemented by storage backends that support client-side direct uploads

**`MinioObjectStorage` now implements `IPresignedStorage`:**
- Uses Minio SDK `PresignedPutObjectArgs`
- Rewrites the internal container host to `PublicEndpoint` when configured, so the URL is browser-reachable (not just internal-network)

**`MediaService.CreateUploadIntentAsync`** (was synchronous):
- Checks if storage is `IPresignedStorage` at runtime
- MinIO configured → returns a presigned PUT URL (browser uploads directly)
- `LocalObjectStorage` / tests → falls back to `/admin/media/upload/{key}`

---

### 8. `3ff8d9b` — Add `GET /admin/game-events` list endpoint + rewrite Events dashboard page

**API — new list endpoint:**
- `GET /admin/game-events` with `page`, `pageSize`, `status` (optional) query params
- Queries `GameEvents` table via `IAppDb`; returns paginated envelope with `GameEventSummaryDto` items
- Status filter is case-insensitive (`Scheduled` / `Open` / `Live` / `Closed`)

**`AdminApiClient` additions:** `ListGameEventsAsync`, `CreateGameEventAsync`, `OpenGameEventAsync`, `StartGameEventAsync`, `CloseGameEventAsync` — all targeting the correct `/admin/game-events/*` routes.

**`Events.razor` — complete rewrite:**
- Was calling `ListEventQueueAsync` (wrong endpoint) and stale `OpenEventAsync`/`CloseEventAsync` methods
- Now uses `ListGameEventsAsync` for the paginated table
- Status filter dropdown; full lifecycle action buttons per row (Open / Start / Close)
- Inline create form with all required fields
- Colour-coded status badges and pagination with total count

**New tests:** List requires ops key; correct envelope shape; round-trip create→list; status filter returns only matching events.

---

### 9. `3c6632c` — Fix `AdminApiClient` routes + fix `Powerups.razor` contract; add tests

**`AdminApiClient` route fixes:**

| Method | Was (broken) | Fixed to |
|---|---|---|
| `GetPlayerPowerupsAsync` | `GET /admin/players/{id}/powerups` | `GET /admin/powerups/state/{playerId}` |
| `GrantPowerupAsync` | `POST /admin/players/{id}/powerups` | `POST /admin/powerups/grant` (playerId in body) |

**`Powerups.razor` — full contract alignment:**
- Lookup tab: was rendering `type/amount/expiresAt/grantedAt` → now renders `type/quantity/cooldownUntilUtc` matching `PowerupStateDto`
- Grant tab: was sending `{ type, amount, durationHours }` with wrong enum values → now sends `{ eventId, playerId, type, quantity, reason }` matching `GrantPowerupRequest`; type options updated to actual `PowerupType` enum values (`FiftyFifty`, `Skip`, `DoublePoints`, `ExtraTime`)

**New tests (`AdminPowerupsEndpointsTests.cs`):** Security contracts; state for unknown player returns empty list; grant → state round-trip; multiple types tracked independently; repeated grants accumulate quantity.

---

### 10. `81903de` — Fix `ApiBaseUrl` in Docker Compose (`localhost:5100` → `backend-api:5000`)

**Root cause:** `appsettings.Development.json` sets `ApiBaseUrl=http://localhost:5100` for standalone dev. In Docker Compose, `ASPNETCORE_ENVIRONMENT=Development` causes that file to load inside the container, where `localhost` resolves to the container itself — not the API container.

**Fix:** Added `ApiBaseUrl: "http://backend-api:5000"` to the `operator-dashboard` environment block in `docker/compose.yml`. Environment variables have higher precedence than `appsettings.json` files, so this overrides the standalone URL without changing the appsettings file (which remains correct for `dotnet run` outside Docker).

The existing `services__tycoon-api__http__0: "http://backend-api:5000"` is kept for Aspire service discovery compatibility.

---

## Files Added (net new)

| File | Purpose |
|---|---|
| `Tycoon.Hosting.Minio/MinioResource.cs` | Aspire container resource for MinIO |
| `Tycoon.Hosting.Minio/MinioBuilderExtensions.cs` | `AddMinio()` / `WithMinioConnection()` helpers |
| `Tycoon.Hosting.Minio/MinioContainerImageTags.cs` | Pinned image tag |
| `Tycoon.Hosting.Minio/Tycoon.Hosting.Minio.csproj` | New project file |
| `Tycoon.Backend.Application/Abstractions/IPresignedStorage.cs` | Presigned PUT URL abstraction |
| `Tycoon.Backend.Api.Tests/AdminAnalytics/AdminAnalyticsEndpointsTests.cs` | Analytics endpoint tests |
| `Tycoon.Backend.Api.Tests/AdminAudit/AdminAuditEndpointsTests.cs` | Audit endpoint tests |
| `Tycoon.Backend.Api.Tests/AdminMatches/AdminMatchesEndpointsTests.cs` | Matches endpoint tests |
| `Tycoon.Backend.Api.Tests/AdminModeration/AdminModerationEndpointsTests.cs` | Moderation endpoint tests |
| `Tycoon.Backend.Api.Tests/AdminPowerups/AdminPowerupsEndpointsTests.cs` | Powerups endpoint tests |
| `Tycoon.Backend.Api.Tests/GameEvents/AdminGameEventsEndpointsTests.cs` | Game events endpoint tests |
| `docs/minio-setup.md` | MinIO bucket setup and usage guide |

---

## Key Bug Fixes Summary

| Bug | Commit | Impact |
|---|---|---|
| Dashboard can't reach API in Docker (`localhost:5100` in container) | `81903de` | Dashboard non-functional in full Docker stack |
| `AdminGameEvents` bypassed ops-key security gate | `2b61479` | Security vulnerability — unauthenticated lifecycle mutations |
| `AdminAnalytics` double-prefix (`/admin/admin/analytics`) | `67e508d` | Endpoint completely unreachable |
| `AdminMedia` double-prefix (`/admin/admin/media`) | `1b24be5` | Endpoint completely unreachable |
| Media upload used `GET` instead of `POST` for intent | `6ccb2d2` | Upload flow broken in dashboard |
| `AdminApiClient` powerups routes pointed to non-existent paths | `3c6632c` | Powerups page non-functional |
| `Events.razor` calling wrong endpoint (`event-queue` vs `game-events`) | `3ff8d9b` | Events page non-functional |
| `ApiBaseUrl` hardcoded; dashboard broken in standalone dev | `1186f6c` | Dashboard non-functional outside Aspire |
