# Alpha Release Priority Plan (Tonight)

Date: 2026-04-01 (UTC)
Release target: 2026-04-02 00:00 UTC

## Status refresh — 2026-04-04 (UTC)

- [x] Priority 2 (questions hardening) complete.
- [x] Priority 3 (store/IAP baseline) complete.
- [x] Priority 5 (crypto baseline + settlement controls) complete.
- [x] Priority 6 backend gap-baseline complete, including ML scoring endpoints/fallback paths.
- [ ] Priority 1 runtime build/migration verification still pending in .NET-capable environment.
- [ ] 6.1 closeout still needs live runtime evidence + recorded go/no-go note.

## Scope checked
- Repo-wide `TODO` scan
- `docs/synaptix_backend_cross_comparison_status.md`
- `docs/synaptix_frontend_cross_comparison_status.md`

## TODO inventory (actionable for alpha)

### High priority (must resolve or explicitly defer before alpha)
1. `Tycoon.Shared/EF/Interceptors/AuditInterceptor.cs`
   - TODO: wire current user resolution in audit interceptor.
   - Impact: audit trail quality and operator trust.
2. `Tycoon.Shared/Validation/Extensions/RegistrationExtensions.cs`
   - TODO: internal validator registration issue.
   - Impact: validation coverage/consistency risk.
3. `Tycoon.OperatorDashboard.Web/src/@menu/hooks/useVerticalNav.tsx`
4. `Tycoon.OperatorDashboard.Web/src/@menu/hooks/useVerticalMenu.tsx`
   - TODO: better error messages.
   - Impact: weaker production diagnosability and UX during failures.

### Medium/low priority (safe to defer if alpha scope is backend + release stability)
- Vuetify/layout/scss TODOs in `Synaptix.OperatorDashboard.Vue` (mostly design-system cleanup or upstream issue references).
- Formatter research TODO in `Synaptix.OperatorDashboard.Vue/src/@core/utils/formatters.ts`.

## Cross-comparison docs status (as of 2026-04-01)

Both docs are newly added today and agree on the main gap: backend gameplay/economy APIs are the critical dependency for full frontend production behavior.

### Backend status highlights
- Completed: packets A/C/D, preferences endpoints, analytics dimensions, operator terminology alignment.
- Open: alpha gameplay backend (auth/profile/quiz/leaderboard/economy/store), authoritative economy sync, crypto layer, runtime build+migration verification.

### Frontend status highlights
- Completed: packets A/B/C/D, onboarding evolution, hub polish, local wallet persistence.
- Open (non-backend): retention hook, sound cues, runtime QA sweep.
- Open (backend-dependent): authoritative wallet sync, rewards reconciliation, crypto UX, full API-backed feature completion.

## Recommended alpha priorities for tonight

### P0 (release blockers)
1. Execute backend runtime verification:
   - `dotnet build` on solution
   - EF migration generation/apply for preferences path
   - smoke-test startup for dashboards/APIs
2. Decide and lock alpha scope for backend gameplay APIs:
   - minimally auth + profile sync + quiz submit + leaderboard read + economy state read/write
3. Define explicit defer list (signed-off):
   - crypto economy layer
   - advanced social/multiplayer if not stable

### P1 (very high value, can land immediately after blocker checks)
1. Fix actionable backend/shared TODOs (audit user + validator registration).
2. Replace placeholder error messages in operator dashboard hooks.
3. Do one cross-stack vocabulary pass to ensure zero mixed branding.

### P2 (post-midnight or next-day)
1. Frontend retention hook (bonus challenge + streak).
2. Sound cue layer.
3. Optional Packet E rename cleanup.

## Suggested owner split for tonight
- Backend lead: build/migration verification + alpha APIs + economy state.
- Frontend lead: runtime onboarding QA + retention hook (if no backend dependency).
- Ops/QA: cross-stack terminology and telemetry smoke checks.

## Go/No-Go checklist at 23:45 UTC
- [ ] Backend build + migrations pass in real environment
- [ ] Critical alpha APIs reachable and returning expected payload shape
- [ ] No unresolved P0 TODOs without explicit defer decision
- [ ] Vocabulary consistency pass complete
- [ ] Release notes include clearly deferred items

---

## Next execution sequence (single-owner mode)

Owner: You (solo execution)
Policy: Ship if P0 passes, defer P1
Scope: All core alpha APIs

### Step 1 — Build + migration gate (P0)
1. Run `dotnet build` for the solution.
2. Generate migrations (if needed) and apply to local dev DB.
3. Capture the exact command output in release notes.

Exit criteria:
- Build is green.
- Migration is applied cleanly.
- App boots without DI/runtime startup errors.

### Step 2 — API readiness gate (P0)
Verify each required API in this order:
1. Auth
2. Profile sync
3. Quiz submit
4. Leaderboard read
5. Economy state read/write

Exit criteria:
- Each endpoint returns expected status code and payload shape.
- No blocking 5xx errors in local logs.

### Step 3 — Cross-stack language + telemetry smoke gate (P0)
1. Launch operator surface(s) and app client.
2. Run one test flow end-to-end.
3. Confirm:
   - no mixed old/new product labels in visible UX
   - expected analytics/event payload fields still flow

