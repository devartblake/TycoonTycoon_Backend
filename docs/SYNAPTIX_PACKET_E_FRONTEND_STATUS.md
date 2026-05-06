# Synaptix — Packet E Status & Frontend Team Confirmation Request

**Document type:** Cross-team status briefing + confirmation request
**Date:** 2026-05-03
**Prepared by:** Backend team
**Audience:** Frontend team (Flutter)
**Companion doc:** `docs/synaptix_packet_e_detailed.md`

---

## Purpose

This document asks the frontend team to confirm the current Packet E status on the Flutter side, and summarises exactly what the backend has completed, what remains deferred, and the one area where backend changes **directly affect frontend configuration** and require frontend sign-off.

---

## 1. Where We Are — Packets A–D (Both Sides)

Before discussing Packet E, shared context:

| Packet | Backend | Frontend |
|---|---|---|
| A — Branding + surface reframe | ✅ Complete | ✅ Complete |
| B — Mode/theme + preferences API | ✅ Complete | ✅ Complete |
| C — Feature surface language (Arena / Labs / Pathways / Circles / Command) | ✅ Complete | ✅ Complete |
| D — Analytics dimensions + stabilisation | ✅ Complete | ✅ Complete |

Both sides of the product rebrand are done. Packet E is the only remaining open question.

---

## 2. What Packet E Covers — Frontend Side

Packet E on the Flutter app has two workstreams. Neither has been started (both were explicitly deferred per the original plan).

### FE-E Workstream 1 — Symbol Cleanup (Low Risk)

These are internal code renames with no impact on app stores, native config, or backend contracts.

| Item | Current | Target | Risk |
|---|---|---|---|
| Root app class | `TriviaTycoonApp` | `SynaptixApp` | Low — one class rename + references |
| Internal helper names tied to old branding | Various `trivia_tycoon_*` or `tycoon_*` symbols | Synaptix equivalents | Low — scoped to files |
| `// TODO(Synaptix Phase 8)` comments | Sprinkled at deferral points | Resolve or remove | Trivial |
| README + inline comments | References to Trivia Tycoon | Synaptix | Trivial |
| `pubspec.yaml` description field | Old product description | Synaptix platform description | Trivial |

**Estimated scope:** A few hours to a day. No store submission required. No backend impact.

---

### FE-E Workstream 2 — Package Root Rename (High Risk)

This is the highest-blast change in the entire project. Every Dart file that imports from the app changes.

| Item | Current | Target | Risk |
|---|---|---|---|
| `pubspec.yaml` package name | `trivia_tycoon` | `synaptix` | **High** — all imports cascade |
| All `package:trivia_tycoon/...` import paths | Across every `.dart` file in `lib/`, `test/`, generated files | `package:synaptix/...` | **High** — hundreds of files |
| `build_runner` generated files | Tied to package name | Need full regeneration | **Medium** |
| Android application ID | `com.tycoon.app` *(confirm current value)* | `com.synaptix.app` | **High** — Play Store implications |
| iOS bundle identifier | `com.tycoon.app` *(confirm current value)* | `com.synaptix.app` | **High** — App Store implications |
| Firebase `google-services.json` / `GoogleService-Info.plist` | Tied to current bundle ID | Requires new Firebase app registration | **High** |
| Google Analytics / Crashlytics config | Package-scoped | Needs update to match new ID | **Medium** |

> **Store submission note:** Changing an Android application ID or iOS bundle identifier on an app with existing users is not a simple rename. It is treated by the stores as a **new app**. Purchase history, subscriptions, ratings, and install base are tied to the old ID. This requires a coordinated plan with product/legal before executing.

---

## 3. What the Backend Has Already Done (Packet E-Adjacent)

During the Packet A–D completion pass on **2026-05-03**, the backend updated several identifiers that are technically Packet E-scope but were low-risk enough to ship as brand-surface fixes. The frontend team should be aware of these because **one of them directly affects auth token validation**.

### ✅ JWT Issuer and Audience — already changed

