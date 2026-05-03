Synaptix Internal Soft Launch Build Plan
Controlled internal rollout package

---

## Status — 2026-05-03

**Build contents checklist against soft launch readiness:**

| Item | Status |
|---|---|
| Phase 1 — visible Synaptix branding (Swagger, dashboards, currency labels) | ✅ Complete |
| Phase 2 — mode/theme foundation (PlayerPreferences, synaptixMode, surface) | ✅ Complete |
| Phase 3 — Synaptix Hub (frontend, separate repo) | ✅ Complete |
| Phase 4 — feature surface rebrand (Arena / Labs / Pathways / Circles / Command) | ✅ Complete |
| Phase 5 — backend-visible language alignment (tiers, missions, JWT, PayPal) | ✅ Complete *(2026-05-03)* |
| Phase 6 — analytics instrumentation (5 Synaptix dimensions) | ✅ Complete |
| Phase 7 — regression pass | ⚠️ Pending live build verification |
| Synaptix Security KMS subsystem | ✅ Complete *(2026-05-02)* |

**Go/no-go blockers remaining:**
- [ ] `dotnet build` clean compile verification
- [ ] `dotnet run --project Tycoon.MigrationService` — applies tier renames + mission seeds to live database
- [ ] Smoke suite execution against running API (`./scripts/alpha-p0-smoke.sh`)
- [ ] Frontend ↔ backend terminology spot-check at runtime

**Packet E items confirmed excluded from this build** (per plan): namespace renames, Elasticsearch alias renames, IAP package-id changes.

---

Constraint note:
This package defines the internal soft launch build plan, validation checklist, release profile, and acceptance criteria. It does not generate a signed production binary from this environment. To produce an actual internal build artifact, the implementation team should run the normal Flutter and backend build pipelines in the project workspace after applying the packet changes.

Soft launch goal:
Validate that Synaptix works coherently as an internal build across:
- branding
- mode-aware UX
- Hub, Arena, Labs, Pathways, Journey, Circles, and Command framing
- analytics events
- regression stability
- onboarding and profile persistence

Internal release target:
Synaptix Internal Soft Launch v0.9.0-internal

Recommended channels:
- internal QA
- design review
- product review
- operator/admin review
- selected engineering stakeholders only

Do not use this internal soft launch for:
- public beta
- external customers
- store submission
- production marketing

Build contents checklist:
- Phase 1 visible Synaptix branding
- Phase 2 mode/theme foundation
- Phase 3 Synaptix Hub
- Phase 4 feature surface rebrand
- Phase 5 backend-visible language alignment
- Phase 6 analytics instrumentation
- Phase 7 regression pass results

Do not include:
- Packet E technical rename work
- unfinished route refactors
- half-migrated namespaces
- partially renamed backend projects

Internal validation matrix:
Core player-facing flows:
- first launch
- splash
- onboarding
- age group to mode mapping
- Hub landing
- Arena launch
- Labs launch
- Pathways launch
- Journey launch
- Circles launch

Admin and operator flows:
- Command shell entry
- Swagger visibility
- dashboard terminology
- analytics visibility for Synaptix surfaces

Stability flows:
- relaunch app
- restore saved profile
- navigate between multiple surfaces
- validate offline and online fallback where relevant
- verify no mixed Trivia Tycoon and Synaptix strings remain in high-visibility paths

Acceptance criteria:
1. the app clearly reads as Synaptix
2. modes render coherently enough to judge kids, teen, and adult differences
3. Hub works as the new product shell
4. Arena, Labs, Pathways, Journey, Circles, and Command are understandable
5. analytics events fire for the new surfaces
6. no critical regressions exist in routing, auth/bootstrap, or persistence
7. admin and operator-facing language does not contradict the frontend rebrand

Recommended internal review participants:
- product owner
- design reviewer
- frontend lead
- backend lead
- admin/operator stakeholder
- QA reviewer

Review form fields:
- brand clarity
- shell clarity
- mode clarity
- feature naming clarity
- admin alignment
- analytics visibility
- blocker bugs
- release confidence

Suggested rating scale:
1 = unacceptable
2 = weak
3 = acceptable
4 = strong
5 = ready

Go decision if:
- no blocker bugs
- no severe terminology inconsistencies
- all critical surfaces launch
- analytics visible enough for validation
- internal reviewers understand the Synaptix product framing

No-go decision if:
- the Hub still feels placeholder
- naming is inconsistent across adjacent screens
- mode experience is broken or confusing
- analytics is missing for major surfaces
- admin/operator surfaces are materially misaligned

Recommended next step after internal soft launch:
If internal soft launch passes:
- move to polish and system tuning in sequential order
- deepen UI polish
- refine onboarding and first-session flow
- refine monetization and progression language
- keep Packet E deferred unless clearly justified

If internal soft launch fails:
- fix Packet D issues
- rerun internal build
- keep Packet E deferred