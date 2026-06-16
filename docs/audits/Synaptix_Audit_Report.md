# Synaptix Platform Audit — Backend (`TycoonTycoon_Backend`) vs Client (`trivia_tycoon`)

**Scope:** Server/client contract consistency, plus production-readiness for an Alpha/Beta live deployment (dev → staging).
**Method:** Both repositories cloned and inspected directly. Backend = .NET 9, clean architecture, ~1,200 C# files, ~35 service projects under the `Synaptix.*` namespace. Client = Flutter/Dart, ~1,530 Dart files. Findings are grounded in the actual source, with file references.

> **Honesty note:** The architecture here is genuinely strong — feature-folder minimal APIs, MediatR, a KMS-backed secure channel, SignalR + raw-WS presence, rate limiting, Traefik TLS, OpenTelemetry, and 148 backend / 265 client test files. The issues below are about the gap between "builds and runs locally" and "survives an Alpha in a live environment." Most are fixable in days, not weeks. I've flagged severity honestly; a few are real launch-blockers.

---

## Severity legend

| Tag | Meaning |
|-----|---------|
| 🔴 **Blocker** | Will break in staging/prod or is a security exposure. Fix before Alpha. |
| 🟠 **High** | Will cause runtime failures, data issues, or bad UX under real load. Fix during Alpha. |
| 🟡 **Medium** | Tech debt / correctness risk that bites later. Schedule for Beta. |
| 🟢 **Low** | Polish / hygiene. |

---

# Part 1 — Server ↔ Client Contract Inconsistencies

These are the highest-value findings: places where the two layers disagree about the API surface. They compile independently, so CI won't catch them — they fail at runtime, in front of testers.

## 1.1 🔴 Auth endpoint path mismatch: `/auth/login` vs `/auth/signup` vs backend reality

**The problem.** The client's `AuthApiClient` hardcodes these paths (`lib/core/services/auth_api_client.dart` L52–57):

```
loginPath            = '/auth/login'
signupPath           = '/auth/signup'
refreshPath          = '/auth/refresh'
logoutPath           = '/auth/logout'
deviceBootstrapPath  = '/auth/device/bootstrap'
accountUpgradePath   = '/auth/account/upgrade'
```

The backend (`Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs`) only maps:

```
/auth/signup   /auth/login   /auth/register   /auth/refresh   /auth/logout
```

**Two endpoints the client calls do not exist on the server:**
- `POST /auth/device/bootstrap` — **not mapped anywhere** in the backend.
- `POST /auth/account/upgrade` — **not mapped anywhere** (grep across all `.cs` returns only the PayPal OAuth token call, which is unrelated).

Likewise `/auth/mobile-game-login`, `/auth/link-game-account`, and `/auth/oauth/{provider}` are referenced in the client but have no server route.

**Why it matters.** Any client flow that hits device bootstrap or guest→account upgrade gets a **404** that the client will interpret as a generic auth failure. For a "guest plays immediately, registers later" mobile funnel (which the device-identity payload in `login()` strongly implies you intend), this is a silent dead end.

**Fix.**
1. Treat the API surface as a **single source of truth**. The cleanest path: generate the client's path constants from the backend's OpenAPI/Swagger doc (the backend already serves `/swagger/v1/swagger.json` in Development). Add a build step that emits a Dart `ApiRoutes` file. This eliminates the entire *class* of drift, not just these instances.
2. Short term: implement the missing endpoints (`/auth/device/bootstrap`, `/auth/account/upgrade`, `/auth/mobile-game-login`, `/auth/link-game-account`, `/auth/oauth/{provider}`) **or** delete the dead client paths and the call sites. Don't leave half-wired flows in an Alpha build — testers will find them.
3. Add a **contract test** (see §3.4) that asserts every client path constant resolves to a mapped server route.

## 1.2 🔴 Inconsistent base-path prefix (`/api/v1`) across client services

**The problem.** The client uses **three different base-path conventions simultaneously**, wired in `lib/core/manager/service_manager.dart`:

- `HttpClient` and `EncryptedApiClient` → `baseUrl + '/api/v1'` (L322, L327).
- `AuthApiClient`, `SecureChannelService` → bare `baseUrl` (no `/api/v1`), then the secure channel manually appends `/api/v1/security/sessions/start` (`secure_channel_service.dart` L73).
- `ApiService` (Dio) → mixes both: most calls are bare (`/quiz/complete`, `/leaderboard`, `/achievements`) but a few hardcode `/api/v1/...` (e.g. `/api/v1/app/config`).