Exit criteria:
- Vocabulary is consistent in app + operator views.
- Telemetry appears for the same test session.

### Step 4 — Release decision
Go:
- All P0 gates pass.

No-Go:
- Any P0 gate fails without a safe temporary mitigation.

### Step 5 — P1 deferrals (allowed by policy)
If P0 passes, explicitly defer to next window:
- retention hook
- sound cues
- additional polish items
- optional Packet E renames

---

## Strict execution checklist (ordered by blocker impact)

### NOW (release blockers — do before any ship decision)
1. [ ] Build gate passes in real environment (`dotnet build` on solution).
2. [ ] Database migration gate passes (`dotnet ef database update` in target dev env).
3. [ ] API P0 smoke checks pass (Auth, Questions, Store, Economy, Leaderboards, Crypto routes). 2026-05-10 update: local Docker crypto contract smoke passed via `scripts/crypto-contract-smoke.ps1`; staging still needs credentials.
4. [ ] Strict IAP precheck complete in Development:
   - real (non-placeholder) provider config values present
   - `/store/iap/validate` no longer returns `IAP_STRICT_CONFIG_MISSING` for valid test requests
5. [ ] One end-to-end player path validated:
   - login -> questions set/check -> purchase path -> leaderboard read
6. [ ] Go/No-Go decision recorded with explicit defer list.

### NEXT (high priority immediately after blockers clear)
1. [ ] Questions pipeline hardening:
   - authoritative match/session integration
   - question bank workflow (category/difficulty/approval)
2. [ ] Store hardening:
   - strict provider-side Apple/Google verification behavior validated
   - inventory/cosmetics endpoint design finalized
3. [ ] Crypto hardening:
   - withdrawal settlement worker/approval flow
   - prize-pool design and audit trail
4. [ ] Cross-stack validation:
   - terminology consistency across app + operator dashboards
   - telemetry continuity verification

### LATER (non-blocking for current alpha cut)
1. [ ] Frontend polish:
   - retention hooks
   - sound cues
   - accessibility/copy sweep
2. [ ] Optional crypto expansion:
   - staking
   - richer wallet history UX
3. [ ] Packet E technical cleanup:
   - namespace/package renames
   - key/cookie/identifier cleanup across CI/runtime surfaces

---

## Execution update — 2026-04-03 (UTC)

What was executed now:
- [x] Route-surface P0 smoke in `routes` mode:
  - command: `SMOKE_MODE=routes bash ./scripts/alpha-p0-smoke.sh`
  - result: pass (`P0 smoke route-check completed.`)
- [ ] Full .NET build gate:
  - attempted: `dotnet build Tycoon.sln`
  - blocker: `dotnet` SDK unavailable in this environment (`dotnet: command not found`)
- [ ] Migration gate:
  - blocked pending .NET SDK/runtime-capable environment

## Backend-only remaining work (no frontend input required)

### NOW (complete in backend environment first)
1. [ ] Install/attach .NET SDK environment and run `dotnet build Tycoon.sln`.
2. [ ] Run DB migration apply in target dev/staging environment (`dotnet ef database update`).
3. [ ] Execute live-mode P0 smoke against a running backend (`BASE_URL=... ./scripts/alpha-p0-smoke.sh`).
4. [ ] Validate strict IAP config with non-placeholder Apple/Google values and successful `/store/iap/validate` path.
5. [ ] Record Go/No-Go with explicit backend defer list.

### NEXT (backend hardening immediately after NOW)
1. [ ] Questions service hardening (authoritative session/match scoring flow).
2. [ ] Store hardening (provider-side verification + inventory/cosmetics contract finalization).
3. [ ] Crypto hardening (withdrawal settlement worker + approval/audit flow).
4. [ ] Add backend integration tests for Auth/Questions/Store/Economy/Leaderboard/Crypto critical flows.
5. [ ] Add CI path for live smoke mode once a runtime test environment is available.

### LATER (backend-only, non-blocking for current alpha)
1. [ ] Packet E backend technical cleanup (`Tycoon.*` -> `Synaptix.*` namespace/project identifiers).
2. [ ] Extended platform APIs (seasons, social, multiplayer) after alpha stability window.
3. [ ] Optional crypto expansion (staking + richer ledger/history capabilities).

---

## Warning triage update — 2026-04-03 (UTC)

Completed now:
1. [x] Fixed CI-breaking issues first:
   - PowerShell smoke argument binding issue (`Password` -> `LoginPassword`)
   - `TypeExtensions.GetExtensionMethod` compile error (`methodName` variable mismatch)
2. [x] Warning triage pass #1 for high-signal web extension warnings:
   - `HeaderDictionaryExtensions` conversion/nullability cleanup
   - `QueryCollectionExtensions` conversion/nullability cleanup

Now / Next / Later follow-through:
- **NOW**: run full `dotnet build` in CI/local runner and confirm no remaining errors.
- **NEXT**: continue warning triage in `Tycoon.Shared` focusing on nullability + obsolete API calls with highest runtime impact.
- **LATER**: broad warning debt cleanup sweep after alpha gate checks and live smoke/IAP gates are green.

