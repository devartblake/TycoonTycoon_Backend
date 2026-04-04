# Synaptix Migration — Remaining Work

**Date:** 2026-04-01 | **Last updated:** 2026-04-04
**Purpose:** Consolidated status across backend and frontend, incorporating cross-comparison findings.

---

## 1. Backend Rebrand Status (Packets A–D) ✅ COMPLETE

| Packet | Description | Status |
|---|---|---|
| **BE-A** | Audit + Brand Surface Reframe | ✅ Complete |
| **BE-B** | Profile Support (PlayerPreferences entity + API) | ✅ Complete |
| **BE-C** | Product-Language Alignment (all 3 dashboards + docs + currency terms) | ✅ Complete |
| **BE-D** | Analytics dimensions (all 5) + Stabilization QA | ✅ Complete |
| **BE-E** | Deep namespace rename | ⏸️ Deferred |

---

## 2. Frontend Rebrand Status (Packets A–D) ✅ COMPLETE

Source: `synaptix_frontend_cross_comparison_status.md`

| Packet | Description | Status |
|---|---|---|
| **FE-A** | Branding (splash, logo, first-touch surfaces) | ✅ Complete |
| **FE-B** | Mode/Theme Foundation + Synaptix Hub | ✅ Complete |
| **FE-C** | Core Feature Surface Rebrand (Arena/Labs/Pathways/Journey/Circles/Command) | ✅ Complete |
| **FE-D** | Analytics + Stabilization (onboarding evolution, Hub polish) | ✅ Complete |
| **FE-E** | Deep technical rename (`TriviaTycoonApp`, package root) | ⏸️ Deferred |

---

## 3. Cross-Layer Verification (Rebrand)

| Check | Status |
|---|---|
| Backend dashboards say "Synaptix Command" | ✅ |
| Swagger says "Synaptix API" | ✅ |
| No "Trivia Tycoon" in operator-visible paths | ✅ |
| Backend currency labels: Credits / Neural XP / Synapse Shards | ✅ |
| Frontend labels match backend terminology | ⚠️ Needs runtime verification |
| No API contract breaks | ✅ |
| Analytics dimensions match (5/5) | ✅ |
| Preferences persistence aligned | ✅ |

---

## 4. Backend Gameplay API Status (Beyond Rebrand)

Source: Full API survey + `synaptix_backend_cross_comparison_status.md` Section 6

| Area | Status | Routes | Notes |
|---|---|---|---|
| **Auth** | ✅ Production | 5 | Register, login, refresh, logout |
| **Profile/Users** | ✅ Alpha+ | 5 | CRUD + preferences; no search/avatar |
| **Leaderboards** | ✅ Production | 6 | Tier-based, ranked, paginated |
| **Economy/Wallet** | ✅ Production | 11 | Authoritative wallet, 3 currencies, safeguards |
| **Missions** | ✅ Production | 4 | Progress tracking, claims, SignalR |
| **Seasons/Tiers** | ✅ Production | 3 | Season state, tier tracking |
| **Skills/Pathways** | ✅ Production | 4 | Unlock, respec |
| **Friends/Social** | ✅ Production | 5 | Requests, accept/decline |
| **Party** | ✅ Production | 8 | Create, invite, group matchmaking |
| **Matchmaking** | ✅ Production | 3 | Enqueue, cancel, status |
| **Matches** | ✅ Production | 4 | Start, submit, retrieve |
| **Guardians** | ✅ Production | 2 | Boss battles |
| **Territory** | ✅ Production | 3 | Tile control, duel |
| **Game Events** | ✅ Production | 4 | Jackpot/Crown, enter, revive |
| **Votes/Powerups/Referrals/QR** | ✅ Production | 10 | All functional |
| **Real-time (SignalR)** | ✅ Production | 3 hubs | Match, notifications, presence |
| **Sidecar (ML/Utils)** | ✅ Utils | 20+ | Analytics, rebalance, placeholder ML models |
| **Questions** | ✅ Production | 3 | `/questions/set`, `/questions/check`, `/questions/check-batch` serve + grade questions |
| **Store/IAP** | ⚠️ Partial | 4 | Catalog + purchase + `/store/iap/validate`; strict provider validation still optional via config |
| **Crypto Economy** | ⚠️ Partial | 4 | `/crypto/link-wallet`, `/crypto/balance/{playerId}`, `/crypto/history/{playerId}`, `/crypto/withdraw` |

---

## 5. Remaining Work — Priority Order

### Priority 1: Build & Migration Verification
- [ ] Verify solution compiles cleanly (`dotnet build` on `Tycoon.Backend.Api/Tycoon.Backend.Api.csproj`)
- [ ] Generate EF Core migration for `PlayerPreferences` table
- [ ] Run migration against dev database
- [ ] Confirm CI passes with no namespace/build regressions

### Priority 2: Questions Gameplay Hardening
- [x] `GET /questions/set?category=&difficulty=&count=` — Serve questions for match play
- [x] `POST /questions/check` and `POST /questions/check-batch` — Server-side grading
- [ ] Integrate questions flow into authoritative match/session pipeline end-to-end
- [ ] Question bank management (categories, difficulty tagging, approval workflow)
- [ ] Wire Python sidecar `/ml/question-difficulty` for NLP-based difficulty estimation

