# Backend Surface Inventory — Synaptix BE Packet A (Phase 0)

**Date:** 2026-03-28 | **Last updated:** 2026-04-01
**Scope:** All product-visible strings in the TycoonTycoon_Backend repo

---

## 1. Swagger / OpenAPI Configuration ✅

| File | Line(s) | Current Value | Target Value | Status |
|---|---|---|---|---|
| `Tycoon.Backend.Api/Program.cs` | 152 | ~~`Title = "Tycoon Backend API"`~~ | `"Synaptix API"` | ✅ Done (BE-A2) |
| `Tycoon.Backend.Api/Program.cs` | 154 | ~~`Description = "Trivia Tycoon Game Backend..."`~~ | `"Platform API for Synaptix gameplay, progression, live competition, and player systems."` | ✅ Done (BE-A2) |
| `Tycoon.Backend.Api/Program.cs` | 157 | ~~`Name = "Tycoon Development Team"`~~ | `"Synaptix Development Team"` | ✅ Done (BE-A2) |
| `Tycoon.Backend.Api/Program.cs` | 507 | ~~`"Tycoon Trivia Backend API v1"`~~ | `"Synaptix API v1"` | ✅ Done (BE-A2) |
| `Tycoon.Backend.Api/Program.cs` | 509 | ~~`DocumentTitle = "Tycoon API Documentation"`~~ | `"Synaptix API Documentation"` | ✅ Done (BE-A2) |

---

## 2. Blazor Operator Dashboard (Tycoon.OperatorDashboard) ✅

| File | Line(s) | Current Value | Target Value | Status |
|---|---|---|---|---|
| `Components/App.razor` | 6 | ~~`<title>Tycoon Operator Dashboard</title>`~~ | `<title>Synaptix Command</title>` | ✅ Done (BE-A2) |
| `Components/Layout/MainLayout.razor` | 10 | ~~`Tycoon <span>Ops</span>`~~ | `Synaptix <span>Command</span>` | ✅ Done (BE-A2) |
| `Components/Pages/Dashboard.razor` | 30 | ~~`tycoon-api`~~ | `synaptix-api` | ✅ Done (BE-A2) |
| `Components/Pages/Events.razor` | 44-45 | ~~`Entry Fee (coins)` / `Revive Cost (gems)`~~ | `(Credits)` / `(Synapse Shards)` | ✅ Done (BE-C5) |
| `Components/Pages/Economy.razor` | 297+ | ~~`Coins` / `XP`~~ display labels | `Credits` / `Neural XP` | ✅ Done (BE-C5) |
| `Pages/_Host.cshtml` | 12 | ~~`Tycoon Operator Dashboard`~~ | `Synaptix Command` | ✅ Done (BE-C2) |
| `Pages/Login.cshtml` | 8+ | ~~`Sign In — Tycoon Ops`~~ + branding | `Sign In — Synaptix Command` | ✅ Done (BE-C2) |
| `wwwroot/css/app.css` | 1 | ~~`Tycoon Operator Dashboard`~~ | `Synaptix Command` | ✅ Done (BE-C2) |

---

## 3. Vue Operator Dashboard (Tycoon.OperatorDashboard.Vue) ✅

| File | Line(s) | Current Value | Target Value | Status |
|---|---|---|---|---|
| `index.html` | 8 | ~~`Materio - Vuetify Vuejs Admin Template`~~ | `Synaptix Command` | ✅ Done (BE-A2) |
| `src/layouts/components/DefaultLayoutWithVerticalNav.vue` | 41-43 | ~~`Tycoon Ops`~~ | `Synaptix Command` | ✅ Done (BE-A2) |
| `src/layouts/components/Footer.vue` | 4 | ~~`Tycoon Ops Dashboard`~~ | `Synaptix Command` | ✅ Done (BE-C3) |
| `src/pages/dashboard.vue` | subtitle | ~~`Tycoon Operator Dashboard`~~ | `Synaptix Command Dashboard` | ✅ Done (BE-C3) |
| `src/pages/login.vue` | brand | ~~`Tycoon Ops`~~ | `Synaptix Command` | ✅ Done (BE-C3) |
| `src/pages/economy.vue` | headers | ~~`XP` / `Coins`~~ | `Neural XP` / `Credits` | ✅ Done (BE-C5) |
| `src/pages/users/[id].vue` | 89 | ~~`{ 1: 'XP', 2: 'Coins' }`~~ | `{ 1: 'Neural XP', 2: 'Credits' }` | ✅ Done (BE-C5) |