## Status update — 2026-04-04 (UTC)

Completed:
1. [x] Verified/fixed `TypeExtensions.GetExtensionMethod` compile issue (`methodName` symbol alignment at the failing location).
2. [x] Added NOW-gate script check to prevent regression of the same compile issue in future local/CI checks.

Current plan status:
- **NOW**:
  - static compile-fix guard check: complete
  - route smoke gate: complete
  - full `dotnet build`/migration gate: pending .NET-capable environment
- **NEXT**:
  - continue warning cleanup in `Tycoon.Shared` (nullability/obsolete API passes)
- **LATER**:
  - broader warning debt sweep after NOW gates pass in runtime-capable environment

## Execution update — 2026-04-04 (NOW/Next/Later continuation)

Delivered:
1. Added `scripts/alpha-now-complete.sh` to execute NOW gates in sequence:
   - static/route guard
   - build gate
   - optional migration gate
   - optional live smoke + strict IAP gate
   - Go/No-Go note
2. Added CI `now-build-gate` job in `.github/workflows/alpha-p0-smoke.yml`:
   - installs .NET 8 SDK
   - runs NOW automation with build + route checks enabled

Plan movement:
- **NOW**: automated and moved into CI execution path (build + route checks).
- **NEXT**: continue warning triage once NOW CI gates are stable.
- **LATER**: keep broader warning-debt and platform expansion items deferred until NEXT is complete.

NEXT progress (2026-04-04):
- [x] Reduced immediate warning noise in `Tycoon.Shared` by:
  - replacing obsolete implicit Redis channel conversion usage with `RedisChannel.Literal(...)`
  - assigning safe defaults for `RedisOptions.Host` and `CorsOptions.AllowedUrls`
- [x] Additional warning triage pass:
  - aligned `MessagePackHybridCacheSerializerFactory.TryCreateSerializer` out-nullability with interface contract
  - constrained `IHaveIdentity<TId>` to `TId : notnull` for safer identity projection
  - updated `SqlKataExtensions.QueryOneAsync` to return `Task<T?>` (matches `QueryFirstOrDefaultAsync` behavior)
- [x] Metrics warning cleanup:
  - initialized `_timer` fields in command/query metrics handlers to avoid non-nullable initialization warnings
  - switched handler duration recording to `Elapsed.TotalSeconds` for more accurate duration telemetry
- [x] Obsolete API warning cleanup:
  - replaced `IApplicationLifetime` check with `IHostApplicationLifetime` in DI dependency validation path
- [x] Integration test coverage start for NEXT:
  - added `AlphaP0RouteContractsTests` to verify core P0 GET/POST routes are mapped (non-404 contract checks)
  - added anonymous request contract assertions for sensitive POST routes (`/store/iap/validate`, `/crypto/withdraw`) to ensure non-404/non-500 behavior under unauthenticated access
- [x] Nullability warning cleanup:
  - guarded nullable `HttpRequestException.StatusCode` mapping in default problem-detail mapper
  - made propagation-context property-name parsing null-safe in JSON converter
- [x] CI blocker fixes:
  - NOW build scripts switched from missing `Tycoon.sln` to explicit `Tycoon.Backend.Api/Tycoon.Backend.Api.csproj` build target
  - Hangfire disabled automatically when `Testing:UseInMemoryDb=true`, preventing test-host attempts to connect to `127.0.0.1:5432`
- [ ] Continue with remaining nullability warning passes after CI build results from the NOW gate.

## Current completion status (2026-04-04 UTC)

### NOW tasks — completion check
1. [x] Route/static gates automated and passing in local script (`alpha-now-status.sh`).
2. [x] NOW gates wired into CI (`now-build-gate` in `alpha-p0-smoke.yml`).
3. [ ] Build gate confirmed green in a real run output (`dotnet build Tycoon.Backend.Api/Tycoon.Backend.Api.csproj`).
4. [ ] Migration gate confirmed green in a real run output (`dotnet ef database update`).
5. [ ] Live smoke + strict-IAP gate confirmed green against running API.
6. [ ] Go/No-Go decision recorded after all NOW runtime gates pass.

**Are all NOW tasks complete?**  
No — automation is in place, but runtime confirmations (build/migration/live strict-IAP/Go-NoGo) are still pending execution evidence.

### Remaining NEXT tasks
1. [ ] Continue nullability warning cleanup in `Tycoon.Shared` (post-NOW CI feedback loop).
2. [ ] Triage obsolete API warnings and replace with supported alternatives where safe.
3. [ ] Expand integration tests to include authenticated happy-path assertions (Auth token acquisition + Store/Crypto success flows), not only anonymous/error contracts.
4. [ ] Resolve EF schema-drift gate by adding/updating migrations in `Tycoon.Backend.Migrations`.

### Remaining LATER tasks
1. [ ] Packet E backend technical cleanup (`Tycoon.*` -> `Synaptix.*` namespace/project identifiers).
2. [ ] Broader warning-debt cleanup sweep after NOW/NEXT stabilize.
3. [ ] Extended platform APIs (seasons/social/multiplayer) after alpha stabilization window.
