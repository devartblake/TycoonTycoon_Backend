# Codebase Audit & Implementation Plan

**Date:** 2026-07-02
**Scope:** Full-repository audit of `TycoonTycoon_Backend` (Synaptix .NET 10 microservices + Django/React operator dashboards + Python crypto/sidecar services)
**Audit areas:** missing dotnet packages, API endpoints, Docker misconfiguration, service credentials, miscellaneous issues, unimplemented areas vs. plan docs, security risks.

---

## Executive Summary

The backend is **substantially implemented** and, in several areas, better hardened than its own status docs suggest (prod/staging compose files enforce required secrets and close ports; the main API's JWT validation and CORS are correct; Central Package Management is clean). The material risks are concentrated in five places:

1. **A real secrets file (`docker/.env`) is committed to git** — including production S3 credentials and a valid service JWT.
2. **The OTP password-reset flow is bypassable** — a security hole in a feature the docs label "production ready".
3. **KMS & Compliance APIs have an "accept any unsigned token" JWT fallback**, guarded only by an environment check.
4. **The React operator dashboard is not wired to the real backend** — most of its API calls target routes that do not exist.
5. **The solution file references a deleted project**, breaking solution-level `dotnet build`/`restore`.

### Severity overview

| # | Finding | Severity | Area |
|---|---------|----------|------|
| 1 | `docker/.env` committed with real S3 keys + signed service JWT | **Critical** | Credentials |
| 2 | Real 256-bit JWT signing key committed in appsettings | **Critical** | Credentials |
| 3 | OTP password-reset token bypass | **Critical** | Security / API |
| 4 | KMS/Compliance accept-any-unsigned-token JWT fallback | **High** | Security |
| 5 | `.slnx` references deleted `Synaptix.OperatorDashboard.csproj` | **High** | Build integrity |
| 6 | No global auth fallback policy (auth is opt-in per endpoint) | **High** | Security / API |
| 7 | `/ws` trusts query-string `playerId` (presence spoofing) | **High** | Security / API |
| 8 | Shared CORS fallback = `AllowAnyOrigin()` | **High** | Security |
| 9 | `NEXT_PUBLIC_ADMIN_OPS_KEY` build ARG leaks admin key to browser | **High** | Docker / Security |
| 10 | React dashboard ↔ backend route contract mismatch | **High** | API integration |
| 11 | Django insecure-by-default (DEBUG/ALLOWED_HOSTS/SECRET_KEY) | **Medium** | Security |
| 12 | Root containers, missing HEALTHCHECKs, `latest` tags | **Medium** | Docker |
| 13 | Widespread dev credentials in committed appsettings/scripts | **Medium** | Credentials |
| 14 | IMPLEMENTATION_ROADMAP §5 services never built | **Medium** | Unimplemented |
| 15 | Hangfire dashboard weakly authorized | **Medium** | Security |
| 16 | Package version skew (Grpc, OpenTelemetry), transitive Redis | **Low** | Packages |
| 17 | Legacy `/admin/admin/questions` route; KMS no-op suite ternary | **Low** | API |
| 18 | 8 open P0 alpha ops blockers; deferred post-alpha features | **Info** | Program status |

---

## 1. Credentials

### 1.1 `docker/.env` is committed to git — CRITICAL
The file is tracked (`git ls-files` confirms it). It was committed **before** the `.gitignore` rule for `docker/.env` was added, so the ignore rule gives a false sense of safety while git keeps tracking it. Contents include:

- Real Linode Object Storage credentials for prod bucket `tt-assets-prod` — `S3_ACCESS_KEY`/`S3_SECRET_KEY` (`docker/.env:66-67`), a genuine 40-char secret, not a placeholder.
- A **valid signed HS256 crypto-service JWT** (`CRYPTO_SERVICE_JWT`, exp ~2027; payload `sub=crypto-settlement-worker, scope=crypto:settlement`).
- All infra passwords (`POSTGRES_PASSWORD=tycoon_password_123`, mongo/redis/elastic/rabbitmq/minio `*_password_123`).
- `SUPER_ADMIN_PASSWORD=ChangeMe123!`, `SUPER_ADMIN_EMAIL=admin@tycoon.local`, `ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION`.

**Remediation:** Rotate the Linode S3 keys and the JWT signing key immediately (assume compromised). Then `git rm --cached docker/.env`, confirm `.gitignore` covers it, and purge it from history (`git filter-repo`/BFG). Coordinate the history rewrite with the team.

### 1.2 Real JWT signing key committed — CRITICAL
`Synaptix.Backend.Application/appsettings.Development.json:3` contains a real 64-hex-char (256-bit) `SecretKey`. Anyone with the repo can forge tokens for any environment that loads this config. **Remediation:** replace with a placeholder, rotate, source from env/secret store.

### 1.3 Widespread dev credentials in committed config — Medium
Dev passwords appear in `Synaptix.Backend.Api/appsettings.{Development,Docker,Local}.json`, `Synaptix.MigrationService/appsettings*.json`, `Synaptix.Compliance.Api/appsettings.Development.json`, `Synaptix.Security.Kms.Api/appsettings.Development.json`, and `Synaptix.AppHost/appsettings.Development.json`. Notably **base** `Synaptix.Backend.Api/appsettings.json:332-335` ships `admin/admin` + `postgres` defaults (not just Development). Scripts (`scripts/setup-dev.sh:228+`, `scripts/run-migrations-local.sh:16`) embed dev passwords. The Elasticsearch password is embedded in a URL-form connection string (`docker/compose.yml:448`), a log-leak vector.

**Positives:** No PEM/`.key`/`.pfx` private keys are committed; `secrets/` contains only `.gitkeep`; prod/staging compose files enforce all secrets via `${VAR:?...}` (fail-closed) and require the Linode/KMS/JWT secrets from the environment.

---

## 2. Security

### 2.1 OTP password-reset bypass — CRITICAL
`Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs`:
- `HandleVerifyOtp` (lines 634-644) generates a random reset token but **never persists it** — its own comment says *"In production, store this token"*. It returns `"{email}:{token}"`.
- `HandleResetPassword` (line 678) validates the token with only `request.Token.StartsWith(email + ":")`.

**Impact:** Anyone who knows a registered email can call `/api/v1/auth/reset-password` with token `"<email>:anything"` and set a new password **without ever possessing the OTP**. This directly contradicts the "PRODUCTION READY" / single-use-token claims in the OTP docs.

Two related defects in the same flow:
- **Email enumeration:** `HandleForgotPassword` returns `404 USER_NOT_FOUND` for unknown emails (`AuthEndpoints.cs:546-550`), contradicting the doc's own "don't reveal if email is registered" principle.
- **Non-functional without SendGrid:** `EmailService` returns `false` when `SendGrid:ApiKey` is missing (`EmailService.cs:30-34`), so the flow returns 500 until a key is provisioned.

**Remediation:** Persist a hashed reset token bound to the user with an expiry and single-use flag (reuse the `OtpToken` / `PasswordResetToken` entity patterns already in the domain). Validate by lookup + hash compare + expiry + consume-on-use. Return a uniform 200 for forgot-password regardless of email existence. Add a null-email transport fallback for dev.

### 2.2 KMS & Compliance "accept any unsigned token" fallback — High
`Synaptix.Security.Kms.Api/Program.cs:64-79` and `Synaptix.Compliance.Api/Program.cs:67-82`: when neither `Jwt:Authority` nor `JwtSettings:SecretKey` is configured, a `SignatureValidator` is installed that simply `ReadJwtToken(token)` — no signature check, `ValidateIssuerSigningKey=false`, `ValidateLifetime=false`. The symmetric branch also sets `ValidateIssuer=false`/`ValidateAudience=false`. Only a startup guard (`if (!IsDevelopment() && both empty) throw`) prevents this in production, so a misconfigured deploy still running `ASPNETCORE_ENVIRONMENT=Development` would accept forged tokens against a key-management service.

**Remediation:** Remove the no-signature fallback entirely; hard-fail if no signing key/authority is configured in any environment. Keep the separate `X-Service-Token` header as defense-in-depth, not as the only barrier.

### 2.3 No global auth fallback policy — High
`Synaptix.Backend.Api` configures named authorization policies (`Security/AdminPolicies.cs`) but **no global `FallbackPolicy`**. Authorization is therefore opt-in: any endpoint that omits `.RequireAuthorization()` is publicly reachable. There are 106 `RequireAuthorization` usages across 40 files (most endpoints are covered), but the default-open posture is a systemic footgun. The Analytics ingestion endpoints (`Analytics/AnalyticsEndpoints.cs:67,104,118,130`) are intentionally anonymous.

**Remediation:** Set a `FallbackPolicy` requiring an authenticated user, then explicitly mark the genuinely public endpoints (`/`, `/healthz`, analytics ingest, auth entry points) with `AllowAnonymous`.

### 2.4 `/ws` WebSocket trusts query-string `playerId` — High
`Synaptix.Backend.Api/Program.cs:875-981` reads `playerId` from the query string and registers presence without verifying it against the authenticated principal (lines 889-903). A client can spoof presence for any player. **Remediation:** bind presence to the authenticated user id from the token, not the raw query parameter.

### 2.5 Shared CORS fallback is `AllowAnyOrigin()` — High
`Synaptix.Shared/Web/Extensions/CorsExtensions.cs:22-35`: when `CorsOptions.AllowedUrls` is empty, the policy falls back to `AllowAnyOrigin()` + `AllowAnyMethod()` + `AllowAnyHeader()`. Any service wiring this with an empty allow-list becomes fully open. (The main API uses its own safer policy in `Program.cs:280-302`.) **Remediation:** make the empty-list case deny-by-default.

### 2.6 Hangfire dashboard weakly authorized — Medium
`Synaptix.Backend.Api/Program.cs:770-772` passes an empty filter array in Development (no auth); non-dev uses `HangfireAuthorizationFilter` (lines 1316-1322) which only checks `IsAuthenticated` — any authenticated user, not admin. **Remediation:** require an admin role/policy for `/hangfire` in all environments.

### 2.7 Django insecure-by-default — Medium
`Synaptix.OperatorDashboard.Django/operator_dashboard/settings.py:10-12`: `SECRET_KEY` defaults to `"dev-only-change-me"`, `DEBUG` defaults to `True`, `ALLOWED_HOSTS` defaults to `["*"]`; secure cookies are off unless `DJANGO_SECURE_COOKIES=true`. All compose files override these correctly, but any deployment that forgets the env vars runs wide open. **Remediation:** flip the code defaults to secure (DEBUG=False, empty ALLOWED_HOSTS, no default SECRET_KEY) and let dev opt in.

---

## 3. API Endpoints

All three .NET hosts use **minimal APIs** (no MVC controllers). `Synaptix.CryptoService` and `Synaptix.Sidecar` are Python/FastAPI.

### 3.1 React operator dashboard ↔ backend contract mismatch — High
The React dashboard (`Synaptix.OperatorDashboard.React`) is a complete-looking scaffold (57 tsx pages, built `dist/`) but most of its API layer targets routes that **do not exist** server-side, so it only works in `MOCK_API_MODE`:

| React call (feature `api.ts`) | Backend reality |
|---|---|
| `/admin/dashboard/stats`, `/services/{id}/history` | No `/admin/dashboard` group exists |
| `/admin/operations/{seasons,events,...}` | Server has `/admin/seasons`, `/admin/game-events` |
| `/admin/content/questions[...]`, `/categories`, `/stats`, `/bulk-review` | Server has `/admin/questions` with `/approve`,`/reject`,`/bulk`,`/export` |
| `/admin/economy/players/*`, `/adjust-balance`, `/refund`, `/stats` | Server `/admin/economy` only has `/transactions`,`/history/{id}`,`/balance`,`/simulate`,`/rollback` |
| `/admin/store/products*`, `/stats` | Server uses `/admin/store/catalog`, `/admin/store/analytics/*` |
| `/admin/anti-cheat/stats`,`/queue`,`/flags/{id}`,`/verdict` | Server has `GET /flags`, `PUT /flags/{id}/review`, `/analytics/summary` |
| `/admin/audit/events`, `/stats`, `/ip-locations` | Server only `/admin/audit/security`, `/security/{id}` |
| notifications `/schedules`,`/test-send`,`/dead-letter/{id}/retry` | Server `/scheduled`,`/schedule`,`/send`,`/dead-letter/{id}/replay` |
| `/admin/users/saved-views` | Not on backend (only in Django ORM) |

The **Django dashboard is correctly wired** to the real routes (verified across `admin_store_client.py`, `admin_notifications_client.py`, `admin_skills_client.py`, `admin_event_queue_client.py`, `mongodb_diagnostics.py`). The `Synaptix.Compliance.Client` and `Synaptix.Security.Kms.Client` .NET clients also match their servers.

**Remediation:** Pick the canonical contract per feature area (adjust React to match existing routes, or add the missing backend endpoints). Track as an integration workstream; do not ship the React dashboard against prod until reconciled. See also the earlier `docs/audits/backend_frontend_gap_audit_2026-06-14.md` for the Flutter-side gaps (party, powerups, season-reward claim, compliance path divergence).

### 3.2 Stubs / oddities — Low–Medium
- 501-by-design: `POST /api/v1/auth/mobile-game-login`, `/link-game-account`, `GET /auth/oauth/{provider}` (`AuthEndpoints.cs:127-138`) — blocked on provider credentials.
- Legacy nested route `/admin/admin/questions` (`AdminQuestions/AdminQuestionsEndpoints.cs:23`, mapped inside an already-`/admin` group).
- KMS `InternalEndpoints.cs:54-56` — suite-selection ternary returns `ClassicalV1` in both branches (no-op).
- Config-gated (not stubs): Stripe/PayPal/Apple-IAP paths in `StoreEndpoints.cs` return 503 when unconfigured; Mongo/MinIO admin fallbacks; `AdminOpsKeyMiddleware` 503 when key unset.

---

## 4. Build Integrity

### 4.1 `.slnx` references a deleted project — High
`TycoonTycoon_Backend.slnx:13` references `Synaptix.OperatorDashboard/Synaptix.OperatorDashboard.csproj`, which no longer exists (only `.Django` and `.React` remain — not .NET projects). This breaks solution-level `dotnet restore`/`build`. It is fallout from the completed Blazor removal (`BLAZOR_REMOVAL_PLAN.md`). `.slnx:15` also declares an empty `/services/` folder (cosmetic). All 34 real project references resolve correctly otherwise. **Remediation:** remove the phantom entry.

---

## 5. Docker

- **Root containers (no `USER`):** `Synaptix.Compliance.Api/Dockerfile`, `docker/Dockerfile.migration-service`, `web-companion/Dockerfile{,.dev}`, `Dockerfile.dashboard-web.txt`. (Contrast: `Dockerfile.api`, `.crypto`, `.sidecar`, `.dashboard-django`, `.setup`, and `Dockerfile.migrate` correctly drop privileges — the two migration images are inconsistent.)
- **Missing HEALTHCHECKs:** Compliance API and all three KMS Dockerfiles.
- **`latest`/unpinned tags:** `minio`, `prometheus`, `grafana`, `pgadmin4`, `mongo-express`, `dbgate`, `cloudflared`; unpinned `rabbitmq:management-alpine` and `web-companion` `nginx:alpine` (while `Dockerfile.dashboard-react` pins `nginx:1.27-alpine`).
- **`NEXT_PUBLIC_ADMIN_OPS_KEY` build ARG** (`Dockerfile.dashboard-web.txt:30-31`): `NEXT_PUBLIC_*` values are inlined into the client JS bundle — an admin ops key would ship to every browser. High severity if ever built with a real key. (This file is archived as `.txt`; recommend deleting it or removing the ARG.)
- **Traefik insecure dashboard** in base `docker/compose.yml:271-279` (`--api.insecure=true` on a host port). Prod/staging override this correctly.
- **Data-store ports host-published** in base compose (postgres/mongo/redis/es/rabbit/minio). Dev-only; prod/staging close them with `ports: !override []`.
- **`.dockerignore` gap:** root `.dockerignore` `.env` pattern is path-anchored and does not match `docker/.env`; with build context `context: ..`, `COPY . .` pulls the committed secrets file into intermediate build layers (not the final image, but present in layer history).
- **Vault dev root token** default `dev-root-token-change-me` (`docker/compose.security.yml`) — dev-labeled.

---

## 6. dotnet Packages

**Central Package Management is clean.** Verified deterministically: every `PackageReference` across all 34 `.csproj` files resolves to a `PackageVersion` in `Directory.Packages.props` (157 pins); **zero** inline `Version=` attributes remain; **no** missing declarations. `Verify-CentralPackageManagement.ps1` would pass today. Note the code uses martinothamar **`Mediator`** (source-generated), not MediatR.

Issues to address (all Low):
- **Transitive-only `StackExchange.Redis`:** used directly in `Synaptix.Backend.Api/Program.cs` (`IConnectionMultiplexer`) but only referenced transitively via SignalR/caching packages. Compiles under transitive pinning but is fragile — add a direct reference.
- **Version skew:** `Grpc.Tools 2.80.0` vs `Grpc.AspNetCore 2.76.0`; OpenTelemetry core `1.15.3` vs instrumentation `1.15.1/1.15.2` + beta pins (`...Prometheus.AspNetCore 1.15.3-beta.1`, etc.).
- **Held-back pins (documented tech-debt):** `FluentAssertions 7.0.0` (v8 license change), `StyleCop.Analyzers 1.2.0-beta`, `NBomber 5.11.0`, `SecurityCodeScan.VS2019` (should move to VS2022 package), `FluentValidation 12.1.1` + `FluentValidation.AspNetCore 11.3.1` split.
- **Noise:** ~38 `PackageVersion` entries are declared but unreferenced (harmless). `Fix-CentralPackageManagement.ps1` leaves `.csproj.backup` files behind if run.

---

## 7. Unimplemented vs. Plan Docs

- **IMPLEMENTATION_ROADMAP.md §5 — never built (0 code):** `IAchievementService`/`Achievement`/`UserAchievement`; `IAppealService`/`ModerationAppeal`; `IBatchOperationService` (bulk ban/reward/reset); the enhanced `AdminAuditLog` before/after-diff schema. ~40+ hrs of roadmap "missing components" remain.
- **Resolved / accurate:** Quiz endpoints (roadmap P1) are implemented (`Features/Questions/QuestionsEndpoints.cs`); password-change feature matches its docs and is correct (bcrypt workFactor 12, `.RequireAuthorization()`); Blazor removal is complete; Django performance Phase 1/2 code is real (`bulk_create`, request-scoped caching, `http_client_pool.py`, indexes) — though the PHASE2_VALIDATION_* staging checklists remain unchecked.
- **Doc reliability caveat:** the OTP and (React) `BACKEND_INTEGRATION.md` docs over-claim "COMPLETE / PRODUCTION READY"; treat their status as aspirational.
- **Program/ops status:** `.codex/heartbeat/current-blockers.md` lists **8 open P0 alpha blockers** — all ops evidence (staging migrations unproven, `/health/ready` unrecorded, golden-path/Flutter smoke uncaptured, release-gate workflow, rollback drill, sign-offs blank, operator cutover). `deferred-post-alpha.md` deliberately defers split wallet, monthly spend-limit enforcement (stored but unchecked), parent-account linking + controls endpoints, `/users/me/entitlements`, per-SKU platform/region gating.
- **Minor code smells:** `Synaptix.Backend.Application/Leaderboards/GetArcadeLeaderboard.cs:81` "placeholder comparison" in a scoring predicate (verify correctness); sidecar inference store silently falls back to a temp-file/in-memory store on init failure (`Program.cs:376-391`).

---

## Implementation Plan (phased)

### Phase 0 — Immediate security response (days)
1. **Rotate** the Linode S3 keys and the crypto-service JWT signing key (treat as compromised). Rotate the committed 256-bit `SecretKey`.
2. `git rm --cached docker/.env`; verify `.gitignore`; purge `docker/.env` and the `appsettings.Development.json` key from git history (BFG/`git filter-repo`), coordinated with the team.
3. **Fix the OTP reset bypass** (`AuthEndpoints.cs`): persist a hashed, expiring, single-use reset token bound to the user; validate by lookup + hash + expiry + consume. Reuse existing `OtpToken`/`PasswordResetToken` patterns.
4. Make forgot-password return a uniform 200 (kill email enumeration).
5. Remove the no-signature JWT fallback in KMS/Compliance; hard-fail when unconfigured in every environment.
6. Fix `.slnx:13` phantom project so the solution builds.
7. Bind `/ws` presence to the authenticated principal, not query `playerId`.

### Phase 1 — Hardening (≈1 week)
8. CORS shared fallback → deny-by-default (`CorsExtensions.cs`).
9. Add a global auth `FallbackPolicy`; annotate genuinely public endpoints with `AllowAnonymous`.
10. Admin-gate the Hangfire dashboard in all environments.
11. Flip Django code defaults to secure (DEBUG=False, empty ALLOWED_HOSTS, no default SECRET_KEY).
12. Dockerfiles: add non-root `USER` (Compliance, migration-service, web-companion), add HEALTHCHECKs (Compliance, KMS), pin `latest`/floating tags.
13. Add `docker/.env` to `.dockerignore`; delete `Dockerfile.dashboard-web.txt` (or remove the `NEXT_PUBLIC_ADMIN_OPS_KEY` ARG).

### Phase 2 — Integration reconciliation (1–2 weeks)
14. React dashboard ↔ backend: choose the canonical contract per feature area and either adjust React `features/*/api.ts` to the real routes or add the missing backend endpoints (`/admin/dashboard`, economy player ops, content review/stats, etc.). Remove `MOCK_API_MODE` reliance before prod.
15. Remove the legacy `/admin/admin/questions` nesting; fix the KMS suite-selection no-op.
16. Add a direct `StackExchange.Redis` reference; align Grpc (2.76 vs 2.80) and OpenTelemetry versions.

### Phase 3 — Feature completion & release readiness (scoped from roadmap)
17. Build roadmap §5 services as prioritized: Achievements, Moderation appeals, Batch operations, enhanced admin audit logging.
18. Provision SendGrid so the password-reset flow is functional; add a dev email transport fallback.
19. Close the 8 P0 alpha ops blockers (staging migration proof, readiness/smoke/rollback evidence, sign-offs) and run the PHASE2 staging validation checklists.

---

## Verification
- **This audit made no code changes** — the deliverable is this document.
- Critical findings were spot-checked directly against source: the OTP bypass (`AuthEndpoints.cs:634-644, 678`), the CORS fallback (`CorsExtensions.cs:30`), the committed `docker/.env` (git tracking confirmed), and CPM cleanliness (`comm` diff of referenced vs. declared packages).
- Each remediation phase should be verified by: (Phase 0) a test proving `/reset-password` rejects a forged `"<email>:x"` token and that `git ls-files docker/.env` returns nothing; (Phase 1) `dotnet build` of the solution succeeds and an unauthenticated request to a non-annotated endpoint returns 401; (Phase 2) React dashboard screens load against a live backend without 404s; (Phase 3) new features covered by tests and the P0 ops checklist signed off.
