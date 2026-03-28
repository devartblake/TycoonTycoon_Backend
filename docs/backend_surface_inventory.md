# Backend Surface Inventory — Synaptix BE Packet A (Phase 0)

**Date:** 2026-03-28
**Scope:** All product-visible strings in the TycoonTycoon_Backend repo

---

## 1. Swagger / OpenAPI Configuration

| File | Line(s) | Current Value | Target Value | Risk |
|---|---|---|---|---|
| `Tycoon.Backend.Api/Program.cs` | 152 | `Title = "Tycoon Backend API"` | `"Synaptix API"` | Low — display only |
| `Tycoon.Backend.Api/Program.cs` | 154 | `Description = "Trivia Tycoon Game Backend - Multiplayer Quiz Game API"` | `"Platform API for Synaptix gameplay, progression, live competition, and player systems."` | Low — display only |
| `Tycoon.Backend.Api/Program.cs` | 157 | `Name = "Tycoon Development Team"` | `"Synaptix Development Team"` | Low — display only |
| `Tycoon.Backend.Api/Program.cs` | 507 | `"Tycoon Trivia Backend API v1"` (SwaggerUI endpoint label) | `"Synaptix API v1"` | Low — display only |
| `Tycoon.Backend.Api/Program.cs` | 509 | `DocumentTitle = "Tycoon API Documentation"` | `"Synaptix API Documentation"` | Low — display only |

---

## 2. Blazor Operator Dashboard (Tycoon.OperatorDashboard)

| File | Line(s) | Current Value | Target Value | Risk |
|---|---|---|---|---|
| `Components/App.razor` | 6 | `<title>Tycoon Operator Dashboard</title>` | `<title>Synaptix Command</title>` | Low |
| `Components/Layout/MainLayout.razor` | 10 | `<div class="brand">Tycoon <span>Ops</span></div>` | `<div class="brand">Synaptix <span>Command</span></div>` | Low |
| `Components/Pages/Dashboard.razor` | 30 | `Verify the <code>tycoon-api</code> container` | `Verify the <code>synaptix-api</code> container` | Low — operator copy only |

---

## 3. Vue Operator Dashboard (Tycoon.OperatorDashboard.Vue)

| File | Line(s) | Current Value | Target Value | Risk |
|---|---|---|---|---|
| `index.html` | 8 | `<title>Materio - Vuetify Vuejs Admin Template</title>` | `<title>Synaptix Command</title>` | Low |
| `src/layouts/components/DefaultLayoutWithVerticalNav.vue` | 41-43 | `Tycoon Ops` | `Synaptix Command` | Low |

---

## 4. Web/React Operator Dashboard (Tycoon.OperatorDashboard.Web)

| File | Line(s) | Current Value | Target Value | Risk |
|---|---|---|---|---|
| `src/app/layout.tsx` | 14 | `title: 'Tycoon Operator Dashboard'` | `title: 'Synaptix Command'` | Low |
| `src/app/layout.tsx` | 15 | `description: 'Admin dashboard for managing the Tycoon platform'` | `description: 'Admin dashboard for managing the Synaptix platform'` | Low |
| `src/configs/themeConfig.ts` | 26 | `templateName: 'Tycoon Ops'` | `templateName: 'Synaptix Command'` | Low |
| `src/configs/themeConfig.ts` | 27 | `settingsCookieName: 'tycoon-ops-dashboard'` | Keep as-is (persistence key) | N/A — deferred |

---

## 5. Backend Code Comments

| File | Line(s) | Current Value | Target Value | Risk |
|---|---|---|---|---|
| `Tycoon.Backend.Infrastructure/Persistence/AppDb.cs` | 14 | XML comment: `Primary EF Core DbContext for Trivia Tycoon` | `Primary EF Core DbContext for Synaptix` | Low — comment only |

---

## 6. Documentation

| File | Current Value | Target Value | Risk |
|---|---|---|---|
| `README.md` line 1 | `# TycoonTycoon Backend` | `# Synaptix Backend` | Low |
| `README.md` line 3 | `scalable multiplayer trivia tycoon game infrastructure` | `scalable multiplayer Synaptix platform infrastructure` | Low |

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