| Setting | Old value | New value | Where used |
|---|---|---|---|
| `JwtSettings:Issuer` | `TycoonBackendApi` | `SynaptixApi` | Embedded in every JWT the backend issues |
| `JwtSettings:Audience` | `TycoonFrontendApp` | `SynaptixApp` | Validated on every authenticated API request |
| `Authentication:Bearer:ValidAudiences` | `TycoonClient` | `SynaptixApp` | `dotnet user-jwts` dev tool validation |

**Action required from frontend team:**

The backend now issues tokens with `iss: SynaptixApi` and `aud: SynaptixApp`. If the Flutter app validates these claims locally (e.g. using `dart_jsonwebtoken` or similar), or if the app passes expected issuer/audience values in any API call or config file, those values must be updated to match:

```
Issuer:   SynaptixApi
Audience: SynaptixApp
```

Please confirm:
- [ ] Does the Flutter app validate JWT `iss` or `aud` claims locally?
- [ ] Are `TycoonBackendApi`, `TycoonFrontendApp`, or `TycoonClient` hardcoded anywhere in the Flutter app's auth or config layer?
- [ ] Does any environment config file (`.env`, `app_config.dart`, `flavors/`) reference the old issuer or audience values?

---

### ✅ PayPal BrandName — already changed

`PayPal:BrandName` is now `"Synaptix"`. This affects the label shown in PayPal checkout flows. No Flutter action required unless the app renders this value directly from a backend config endpoint.

---

### Backend items still deferred (not changed yet)

These are still on the Packet E backlog and **have not been changed**:

| Item | Current (unchanged) | Target (future) |
|---|---|---|
| Backend namespace | `Tycoon.Backend.*` | `Synaptix.Backend.*` |
| Docker container names | `tycoon_*` | `synaptix_*` |
| Elasticsearch aliases | `tycoon-qa-*` | `synaptix-qa-*` |
| `Observability:ServiceName` | `Tycoon.Backend.Api` | `Synaptix.Backend.Api` |
| IAP `GooglePackageName` config | `com.tycoon.app.dev` | `com.synaptix.app.dev` |
| CI/CD pipeline labels | `tycoon-*` | `synaptix-*` |

No Flutter impact from any of these until the IAP package name changes (which requires a coordinated store decision).

---

## 4. Confirmation Requested from Frontend Team

Please review each item below and respond with current status.

### 4.1 Symbol Cleanup (FE-E Workstream 1)

| Item | Status — please confirm |
|---|---|
| Has `TriviaTycoonApp` been renamed to `SynaptixApp`? | ☐ Done &nbsp;&nbsp; ☐ Not started &nbsp;&nbsp; ☐ In progress |
| Have internal `trivia_tycoon_*` / `tycoon_*` symbol names been cleaned up? | ☐ Done &nbsp;&nbsp; ☐ Not started &nbsp;&nbsp; ☐ Partial |
| Are all `// TODO(Synaptix Phase 8)` comments resolved or reviewed? | ☐ Done &nbsp;&nbsp; ☐ Not started &nbsp;&nbsp; ☐ N/A |
| Has `pubspec.yaml` description been updated? | ☐ Done &nbsp;&nbsp; ☐ Not started |

### 4.2 Package Root Rename (FE-E Workstream 2)

| Item | Status — please confirm |
|---|---|
| Has `pubspec.yaml` `name:` been changed from `trivia_tycoon` to `synaptix`? | ☐ Done &nbsp;&nbsp; ☐ Not started &nbsp;&nbsp; ☐ Planned |
| Have all `package:trivia_tycoon/...` imports been updated? | ☐ Done &nbsp;&nbsp; ☐ Not started |
| What is the current Android application ID? | `______________________` |
| What is the current iOS bundle identifier? | `______________________` |
| Has a store transition plan been agreed with product/legal for the bundle ID change? | ☐ Yes &nbsp;&nbsp; ☐ No &nbsp;&nbsp; ☐ Not applicable yet |

### 4.3 JWT Config (action required — backend already changed)

| Item | Status — please confirm |
|---|---|
| Does the Flutter app validate `iss` or `aud` JWT claims locally? | ☐ Yes &nbsp;&nbsp; ☐ No |
| Does any config file reference `TycoonBackendApi`, `TycoonFrontendApp`, or `TycoonClient`? | ☐ Yes (needs update) &nbsp;&nbsp; ☐ No |
| Flutter auth layer confirmed compatible with `iss: SynaptixApi` / `aud: SynaptixApp`? | ☐ Confirmed &nbsp;&nbsp; ☐ Needs check &nbsp;&nbsp; ☐ Unknown |