On the backend, **almost nothing is actually served under `/api/v1`**. The minimal-API groups are bare: `/auth`, `/quiz`, `/leaderboard`, `/missions`, `/store`, etc. (confirmed via `MapGroup` grep). Only a handful of routes carry the prefix on purpose: `/api/v1/app/config`, `/api/v1/leaderboard` (a *legacy alias*), `/api/v1/analytics`, `/api/v1/security/sessions/*`.

The Traefik prod config (`docker/traefik/dynamic.yml`) **strips `/api`** before forwarding (`stripPrefix: ["/api"]`). But the prod `compose.prod.yml` routes the backend by **`Host(api.${DOMAIN})`** with **no path prefix and no strip middleware** attached to that router. So:

- In the `dynamic.yml` topology, a client request to `/api/v1/quiz/complete` becomes `/v1/quiz/complete` at the API → **404** (server expects `/quiz/complete`).
- In the `compose.prod.yml` topology, `/api/v1/quiz/complete` reaches the API **unstripped** → also **404**.

Either way the prefix story is incoherent, and which one bites you depends on which compose file staging uses.

**Why it matters.** This is the kind of bug that "works on my machine" because in local dev everything is `localhost:5000` with no proxy, and the bare paths happen to line up for the Dio `ApiService`. The moment you put Traefik in front in staging, a subset of calls 404 and a subset succeed — maddening to debug because it's path-dependent.

**Fix.** Pick **one** convention and enforce it end to end:
- **Recommended:** standardize the public API under `/api/v1` on the **server** (wrap the top-level `MapGroup` registrations in Program.cs in a parent `app.MapGroup("/api/v1")`), keep the Traefik `PathPrefix(/api)` + `stripPrefix` design, and make **every** client service use `baseUrl + '/api/v1'`. Delete the bare-path and double-prefix special cases.
- Whichever you choose, make the prod and dynamic Traefik configs **agree** — right now `compose.prod.yml` and `dynamic.yml` describe different routing. Add the strip middleware to the prod router or move to path-based routing consistently.

## 1.3 🟠 Secure-channel handshake URL vs deployment topology

**The problem.** The Flutter secure channel posts the handshake to `'$baseUrl/api/v1/security/sessions/start'` (`secure_channel_service.dart` L73). On the backend the KMS session endpoints live under `MapGroup("/security/sessions")` inside the **separate** `Synaptix.Security.Kms.Api` service (`SessionEndpoints.cs` L13) — there is **no `/api/v1/security/sessions/start` route on the main API**, and `grep` for `security/sessions` in `Synaptix.Backend.Api` returns nothing. The only callers are the internal `KmsSessionClient` hitting `/security/sessions/start` server-to-server.

So the client expects the **main backend** to expose a public `/api/v1/security/sessions/start`, but that route is (a) on a different service and (b) at a different path (`/security/...`, not `/api/v1/security/...`).

**Why it matters.** Every endpoint marked `RequireSecureChannel()` — which includes `POST /auth/refresh` — depends on a live secure session. If the handshake 404s, the secure channel never establishes, and **token refresh fails**, which means sessions silently die after the access token expires. Testers get logged out mid-session with no clear reason.

There's a mitigating escape hatch: `SecureChannelMiddleware` allows plain JSON when `AdminAuth:AllowTrustedBffPlainJson` + a valid ops key, or in tests. But that's not the mobile client's path.

**Fix.**
1. Decide where the secure-session handshake is publicly exposed. Most likely you want the **main API** to proxy/host `/api/v1/security/sessions/start` (and `/renew`, `/revoke`) and forward to the KMS service internally. Add those public routes to `Synaptix.Backend.Api` and have them call the existing `IKmsSessionClient`.
2. Until that exists, **gate the secure channel behind a feature flag** in the client so `/auth/refresh` can fall back to plain JSON in Alpha. Losing refresh in a live test is worse than losing payload encryption for a closed beta.
3. Add an integration test that runs the **real** handshake against the assembled stack (not the in-memory test bypass), because the test bypass (`SecureChannel:AllowPlainJsonInTests`) currently hides this gap.

## 1.4 🟠 `/auth/refresh` payload & response field-name ambiguity

**The problem.** The client sends the refresh body with **duplicated snake_case and camelCase keys** as a hedge (`auth_api_client.dart` L341–348):

```dart
{ 'refresh_token': ..., 'refreshToken': ..., 'device_id': ..., 'deviceId': ..., ... }
```

and parses responses defensively against both casings (L727–728). The backend contract is a strict positional record: `record RefreshRequest(string RefreshToken)` (`Synaptix.Shared.Contracts/Dtos/AuthDtos.cs` L7) with default System.Text.Json (camelCase-insensitive binding, so `refreshToken`/`RefreshToken` bind, but `refresh_token` does **not** unless a naming policy is set).

