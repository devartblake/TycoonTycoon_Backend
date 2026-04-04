# Synaptix Frontend Cross-Comparison Status
## Completed work, dependency map, and remaining work

**Purpose:** summarize the current frontend status, split into work that is independent of the backend vs. work that depends on backend readiness or backend verification.

---

## 1. Snapshot

The frontend is substantially ahead of the backend in visible product migration.  
Most core Synaptix-facing Flutter work is marked complete:

- Packet A — Branding
- Packet B — Mode + Hub
- Packet C — Core feature surfaces
- Packet D — Analytics + stabilization
- Onboarding evolution to 11-step Synaptix flow
- Premium Hub polish pass
- Local wallet persistence for coins/gems

What remains is mostly:
- backend-dependent monetization and economy sync
- backend-dependent cross-layer validation
- a few product-layer retention and polish features

---

## 2. Frontend completed work that does **not** depend on backend changes

These items are self-contained in the Flutter app and can function or render correctly without new backend work.

### 2.1 Packet A — visible brand reframe
- Synaptix branding applied to app shell
- splash / logo / first-touch surfaces rebranded
- old product naming removed from primary UI entry points

### 2.2 Packet B — mode/theme foundation + Synaptix Hub
- `SynaptixMode` foundation added
- mode-aware Hub architecture implemented
- Hub shell established as the new product home
- mode-aware UI differences for kids / teen / adult introduced

### 2.3 Packet C — core feature surface rebrand
- Arena
- Labs
- Pathways
- Journey
- Circles
- Command

These are display-level and UX-layer conversions, not backend contract changes.

### 2.4 Onboarding evolution
- onboarding expanded from 7 steps to 11 steps
- intent, play style, first challenge, and reward reveal steps added
- onboarding remains on existing controller/persistence architecture
- age group -> Synaptix mode mapping integrated
- preferred home surface is derived from onboarding intent

### 2.5 First-session gameplay and reward reveal
- local first-session challenge exists
- reward reveal exists before Hub handoff
- deterministic starter XP / coin flow is defined in onboarding
- onboarding can complete without introducing a full backend quiz dependency into the first pass

### 2.6 UI polish already completed or partially completed
- premium dark Hub redesign
- glassmorphic / neon-style card language
- pulse motion and featured match presentation
- progress and CTA animation patterns
- haptics partially wired
- visual pulse feedback present

### 2.7 Local economy foundation
- coins/gems wallet persistence exists locally via Hive
- this is sufficient for frontend display and internal prototype flows
- backend sync is still outstanding

---

## 3. Frontend completed work that **does** depend on backend support and is already aligned

These items require backend readiness or backend agreement, and the backend docs indicate the supporting work is already in place.

### 3.1 Mode/theme preference persistence
Frontend dependency:
- `synaptixMode`
- `preferredHomeSurface`
- `reducedMotion`
- `tonePreference`

Backend support status:
- backend has additive preferences support via `GET /users/me/preferences`

### 3.2 Analytics dimensions
Frontend dependency:
- `synaptix_mode`
- `surface`
- `entry_point`
- `audience_segment`
- `brand_version`

Backend support status:
- backend analytics dimensions are documented as complete and aligned

### 3.3 Product vocabulary alignment
Frontend dependency:
- Arena / Labs / Pathways / Journey / Circles / Command
- Credits / Neural XP / Synapse Shards terminology consistency

Backend support status:
- backend dashboards/docs were updated to Synaptix-facing naming
- currency terminology alignment was also completed in backend-visible surfaces

---

## 4. Frontend completed work that is **implemented**, but still needs runtime validation or cross-stack verification

These items appear complete in the planning/status docs but still need runtime or release validation.

### 4.1 Onboarding runtime validation
Still needs explicit runtime verification for:
- persistence restore
- first challenge completion path
- reward reveal handoff
- completion -> `/home` handoff

### 4.2 Full QA pass across all modes
Needs confirmation in real app runs for:
- kids mode
- teen mode
- adult mode
- Hub layout and copy behavior in each mode

### 4.3 Frontend/backend vocabulary match verification
The backend docs say the backend side is ready, but a final cross-layer pass is still needed to confirm:
- frontend labels match operator dashboards/docs
- no mixed terminology remains between app and backend surfaces

---

## 5. Remaining frontend work with **no backend dependency**

These items can be completed entirely in Flutter.

### 5.1 Retention hook after first session
Not yet implemented:
- bonus challenge prompt
- streak system
- session-end return trigger

### 5.2 Sound cue layer
UI polish is only partially complete because:
- haptics are present
- motion is present
- visual pulses are present
- sound cues are still missing

### 5.3 Additional polish / release hardening
Still useful before beta:
- final empty-state sweep
- edge-case copy consistency
- final mode-specific accessibility pass
- stronger release-level QA on all core screens

### 5.4 Optional Packet E frontend cleanup
Deferred:
- `TriviaTycoonApp` -> `SynaptixApp`
- `package:trivia_tycoon/...` rename
- internal symbol cleanup

---

## 6. Remaining frontend work that **does depend** on backend work

These are the main true frontend/backend dependency gaps still open.

### 6.1 Full monetization backend integration
Not started end-to-end:
- remote wallet synchronization
- authoritative backend economy state
- reward reconciliation
- live grant/claim loops
- shared purchase outcomes

### 6.2 Crypto economy integration
Not started:
- crypto rewards
- prize pool visibility
- crypto balance/history UI
- withdrawal / wallet-linking UX

### 6.3 Backend-driven onboarding rewards (optional later)
Current onboarding rewards are deterministic/local-first.
Still open if desired:
- server-issued starter rewards
- reward claim auditing
- anti-abuse protections for onboarding grants

### 6.4 Full API-backed feature completion
The frontend plan shows the Synaptix rebrand is complete at UI level, but backend delivery is still needed for:
- full store behavior
- authoritative economy state
- seasons/tiers backend progression
- friends/social deeper features
- live multiplayer / matchmaking
- skills/pathways API if the product moves off local/static definitions

---

## 7. Practical frontend priority order from here

### Highest-value remaining frontend items
1. retention hook after onboarding (bonus challenge + streak)
2. sound cue layer
3. real runtime QA of onboarding restore/handoff
4. cross-stack label verification with backend
5. economy UI integration against live backend state
6. crypto UX only after the backend economy layer exists

---

## 8. Final frontend assessment

### Completed and mostly independent
The frontend product-layer migration is largely complete.

### Completed but cross-stack dependent
Preferences and analytics alignment appear ready, pending final verification.

### Still open
The biggest unfinished frontend-adjacent work is not naming or shell work anymore — it is:
- retention
- real backend economy integration
- crypto UX
- final QA hardening