### Priority 3: Store/Shop/IAP
- [x] `GET /store/catalog` — Fetch available items/bundles
- [x] `POST /store/purchase` — Purchase with in-game currency (Credits/Synapse Shards)
- [x] `POST /store/iap/validate` — Receipt validation endpoint + transaction tracking
- [ ] Strict Apple/Google provider-side verification (enable with `Iap:EnableStrictValidation`)
- [x] Player inventory/cosmetics endpoint
- [ ] Battle pass / premium subscription support (if planned)

### Priority 4: Frontend Economy Integration
- [ ] Frontend wallet sync against authoritative backend state (economy endpoints exist)
- [ ] Frontend reward reconciliation via backend economy transactions
- [ ] Frontend purchase flow wired to store API (once built)

### Priority 5: Crypto Economy Layer
- [x] Crypto ledger entries via `PlayerTransaction` (`crypto-*` kinds)
- [x] Wallet linking API (external wallet address)
- [x] Crypto balance/history endpoints
- [x] Withdrawal request flow (pending, approval/audit ready)
- [ ] Prize pool system
- [ ] Optional staking (later phase)

### Priority 6: Polish & Gaps
- [x] Player search/discovery endpoint (`GET /users/search?handle=`)
- [x] Profile enrichment (career stats summary, W-L, winrate)
- [x] Unfriend endpoint
- [ ] Cosmetics/avatar loadout system
- [ ] ML model deployment (replace placeholder churn/difficulty/quality scorers)
- [x] Added backend smoke route contract integration tests (`Tycoon.Backend.Api.Tests/Smoke/AlphaP0RouteContractsTests.cs`)
  - validates core P0 route mapping (non-404 contract checks)
  - validates sensitive anonymous POSTs avoid 500 regression

### Priority 7: Frontend Polish (No Backend Dependency)
- [ ] Retention hooks (bonus challenge, streak system, session-end trigger)
- [ ] Sound cue layer
- [ ] Final empty-state sweep and copy consistency
- [ ] Mode-specific accessibility pass
- [ ] Release-level QA on all core screens

### Deferred: BE Packet E + FE Packet E
- [ ] Backend namespace rename (`Tycoon.*` → `Synaptix.*`)
- [ ] Frontend package rename (`package:trivia_tycoon` → `package:synaptix`)
- [ ] Cookie/persistence key cleanup
- [ ] Docker/CI/JWT/telemetry identifier updates

---

## 6. Deployment Readiness Assessment

| Milestone | Status | Blockers |
|---|---|---|
| **Closed Beta / Soft Launch** | ✅ Ready | Core gameplay loop functional (auth → match → rewards → leaderboard) |
| **Public Production** | ⚠️ Blocked | Strict external IAP verification + withdrawal settlement pipeline still need hardening |
| **Monetization** | ⚠️ Partial | Store + IAP endpoint + crypto request flow exist; settlement/prize pool still open |

---

## 6.1 Immediate next steps (owner runlist)

1. [ ] Run local build + migration gate (`dotnet build`, `dotnet ef database update`).
2. [ ] Perform request-level smoke checks for Auth, Questions, Store, and Crypto routes.
   - [x] Route/static smoke executed via helper scripts (`SMOKE_MODE=routes`).
   - [ ] Live/request-level smoke (`SMOKE_MODE=live`) against running API instance.
   - Helper (bash): `./scripts/alpha-p0-smoke.sh`
   - Helper (PowerShell): `pwsh ./scripts/alpha-p0-smoke.ps1`
   - CI helper: `.github/workflows/alpha-p0-smoke.yml` (NOW build + route checks)
3. [ ] Replace strict IAP placeholders in Development config and verify `/store/iap/validate` no longer returns `IAP_STRICT_CONFIG_MISSING`.
4. [ ] Validate one full player path end-to-end (login -> question set/check -> purchase -> leaderboard view).
5. [ ] Record go/no-go with explicit defer list (prize pool, staking, strict provider hardening follow-ups if needed).

---

## 7. Backend API Reference (for Frontend Integration)

### Synaptix-Specific Endpoints

| Endpoint | Method | Purpose |
|---|---|---|
| `/users/me/preferences` | `GET` | Read player preferences (defaults if none set) |
| `/users/me/preferences` | `PUT` | Upsert preferences (partial update, validated) |
| `/analytics/events` | `POST` | Ingest analytics events with Synaptix dimensions |

### Preferences Shape
```json
{
  "synaptixMode": "kids | teen | adult",
  "preferredSurface": "hub | arena | labs | pathways | journey | circles | command",
  "reducedMotion": true | false,
  "tonePreference": "playful | balanced | competitive"
}
```

### Analytics Dimensions
```json
{
  "synaptixMode": "teen",
  "surface": "arena",
  "audienceSegment": "competitive",
  "entryPoint": "hub_card",
  "brandVersion": "1.0"
}
```