**Why it matters.** The "send both keys" pattern is a smell that the contract was never pinned down. It happens to work for refresh because STJ binds `refreshToken`. But it means nobody actually knows the canonical wire format, and the next person who adds an endpoint will guess wrong. The `device_id`/`device_type` fields the client sends are **silently dropped** by the server (the record only has `RefreshToken`) — so if you ever intend to do device-bound refresh-token rotation (you should, for security), it's not wired.

**Fix.**
1. Pin the wire contract. Either configure STJ with `JsonNamingPolicy.SnakeCaseLower` globally and commit to snake_case, or commit to camelCase and **remove the duplicate keys** from the client. Don't ship "both."
2. If device-bound refresh is intended, extend `RefreshRequest` to carry `DeviceId` and validate it server-side against the issued token. Rotating refresh tokens bound to a device is a meaningful anti-theft control for a game economy.

## 1.5 🟠 SignalR payload casing: client reads PascalCase, JSON protocol set to "as-is"

**The problem.** The backend configures the SignalR JSON protocol with `PropertyNamingPolicy = null` (Program.cs L365–369) — i.e., serialize **exactly as the C# property is named** (PascalCase). The Flutter hub clients read PascalCase keys to match: `['PlayerId']`, `['TicketId']`, `['Status']`, `['Timestamp']`, `['Tier']`, etc. (across `lib/core/networking/signalr/*.dart`). So *today they agree* — but only because both sides independently chose PascalCase.

**Why it matters.** This coupling is invisible and fragile. The instant someone "tidies up" the server by setting a camelCase policy (the ASP.NET default everywhere else in this codebase), **every realtime payload silently changes shape** and the Flutter side reads nulls — matchmaking tickets, presence, leaderboard pushes all break with no compile error and no exception, just dead UI. The two layers are also split across **two transports** (raw `/ws` JSON in Program.cs L786 uses camelCase `op`/`ts`/`data`, while the SignalR hubs use PascalCase) — so the client already has to handle two casing conventions for realtime.

**Fix.**
1. **Document the realtime contract explicitly** and add a guard test that serializes a representative server DTO and asserts the exact key casing the client expects.
2. Strongly consider unifying: pick camelCase for **all** wire formats (REST, raw WS, SignalR) and update the Flutter hub readers once. Mixed casing across transports is a long-term footgun.
3. Define shared message DTOs in `Synaptix.Shared.Contracts` and generate the Dart models from them, the same way I recommend for REST routes.

## 1.6 🟡 `/quiz/complete` field-name and trust mismatch

**The problem.** Client posts (`api_service.dart` L204–210):
```dart
{ 'eventId', 'playerId', 'score', 'totalQuestions', 'category' }
```
Server expects (`QuizEndpoints.cs` L31–36): `record CompleteQuizRequest(Guid PlayerId, Guid EventId, int XpEarned, int CoinsEarned)`.

So `score`, `totalQuestions`, `category` are **ignored** by the server, and the server's `XpEarned`/`CoinsEarned` are **never sent** by this client method → they bind to `0`. The quiz-complete call effectively grants nothing.

Separately, this is the **anti-cheat hole**: the design intent (per README: "fair, validated outcomes for scoring, rewards") requires the server to compute rewards from validated answers, not accept client-supplied `XpEarned`/`CoinsEarned`. The current contract lets a tampered client request arbitrary XP/coins.

**Fix.**
1. **Server computes rewards.** The client should send the *evidence* (event id, answer submissions, timing), and the server derives XP/coins from validated results — never accept `XpEarned`/`CoinsEarned` from the client. This closes both the contract mismatch and the cheat vector at once.
2. Reconcile field names; align the Dart model and the C# record on one schema.
3. There's already a `/questions/check` + `/questions/check-batch` grading surface — wire completion to *that* validated result rather than a self-reported score.

## 1.7 🟡 Compliance microservice path mismatch

The client (`compliance_api_client.dart`) calls `/api/compliance/status/{userId}`, `/api/kyc/initiate`, `/api/privacy/...`, `/api/transaction/...`. The `Synaptix.Compliance.Api` exposes `MapGroup("/compliance/age-verification")`, `/compliance/consent`, `/compliance/parental-consent`, `/compliance/privacy-requests`, `/internal/compliance`. The path shapes (`/api/compliance/status` vs `/compliance/age-verification`) don't line up. Given this is the **COPPA/CCPA/KYC/geo-block** layer, a mismatch here isn't just a 404 — it's a regulatory-gate that silently fails open or closed. **Verify every compliance route end-to-end before any real-money or crypto surface is enabled**, and add it to the contract test.

---

# Part 2 — Server Layer: Production Readiness

## 2.1 🔴 Secrets committed in `appsettings.json`