---

## 4. Web/React Operator Dashboard (Tycoon.OperatorDashboard.Web) ✅

| File | Line(s) | Current Value | Target Value | Status |
|---|---|---|---|---|
| `src/app/layout.tsx` | 14 | ~~`title: 'Tycoon Operator Dashboard'`~~ | `title: 'Synaptix Command'` | ✅ Done (BE-A2) |
| `src/app/layout.tsx` | 15 | ~~`description: '...Tycoon platform'`~~ | `'...Synaptix platform'` | ✅ Done (BE-A2) |
| `src/configs/themeConfig.ts` | 26 | ~~`templateName: 'Tycoon Ops'`~~ | `templateName: 'Synaptix Command'` | ✅ Done (BE-A2) |
| `src/configs/themeConfig.ts` | 27 | `settingsCookieName: 'tycoon-ops-dashboard'` | Keep as-is (persistence key) | ⏸️ Deferred (BE-E) |
| `src/components/layout/vertical/FooterContent.tsx` | 15 | ~~`Tycoon Ops Dashboard`~~ | `Synaptix Command` | ✅ Done (BE-C4) |
| `src/views/economy/EconomyView.tsx` | 40+ | ~~`'XP'` / `'Coins'`~~ | `'Neural XP'` / `'Credits'` | ✅ Done (BE-C5) |
| `src/views/users/UserDetailView.tsx` | 100 | ~~`{ 1: 'XP', 2: 'Coins' }`~~ | `{ 1: 'Neural XP', 2: 'Credits' }` | ✅ Done (BE-C5) |

---

## 5. Backend Code Comments ✅

| File | Line(s) | Current Value | Target Value | Status |
|---|---|---|---|---|
| `Tycoon.Backend.Infrastructure/Persistence/AppDb.cs` | 14 | ~~`Primary EF Core DbContext for Trivia Tycoon`~~ | `Primary EF Core DbContext for Synaptix` | ✅ Done (BE-A2) |

---

## 6. Documentation ✅

| File | Current Value | Target Value | Status |
|---|---|---|---|
| `README.md` line 1 | ~~`# TycoonTycoon Backend`~~ | `# Synaptix Backend` | ✅ Done (BE-A2) |
| `README.md` line 3 | ~~`scalable multiplayer trivia tycoon game infrastructure`~~ | `scalable multiplayer Synaptix platform infrastructure` | ✅ Done (BE-A2) |
| `docs/FLUTTER_INTEGRATION.md` | ~~`Trivia Tycoon Backend`~~ heading | `Synaptix Backend` | ✅ Done (BE-C5) |

---

## 7. DO NOT CHANGE (Risk Register)

These items must remain stable in BE Packet A:

| Item | Location | Reason |
|---|---|---|
| `Tycoon.Backend.*` namespaces | All .cs files | Packet E scope — broad compile churn |
| `Tycoon.OperatorDashboard` project name | .csproj, solution | Packet E scope |
| `Tycoon.Shared.*` namespaces | Shared libraries | Packet E scope |
| `package:trivia_tycoon` | N/A (frontend) | Not in this repo |
| JWT Issuer `TycoonBackendApi` | appsettings.json | Auth contract — breaking change |
| JWT Audience `TycoonFrontendApp` | appsettings.json | Auth contract — breaking change |
| ServiceName `Tycoon.Backend.Api` | Observability config | Telemetry pipeline dependency |
| MongoDB database `tycoon_analytics` | Analytics config | Data pipeline dependency |
| Elasticsearch aliases `tycoon-qa-*` | Analytics writers | Data pipeline dependency |
| gRPC package `tycoon.sidecar` | protos/sidecar.proto | Contract — breaking change |
| HttpClient name `"tycoon-api"` | Dashboard Program.cs | Internal wiring — low visibility |
| Cookie name `tycoon-ops-dashboard` | themeConfig.ts | Browser persistence key |
| Endpoint paths | All API endpoints | Contract — breaking change |
| DTO field names | All DTOs | Contract — breaking change |
| Database table/column names | EF migrations | Schema — breaking change |
| CI/CD pipeline names | Build scripts | Deployment dependency |
| Docker image names | Dockerfiles | Deployment dependency |
