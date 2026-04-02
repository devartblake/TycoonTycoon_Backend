# Alpha Release Priority Plan (Tonight)

Date: 2026-04-01 (UTC)
Release target: 2026-04-02 00:00 UTC

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
- Vuetify/layout/scss TODOs in `Tycoon.OperatorDashboard.Vue` (mostly design-system cleanup or upstream issue references).
- Formatter research TODO in `Tycoon.OperatorDashboard.Vue/src/@core/utils/formatters.ts`.

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