`Synaptix.Backend.Api/appsettings.json` contains real-looking literals:
- DB connection string with `Password=synaptix_password_123` (L3)
- `Jwt:SecretKey = "YOUR-SUPER-SECRET-KEY-MINIMUM-32-CHARACTERS-LONG-FOR-SECURITY"` (L33)
- `AdminOps:Key = "CHANGE_ME"` (L276)

The `.Production.example.json` correctly uses `<REPLACE>` placeholders and env injection — good. But the base `appsettings.json` ships weak defaults, and the JWT signing key is a **symmetric HMAC key** (`SymmetricSecurityKey`, Program.cs L452). If a prod deploy ever inherits the base file's defaults (easy to do by accident), every token is forgeable.

**Fix.**
1. Remove all secret-shaped values from committed `appsettings.json`; leave only non-secret structure. Fail fast at startup if `Jwt:SecretKey` is the dev default outside Development (you already throw when it's *empty* at L432 — extend that to reject the known dev string).
2. Rotate any of these that have ever been deployed.
3. For tokens, consider moving from symmetric HMAC to **asymmetric (RS256/ES256)** so the signing key never leaves the auth service and verifiers only hold the public key. This matters once you have multiple services validating tokens (you already validate audiences `mobile-app`, `admin-app`, `crypto-service`).
4. Add a secret scanner (gitleaks/trufflehog) to CI.

## 2.2 🔴 Duplicate route registration: `/health/ready`

Program.cs maps `/health/ready` **twice** — once as `MapHealthChecks("/health/ready", ...)` (L733) and again as `MapGet("/health/ready", ...)` (L744). ASP.NET will throw an **ambiguous route / duplicate endpoint** exception at startup, or one silently shadows the other depending on order. Either way your readiness probe is unreliable, and in Kubernetes/Traefik an unreliable readiness endpoint means **pods flap in and out of rotation**.

**Fix.** Keep the `MapHealthChecks` registration (it integrates with the health-check system and dependency probes) and delete the manual `MapGet("/health/ready")`. Verify the app boots clean and the probe returns real dependency health.

## 2.3 🟠 In-memory presence/connection state with a partial Redis backplane

`ConnectionRegistry` and `PresenceSessionManager` are `ConcurrentDictionary`-backed, **per-process** (`Realtime/ConnectionRegistry.cs`). SignalR *does* get a Redis backplane — but **only when Redis is configured and not in test mode** (Program.cs L371–379), and the log line `"⚠️ SignalR running without Redis backplane"` shows the no-Redis path is a supported runtime state.

**Why it matters.** The raw `/ws` presence endpoint (Program.cs L786) registers connections in the **in-memory** `PresenceSessionManager` and broadcasts presence by iterating local connections. With more than one API replica (which you'll want the moment you scale past one box), a player connected to replica A is **invisible** to friends connected to replica B. Presence, friend-online indicators, and any "send to player" routing silently fragment across instances. SignalR-over-Redis handles its own groups, but the **raw-WS presence layer has no backplane at all**.

**Fix.**
1. Move presence/connection state to Redis (or the SignalR backplane) so it's shared across replicas. For the raw `/ws` path, either back it with Redis pub/sub for cross-node fan-out or migrate that presence flow onto the SignalR `PresenceHub` (you already have one) so it inherits the backplane.
2. Make Redis **required** in staging/prod — fail startup if absent rather than silently degrading. The current "warn and continue" is fine for local dev, dangerous in a multi-replica env.

## 2.4 🟠 `X-Forwarded-*` not honored behind Traefik

The app runs behind Traefik (TLS terminates at the proxy; `compose.prod.yml` exposes only 80/443 on Traefik, backend has `ports: []`). But I find **no `UseForwardedHeaders` / `ForwardedHeadersOptions`** in Program.cs. Consequences:
- `RequireHttpsMetadata` and any scheme-dependent logic see HTTP, not the original HTTPS.
- Rate-limit partitions keyed on `RemoteIpAddress` (Program.cs L540, L585) will see **Traefik's IP for every request**, collapsing all clients into one bucket — so the `api` and `matches-submit` limiters throttle everyone together or nobody, and per-IP abuse protection is gone.
- Audit logs record the proxy IP, not the client.

**Fix.** Add `app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto })` early in the pipeline, configure `KnownProxies`/`KnownNetworks` to trust only Traefik, and re-key the IP-based rate limiters off the corrected `RemoteIpAddress`. Verify with a request through the proxy that `HttpContext.Connection.RemoteIpAddress` is the real client.

## 2.5 🟠 Database migration strategy at deploy time is unclear

There's a dedicated `Synaptix.MigrationService` and `Synaptix.Backend.Migrations` project plus a `Dockerfile.migrate` — good, that's the right pattern (migrations as a separate job, not on API boot). I confirmed the API does **not** call `Migrate()`/`MigrateAsync()` in Program.cs, which is correct for multi-replica safety. But make sure staging/prod actually **runs the migration job as an ordered step before the API rolls out**, and that two API replicas never race. If the compose/orchestration doesn't gate API start on migration completion, you'll get EF "pending model changes" or schema-mismatch errors under rolling deploys.

**Fix.** Make the migration job a hard predecessor in your deploy pipeline (init container / job dependency), and have the API **fail fast** if the schema version doesn't match expected (an assembly-version check or a `__MigrationsHistory` assertion at startup).

## 2.6 🟡 CORS in production depends on config that may be empty

The `Frontend` CORS policy (Program.cs L271): in Development it allows any localhost origin (fine); in prod it uses `Cors:AllowedOrigins` **only if non-empty**, otherwise the policy has `AllowAnyHeader().AllowAnyMethod()` but **no origins** → effectively blocks browser clients. For a Flutter **web** build (you have a `web/` target and `run_web.sh`), an empty `Cors:AllowedOrigins` in staging silently breaks the web client while mobile (no CORS) works — another "works on mobile, broken on web" trap.

**Fix.** Fail startup if `Cors:AllowedOrigins` is empty in non-Development, or explicitly document that web is unsupported in that env. Don't let it be silently misconfigured.

## 2.7 🟡 gRPC on port 5001 — internal-only vs mobile-facing contradiction

Program.cs comments say port 5001 / HTTP-2 is "**sidecar only, internal network**" (L136), but it also maps `MobileMatchGrpcService` there for "**Flutter clients**" (L919), and the client's `EnvConfig` exposes `grpcHost`/`grpcPort` (default 5001) with `GRPC_USE_TLS`. So 5001 is simultaneously described as internal-only and as a mobile entry point. In prod, Traefik only routes 80/443 and the backend has no host ports — so **how do mobile clients reach gRPC 5001 in prod?** There's no Traefik gRPC router in the configs I can see.

**Fix.** Decide the boundary: if mobile uses gRPC, you need a TLS gRPC route through Traefik (h2c/h2 with the right entrypoint) and the port must be reachable; if gRPC is internal-only, remove the mobile gRPC client path. Right now it's underspecified and will fail in staging.

## 2.8 🟢 Error-envelope consistency

`ApiResponses.Error` emits `{ error: { code, message, details } }` (Contracts/ApiResponses.cs), and the SignalR/feature-flag path emits the same shape. But several handlers return ad-hoc shapes — e.g. `HandleRegistration` returns `Results.BadRequest(new { error = "registration_failed", message })` (AuthEndpoints.cs L45) where `error` is a **string**, not the `{code,message,details}` object. The client's error parser (`api_service.dart` L292) reads `envelope['error']` expecting it to sometimes be a code string and sometimes an object — it's coping with the inconsistency. Standardize every error response on the one envelope and update handlers that drifted.

---

# Part 3 — Client Layer: Production Readiness

## 3.1 🔴 Repo ships ~400 MB of assets; ~214 MB images, ~81 MB 3D, ~69 MB audio

`assets/` is 400 MB of the 754 MB repo. The `pubspec.yaml` bundles whole directories (`assets/images/avatars/`, `assets/avatarPackages/`, `assets/images/quiz/category/`, etc.). This goes straight into the app binary.

**Why it matters.** App-store size limits and user download friction. iOS cellular-download caps and Android's 150 MB base-APK limit (overflow requires Play Asset Delivery / App Bundles) mean a 400 MB asset payload either won't ship or will tank install conversion. For an Alpha it "works" via sideload/TestFlight, but it's a wall you'll hit before public Beta.

**Fix.**
1. Move large/optional assets (3D models, songs, avatar packages, category art) to **remote delivery** — you already have a CDN/MinIO story on the backend (`MinIO:PublicEndpoint`, `cdn.<REPLACE>`) and an `AssetManifestEndpoints` + `asset_resolver.dart` on the client. Lean into it: bundle only first-run-critical assets; fetch the rest on demand with caching.
2. Use **Play Asset Delivery** (Android App Bundle) and **On-Demand Resources** (iOS) for anything that must ship with the store package.
3. Audit `assets/screenshots` (6.5 MB) and `assets/3d` (81 MB) — screenshots almost certainly shouldn't be in the shipped bundle at all.

## 3.2 🟠 3-second connect/receive timeouts are too aggressive for mobile networks

`ApiService` sets `connectTimeout` and `receiveTimeout` to **3 seconds** (`api_service.dart` L90–96). On real cellular (especially the first request after radio wake, or in a beta tester's basement), 3s round-trips will time out constantly, and your defensive refresh/retry logic will compound the load.

**Fix.** Use tiered timeouts: ~10s connect, ~20–30s receive for normal calls; keep short timeouts only for explicitly latency-critical paths (matchmaking poll). Make them env-configurable. Add jittered retry/backoff for idempotent GETs only (never blind-retry POSTs that grant rewards).

## 3.3 🟠 `https→http` silent downgrade for local hosts can leak into staging

`EnvConfig._normalizeApiBaseUrlForRuntime` (`env.dart` ~L160) **silently downgrades https→http** for `localhost`/`10.0.2.2`/`127.0.0.1`. Convenient for dev. But if a staging env var ever points at one of those hosts (e.g. a tunneled/forwarded staging box, or a misconfigured `API_BASE_URL_STAGING=http://localhost:5000` — which is literally what `.env.example` ships), you get **cleartext auth traffic** and a false sense that TLS is on.

**Fix.** Restrict the downgrade to `kDebugMode` only. In profile/release builds, never rewrite the scheme — fail loudly on a bad cert instead, so misconfig is caught before testers' tokens cross the wire in plaintext.

## 3.4 🟠 No contract test guarding client paths against the server (the meta-fix)

Findings 1.1–1.7 all share one root cause: **the client's API surface is hand-maintained string constants** with no automated check against the server. The backend serves Swagger; the client has 265 test files but (from the structure) none assert route parity.

**Fix.** Add a CI job that:
1. Boots the backend (or reads the committed `swagger.json`).
2. Extracts every client path constant (they're already centralized in `AuthApiClient`, `SynaptixApiClient`, `ApiService`).
3. Asserts each resolves to a mapped server route with a compatible method.
This single test would have caught the auth, `/api/v1`, secure-channel, and compliance mismatches above. It's the highest-ROI item in this report.

## 3.5 🟡 Token storage is correct — keep it that way

Credit where due: `AuthTokenStore` puts tokens in `flutter_secure_storage` (Keychain/Keystore) and keeps only non-secret metadata in Hive (`auth_token_store.dart` L130–188), with a legacy-Hive migration path. That's the right design. Two follow-ups: (a) ensure the **legacy Hive session** is wiped after migration (don't leave plaintext-ish tokens in Hive), and (b) confirm secure storage is configured with `IOSAccessibility.first_unlock` (not `always`) so tokens aren't readable before first unlock.

## 3.6 🟡 Dual snake/camel field hedging across the client

The "send both `refresh_token` and `refreshToken`" pattern (§1.4) appears to be a general client habit. It bloats payloads and masks the real contract. Once the server contract is pinned (§1.4, §1.5), do a pass to remove the redundant keys. It also makes the wire logs (which you emit in debug) far easier to read during the Alpha.

## 3.7 🟢 Logging hygiene for release

`ApiService` gates Dio logging behind `ConfigService.enableLogging && kDebugMode` and `AuthApiClient` gates request/response logging on `kDebugMode` — good, no token leakage in release. One thing to double check: the secure-channel and auth logs print full bodies in debug; make sure no internal QA/TestFlight build ships with a debug flag flipped on, or refresh tokens land in device logs.

---

# Part 4 — Cross-Cutting & Sequencing

## 4.1 Versioning the API contract

You have a `/api/v1` aspiration that isn't consistently realized (§1.2). Before Beta, **commit to versioning**: every public route under `/api/v1`, a documented deprecation policy, and the legacy aliases (`/api/v1/leaderboard` already exists as a "legacy" alias) clearly marked. Mobile clients update slowly; once real users are on an Alpha build, you can't move an endpoint without breaking installed apps. Version now while the user count is ~0.

## 4.2 Observability is in place — wire it to alerts

OpenTelemetry/OTLP is configured (`Observability:OtlpEndpoint`), there are `ops/dashboards` and `ops/runbooks`, and `AdminSecurityMetrics` records rate-limit rejects. For the Alpha, make sure you have **alerts** on: 4xx/5xx rate, auth-failure spikes (the contract mismatches above will show here first), SignalR connection churn, and DB/Redis health. The first signal that §1.1–1.3 are biting will be a 404/401 spike — make that page someone.

## 4.3 Suggested fix order for Alpha → Beta

**Before Alpha (blockers):**
1. §2.1 Secrets out of `appsettings.json` + fail-fast on dev JWT key.
2. §2.2 Remove duplicate `/health/ready`.
3. §1.1 Reconcile auth routes (implement or delete device-bootstrap / account-upgrade).
4. §1.2 Decide and enforce one `/api/v1` convention; make Traefik configs agree.
5. §1.3 Expose the secure-session handshake on the reachable host (or flag it off for Alpha).
6. §3.3 Restrict https→http downgrade to debug.

**During Alpha (high):**
7. §3.4 Add the route-parity contract test (prevents regression of everything above).
8. §2.3 Redis-back presence / make Redis required in staging.
9. §2.4 `UseForwardedHeaders` + re-key rate limiters.
10. §1.6 Server-authoritative quiz rewards (anti-cheat).
11. §3.1 Asset delivery off-bundle.
12. §3.2 Realistic mobile timeouts.

**Before Beta (medium):**
13. §1.5 Unify/lock realtime payload casing.
14. §1.7 Verify compliance gate routes end-to-end.
15. §2.5–2.8, §3.5–3.7 hygiene.

---

## Final assessment

This is a **substantial, well-structured platform** — the clean-architecture split, KMS secure channel, feature-flagged realtime, and Traefik/OTEL ops scaffolding are well beyond typical indie-project quality. The risk for your Alpha isn't the architecture; it's the **seams**: a handful of client/server contract drifts (§1.1–1.3 especially) that compile fine and only fail once a proxy and real network sit between the layers, plus a couple of config-hygiene blockers (§2.1, §2.2). Fix the blocker list and add the contract test (§3.4) and you'll have removed the failure modes most likely to embarrass you in front of live testers.

---

# Part 5 — Implementation Status (updated 2026-06-13)

Merged to **`main`** in both repos: backend PRs **#389** (compliance microservice) and **#390** (`alpha-audit-blockers`) plus follow-on prod-infra commits; client PRs **#269** / **#270**. The full backend test suite is **green — 546 passed, 0 failed, 1 skipped** (`Synaptix.Backend.Api.Tests`).

What started as the six Before-Alpha blockers grew to cover most of the During-Alpha (High) list, a desynced-migration repair, and a sweep that took the suite from 33 pre-existing failures to 0. Statuses below are verified against `main`.

## 5.1 ✅ Before Alpha — all six blockers complete

| Finding | Commit(s) | Notes |
|---------|-----------|-------|
| 🔴 §2.2 Duplicate `/health/ready` | `38d81eaa` | Removed the manual `MapGet`; kept `MapHealthChecks`. |
| 🔴 §2.1 Secrets in `appsettings.json` | `89ee91a3` | Blanked secret-shaped values in the committed base file; real values inject per-env, dev keeps working via `appsettings.Development.json`. The dev-key fail-fast was already present. |
| 🔴 §1.1 Auth route mismatch | `0367083b` | `/auth/device/bootstrap` + `/auth/account/upgrade` fully implemented (guest funnel; `User.IsAnonymous` + migration). `mobile-game-login` / `link-game-account` / `oauth/{provider}` registered but **fail closed (501)** — no 404 dead-ends, no unverified-identity bypass. |
| 🔴 §1.2 `/api/v1` prefix | `a1d131b1` + client | All public endpoints under `app.MapGroup("/api/v1")` (~48 groups + `/mobile`); infra and `/admin` un-prefixed. Traefik `dynamic.yml` no longer strips `/api`; client centralised on `EnvConfig.apiV1BaseUrl`. |
| 🟠 §1.3 Secure-channel handshake | `ba015c26` | `/api/v1/security/sessions/{start,renew,revoke}` on the main API, proxying to KMS via `IKmsSessionClient` with bearer forwarding. |
| 🟠 §3.3 https→http downgrade | `1bb8f0d` (client) | Scheme downgrade gated behind `kDebugMode`. |

## 5.2 ✅ During Alpha (High) — mostly complete

| Finding | Status | Where | Notes |
|---------|--------|-------|-------|
| §3.4 Route-parity contract test | **Done** | `RouteParityContractTests.cs` | Asserts every client path (incl. the §1.1 routes, `/api/v1/*`, `/security/sessions/start`) resolves to a mapped backend route. The meta-fix. |
| §1.6 Server-authoritative quiz rewards | **Done** | `QuizEndpoints.cs`, `CompleteQuizHandler.cs` | `/quiz/complete` requires auth, verifies the JWT player matches, **grades answers server-side** against `question.CorrectOptionId`, and derives XP/coins from validated results + difficulty (no client-supplied rewards). Idempotent. `QuizCompletionAntiCheatTests` added. |
| §2.3 Redis-backed presence | **Done** | `RedisConnectionRegistry`, `RedisPresenceSessionManager` | Presence/connection state moved to Redis; **fails startup in Staging/Prod if Redis is absent** (`Realtime:RequireRedis`). |
| §2.4 `UseForwardedHeaders` + rate limiters | **Done** | `Program.cs` | `ForwardedHeadersOptions` with `KnownProxies`/`KnownNetworks`, `app.UseForwardedHeaders()` early; IP rate limiters key off the corrected client IP. |
| §3.2 Realistic mobile timeouts | **Done** | client `env.dart` | 10s connect / 30s receive, env-overridable (`API_*_TIMEOUT_SECONDS`); no longer the old 3s. |
| §2.5 Migration deploy gating | **Done** | `compose.prod.yml` | `backend-api depends_on: migration: condition: service_completed_successfully` — the migration job is a hard predecessor. |
| Backend integration tests → `/api/v1` | **Done** | `26b1ac1c` | All test request paths moved to `/api/v1/*` after the §1.2 move. |
| Client provider-button gating | **Done** | `29eedf3` (client) | OAuth / Game Center / Play Games buttons + silent auto-login hidden behind `EXTERNAL_AUTH_PROVIDERS_ENABLED` (default off) so testers don't hit the 501s. |

**Still open (High):**
- **§1.1 Real provider verification** — Apple Game Center / Google Play Games signature validation, OAuth client config + callback/token exchange, and a queryable game-identity table. The routes exist and fail closed (501); completing them needs provider credentials. (Intentionally not faked — that would be an auth bypass.)
- **§3.1 Off-bundle asset delivery** — the client still bundles ~400 MB; not yet migrated to CDN / on-demand delivery.

## 5.3 ✅ Discovered & resolved while implementing

- **EF model snapshot desync (the prior 5.2 high item) — RESOLVED** (`449d67fb`). The `AppDbModelSnapshot` had drifted from the migration history (bad merge): it was missing schema that earlier migrations (`AddQuestionTaxonomy`, `AddQuestionTaxonomySuggestions`, `AddRewardReactor`) had already applied. Regenerating rewrote the snapshot to match the model (verified: a follow-up `migrations add` produces an **empty** diff), and the new migration creates **only** the two genuinely-unmigrated tables (`player_lookup_codes`, `reward_chain_tickets`).
- **Pre-existing compile break — fixed** (`515ec707`). `ComplianceClient.cs` had two missing braces (from `cf1999aa`), masked by incremental-build caching.
- **`/matches/start` claim bug — fixed** (`76be7dee`). It read `ClaimTypes.NameIdentifier` (always null with `sub`), so it forbade every real user; now reads `sub`. A real production bug.
- **AdminStorage object-list prefix bug — fixed**. `GET /admin/storage/objects?prefix=seeds/` 400'd because `NormalizePrefix` rejected the canonical trailing-slash form; now stripped before key validation.
- **Learning-module weak-category matching — fixed**. Recommendations now match weak categories on both the taxonomy-canonical key and the normalized key, so categories the taxonomy doesn't recognise still surface their modules.
- **Test suite: 33 pre-existing failures → 0.** Match/season/party/anti-cheat tests authenticated via a new `TestAuth` helper (they predated `.RequireAuthorization()`); store/IAP tests seed the DB `store_purchases_enabled` flag; the gRPC test was fixed for the 4-arg constructor; the reactor active-event test made deterministic; AdminStore contract assertions corrected (envelope `error.code`, `totalItems`) and `storePurchasesEnabled` added to the status DTO; the SecureChannel AAD assertion updated for the `/api/v1` path; the AdminAuth scopes test moved off a removed private field onto `AdminPermissionProfiles`.

## 5.4 ⏳ Remaining — not yet started

**High:** §1.1 real provider verification (needs credentials); §3.1 off-bundle assets.

**Before Beta (Medium):**
- §1.4 Pin the `/auth/refresh` wire contract (drop the dual snake/camel keys; add device-bound refresh). Client still sends both `refresh_token`/`refreshToken`; server record is still `RefreshRequest(string RefreshToken)`.
- §1.5 Lock the realtime payload casing — SignalR still serialises PascalCase (`PropertyNamingPolicy = null`); no guard test yet.
- §1.7 Verify compliance routes end-to-end — client still calls `/api/compliance/*`, `/api/kyc/*` while the service exposes `/compliance/age-verification` etc. **Must reconcile before any real-money / crypto surface.**
- §2.6 Fail startup on empty `Cors:AllowedOrigins` in non-Development.
- §2.7 Resolve the gRPC 5001 internal-vs-mobile boundary (Traefik gRPC route, or remove the mobile gRPC path).
- §2.8 Standardise the error envelope — several handlers still return ad-hoc `{ error: "string" }` shapes.
- §3.6 Remove the dual snake/camel field hedging in the client once §1.4/§1.5 are pinned.

**Already correct (no action):** §3.5 token storage, §3.7 release logging.