### 4.4 Decision: Proceed with FE-E or Hold?

Per the original plan, Packet E only proceeds if the product layer is stable and the team formally approves it. Please indicate the frontend team's current position:

| Question | Response |
|---|---|
| Is Packet D fully stable in the Flutter app? | ☐ Yes &nbsp;&nbsp; ☐ Not yet |
| Has the internal soft launch validation been run? | ☐ Yes &nbsp;&nbsp; ☐ Not yet |
| Does the frontend team want to proceed with FE-E Workstream 1 (symbol cleanup)? | ☐ Proceed &nbsp;&nbsp; ☐ Hold &nbsp;&nbsp; ☐ Needs discussion |
| Does the frontend team want to proceed with FE-E Workstream 2 (package root rename)? | ☐ Proceed &nbsp;&nbsp; ☐ Hold &nbsp;&nbsp; ☐ Needs discussion |

---

## 5. Recommended Sequencing (if Packet E is approved)

If the frontend team decides to proceed, the backend recommendation is:

```
Step 1:  FE-E Workstream 1 (symbol cleanup) — safe to start anytime
Step 2:  JWT config confirmation (see Section 4.3) — unblock first
Step 3:  Agree bundle ID strategy with product/legal
Step 4:  FE-E Workstream 2 (package root rename) — as a dedicated sprint
Step 5:  BE-E backend namespace rename — coordinate timing with frontend
Step 6:  Ops/telemetry/Elasticsearch rename — backend-led, last
```

Workstreams 1 and 2 are **independent** — Workstream 1 does not need to wait for any store decisions.

---

## 6. Hard Dependencies Between Frontend and Backend Packet E

Most of Packet E can be executed independently on each side. The only hard cross-team dependencies are:

| Dependency | Detail |
|---|---|
| **JWT issuer/audience** | Backend has already changed to `SynaptixApi`/`SynaptixApp`. Frontend must confirm no local validation break. |
| **IAP package name** | Backend `Iap:GooglePackageName` config and the Flutter Android application ID must change together in the same release. Requires a store plan first. |
| **Timing coordination** | If both sides rename their namespaces/packages simultaneously, a coordinated release window is needed to avoid integration test gaps. |

---

## 7. What Does Not Change in Packet E

To set expectations clearly — the following are **explicitly out of scope** for Packet E and will not change:

| Item | Reason |
|---|---|
| API route paths (`/users/me`, `/leaderboard`, etc.) | Would break all existing clients |
| DTO / JSON field names | Would break serialisation contracts |
| Database table names | Would require a full data migration |
| EF Core migration history identifiers | Must remain stable |
| Persistence / Hive key names (Flutter) | Would lose existing user data without a migration layer |
| Historical analytics event schema | Would break dashboard queries |
| Public API contracts | Would break any third-party or external integrations |

---

## 8. Summary

| Area | Backend status | Frontend status |
|---|---|---|
| FE-E Workstream 1 — symbol cleanup | N/A | ⚠️ Awaiting confirmation |
| FE-E Workstream 2 — package root rename | N/A | ⚠️ Awaiting confirmation |
| BE-E — namespace rename | ⏸️ Deferred | N/A |
| JWT issuer/audience | ✅ Already updated to Synaptix values | ⚠️ **Needs frontend confirmation** |
| IAP bundle ID | ⏸️ Deferred — needs store plan | ⏸️ Deferred — needs store plan |
| Elasticsearch alias rename | ⏸️ Deferred | N/A |
| Ops/telemetry identifiers | ⏸️ Deferred | N/A |

**The one item requiring immediate attention** is the JWT issuer/audience change — the backend is already live with new values and the frontend team should verify there is no auth regression before the next integration test pass.

Everything else in Packet E is a deliberate deferral and can be scheduled as a standalone modernisation sprint when the team is ready.

---

*Questions or corrections? Raise against this document or contact the backend lead directly.*
