# Alpha Release Priority Update — 2026-04-04 (UTC)

Date: 2026-04-04  
Scope: Backend alpha readiness follow-up after P0 route/test surface expansion

---

## Executive status

- **Priority 1 (build + migration verification):** ❗ **Not complete in this execution environment** (`dotnet` SDK unavailable).
- **Priority 2 (questions hardening):** ✅ Complete.
- **Priority 3 (store/IAP):** ✅ Complete for current backend contract scope.
- **Priority 4 (frontend economy integration):** ⏸️ Frontend-dependent.
- **Priority 5 (crypto layer):** ✅ Complete baseline (wallet, history, prize pool, staking, settlement controls).
- **Priority 6 (polish/gaps):** ✅ Complete for backend baseline scope (search/profile/social/loadout + ML scorer endpoints/fallbacks).
- **6.1 deployment runlist:** ⚠️ Partially complete (tooling is ready; live/runtime execution evidence still needed).

---

## 6.1 Deployment readiness assessment (what remains to fully complete)

### 1) Build + migration gate
**Status:** Blocked in this container.  
**What to run in .NET-capable environment:**
1. `dotnet build Tycoon.sln`
2. `dotnet ef database update --project Tycoon.Backend.Infrastructure --startup-project Tycoon.Backend.Api`
3. Save command output to release notes / execution log.

### 2) Request-level smoke checks (Auth/Questions/Store/Crypto)
**Status:** Script coverage is ready; live execution evidence pending.

Implemented tooling now supports:
- auto-signup bootstrap
- question set/check
- store catalog + iap validate + purchase contract check
- crypto history
- leaderboard read

Run:
- Bash: `BASE_URL=http://<running-api> SMOKE_MODE=live AUTO_SIGNUP=true bash ./scripts/alpha-p0-smoke.sh`
- PowerShell: `pwsh ./scripts/alpha-p0-smoke.ps1 -BaseUrl "http://<running-api>" -SmokeMode live -AutoSignup`

### 3) Strict IAP precheck
**Status:** Backend path complete; keep in go/no-go verification pass.
Validate:
- non-placeholder `Iap:*` values in target environment
- live `/store/iap/validate` request does not return strict-config-missing

### 4) End-to-end player path proof
**Status:** Script path implemented, execution evidence pending in running environment.

Proof required:
- Auth success
- Question set/check success
- Store purchase attempt result (2xx/known contract error)
- Leaderboard read success

### 5) Go/No-Go recording
**Status:** Pending.  
Record explicitly:
- what passed
- what remains deferred (ML churn/quality deployment + settlement worker/monitoring hardening)
- owner + target date for each deferred item

---

## Recommended completion checklist (to close all alpha priorities)

1. Run all 6.1 commands in staging/runtime environment with .NET SDK.
2. Attach logs to `docs/alpha_execution_log_2026-04-04.md` (or new dated execution log).
3. Mark 6.1 tasks done in `docs/synaptix_remaining_work.md` once evidence is archived.
4. Create release decision note (GO or NO-GO) with defer list and follow-up owners.

---

## Defer list proposal (if GO today)

1. ML model ops hardening:
   - scorer calibration against production data
   - alerting/observability for model endpoint failures
2. Withdrawal settlement operational hardening:
   - worker automation
   - monitoring/alerting
   - reconciliation runbook
