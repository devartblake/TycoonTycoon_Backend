# Synaptix Migration — Remaining Work

**Date:** 2026-04-01
**Purpose:** Summary of completed and remaining work across backend and frontend for review.

---

## Backend Status (TycoonTycoon_Backend)

### Completed

| Packet | Description | Commits |
|---|---|---|
| **BE-A** | Audit + Brand Surface Reframe | `dc0b90f`, `03e4f10` |
| **BE-B** | Profile Support (PlayerPreferences entity + API) | `8ddeaeb` |
| **BE-C** | Product-Language Alignment (all 3 dashboards + docs) | `1a971c9` |
| **BE-C5** | Currency terminology (Coins->Credits, XP->Neural XP, gems->Synapse Shards) | `a168e5c` |
| **BE-D1** | Analytics dimensions (all 5: SynaptixMode, Surface, AudienceSegment, EntryPoint, BrandVersion) | `9080021`, `a168e5c` |
| **BE-D2** | Stabilization QA — no old branding in operator-visible UI | `9080021` |

### Remaining Backend Work

#### Requires Build Environment
- [ ] Verify solution compiles cleanly (`dotnet build`) — no .NET SDK available in current environment
- [ ] Generate EF Core migration for `PlayerPreferences` table (`dotnet ef migrations add AddPlayerPreferences`)
- [ ] Run migration against dev database

#### Awaiting Frontend Status (Cross-Layer)
- [ ] Confirm frontend labels match backend dashboard/docs language
- [ ] Confirm operator surfaces use the same vocabulary as the app
- [ ] Verify no namespace-related build regressions in CI

#### Deferred (BE Packet E — Optional)
- [ ] Deep namespace rename: `Tycoon.Backend.*` -> `Synaptix.Backend.*`
- [ ] Deep namespace rename: `Tycoon.OperatorDashboard.*` -> `Synaptix.OperatorDashboard.*`
- [ ] Deep namespace rename: `Tycoon.Shared.*` -> `Synaptix.Shared.*`
- [ ] Cookie/persistence key rename: `tycoon-ops-dashboard`
- [ ] Docker image name updates
- [ ] CI/CD pipeline name updates
- [ ] JWT issuer/audience name updates
- [ ] Service name and telemetry identifier updates

**Decision gate for BE-E:** Approve only after stable production release of Packets A-D, with rollback strategy documented and build/test coverage confirmed.

---

## Frontend Status (Flutter App)

> **Awaiting status update from frontend development process.**

### FE Packet A — Audit + Brand Surface Reframe
- [ ] FE-A1: Frontend surface inventory
- [ ] FE-A2: Brand surface reframe (logo, splash, main, menu, router)

### FE Packet B — Mode/Theme Foundation + Hub
- [ ] FE-B1: Mode and theme foundation (SynaptixMode enum, provider, theme presets)
- [ ] FE-B2: Shell and navigation upgrade (Synaptix Hub)
- **Backend dependency:** ✅ Ready — `GET/PUT /users/me/preferences` endpoint available

### FE Packet C — Core Feature Surface Rebrand
- [ ] FE-C1: Arena (Leaderboard labels)
- [ ] FE-C2: Labs (Arcade labels)
- [ ] FE-C3: Pathways (Skill Tree labels)
- [ ] FE-C4: Journey (Profile labels)
- [ ] FE-C5: Circles (Social labels)
- [ ] FE-C6: Command (Admin labels)

### FE Packet D — Analytics + Stabilization
- [ ] FE-D1: Analytics instrumentation
- [ ] FE-D2: Stabilization and QA
- **Backend dependency:** ✅ Ready — all 5 analytics dimensions wired in backend

### FE Packet E — Optional Deep Technical Rename
- [ ] Workstream 1: Frontend symbol cleanup (`TriviaTycoonApp` -> `SynaptixApp`)
- [ ] Workstream 2: Package root rename (`package:trivia_tycoon` -> `package:synaptix`)

---

## Backend API Endpoints Available for Frontend

| Endpoint | Method | Purpose | Status |
|---|---|---|---|
| `/users/me/preferences` | `GET` | Read player preferences (returns defaults if none set) | ✅ Available |
| `/users/me/preferences` | `PUT` | Upsert player preferences (partial update) | ✅ Available |
| `/analytics/events` | `POST` | Ingest analytics events with Synaptix dimensions | ✅ Available |

### Preferences API Shape

**GET response / PUT request:**
```json
{
  "synaptixMode": "kids | teen | adult",
  "preferredSurface": "hub | arena | labs | pathways | journey | circles | command",
  "reducedMotion": true | false,
  "tonePreference": "playful | balanced | competitive"
}
```
All fields are optional on PUT — only provided fields are updated.

### Analytics Event Dimensions

Send with `/analytics/events` POST body:
```json
{
  "synaptixMode": "teen",
  "surface": "arena",
  "audienceSegment": "competitive",
  "entryPoint": "hub_card",
  "brandVersion": "1.0"
}
```
All dimension fields are optional and nullable.
