# Synaptix Phase Implementation Order
## Detailed phased execution plan for the Trivia Tycoon -> Synaptix migration

**Purpose:** turn the Synaptix master migration blueprint into an implementation-ready phased plan  
**Audience:** AI implementation systems, engineers, designers, and product owners  
**Scope:** Flutter frontend, .NET backend, operator dashboards, analytics, copy, and migration safety

---

## 1. How to use this document

This plan is sequenced so another AI system can execute the Synaptix migration in a controlled order without destabilizing the existing codebase.

The governing rule is:

**Ship the visible product rebrand first. Delay deep technical renames until the product layer is coherent and tested.**

Every phase below includes:
- objective
- scope
- frontend work
- backend work
- deliverables
- dependencies
- risks
- exit criteria

This is intentionally implementation-first rather than strategy-first.

---

## 2. Global implementation principles

Before phase work starts, the implementation system should obey these rules:

1. Do not begin with a global search/replace on `trivia_tycoon` or `Tycoon.*`.
2. Preserve runtime behavior before improving architecture purity.
3. Keep persistence keys, DTO names, and endpoint contracts stable unless there is explicit migration logic.
4. Treat the product as a platform rebrand, not just a theme swap.
5. Prioritize:
   - home/menu shell
   - onboarding
   - leaderboard/Arena
   - arcade/Labs
   - skill tree/Pathways
   - profile/community
   - admin/Command
6. Make the UI read as Synaptix before touching deep namespace concerns.
7. Keep a migration log for every visible rename and every deferred technical rename.

---

## 3. Phase overview at a glance

| Phase | Name | Main Outcome |
|---|---|---|
| 0 | Audit and Freeze | full inventory, rename matrix, migration safety log |
| 1 | Brand Surface Reframe | app visibly reads as Synaptix without deep code churn |
| 2 | Mode and Theme Foundation | Kids / Teen / Adult presentation layer added |
| 3 | Shell and Navigation Upgrade | Synaptix Hub and platform IA introduced |
| 4 | Core Feature Surface Rebrand | Arena, Labs, Pathways, Journey, Circles, Command |
| 5 | Backend Product-Language Alignment | backend-visible docs/dashboards/copy align with Synaptix |
| 6 | Analytics and Telemetry Upgrade | new platform surfaces become measurable |
| 7 | Stabilization and Regression Hardening | quality pass, consistency pass, bug hardening |
| 8 | Optional Deep Technical Rename | only if still valuable after stabilization |

---

## 4. Phase 0 - Audit and Freeze

### Objective
Create a complete migration inventory before implementation begins.

### Why this phase exists
The project already spans a large Flutter frontend and a broad .NET/Python backend. A rebrand without inventory control will create inconsistent copy, broken labels, and avoidable technical churn.

### Frontend scope
Inventory all visible product-facing surfaces, including:
- app title strings
- splash strings
- onboarding copy
- settings labels
- menu/home labels
- leaderboard/rank labels
- arcade labels
- skill tree labels
- economy terms
- profile/community labels
- admin shell labels
- dialogs, toasts, modals, banners, and empty states

### Backend scope
Inventory all product-visible backend surfaces, including:
- Swagger/OpenAPI titles and descriptions
- dashboard titles and headings
- operator copy
- server-rendered references to product areas
- event names that may be exposed in analytics/admin tools
- docs and readmes that affect operators or downstream AI systems

### Required outputs
1. Rename matrix
2. Asset replacement list
3. Route label map
4. Theme entry-point inventory
5. Persistence risk list
6. Deferred technical rename list
7. Backend project impact matrix

### Deliverables
- `synaptix_rename_matrix.md` or CSV
- `frontend_surface_inventory.md`
- `backend_surface_inventory.md`
- `risk_register.md`

### Dependencies
None. This is the starting phase.

### Risks
- missing old copy strings
- underestimating hidden admin surfaces
- forgetting economy terminology in edge-case widgets

### Exit criteria
- all major frontend surfaces inventoried
- all product-visible backend surfaces inventoried
- rename matrix approved
- deferred technical rename list documented

---

## 5. Phase 1 - Brand Surface Reframe

### Objective
Make the application visibly read as Synaptix across first-touch surfaces without destabilizing architecture.

### Scope boundary
This phase changes what users and operators see. It does **not** do a broad package/namespace rename.

### Frontend work
Update:
- splash branding
- app title in shell
- first-run language
- top-level visible brand strings
- key menu labels
- obvious Trivia Tycoon references in UI copy

Primary files/surfaces:
- `lib/main.dart`
- splash-related files
- app shell/title areas
- high-visibility settings/about surfaces
- current menu/home entry

### Backend work
Update:
- API doc titles/subtitles
- operator/dashboard headers
- product-facing documentation headings
- public-facing metadata where beneficial

### What should not change yet
- route path constants unless necessary
- package root imports
- DTO property names
- backend namespaces
- repo/project names
- persistence keys

### Deliverables
- Synaptix brand shell active in app UI
- updated splash/welcome assets or placeholders
- updated primary dashboard headings
- app visibly identifiable as Synaptix

### Dependencies
Requires Phase 0 rename matrix.

### Risks
- visible brand mismatch between splash and deeper screens
- incomplete copy replacement causing mixed Trivia Tycoon / Synaptix language

### Exit criteria
- app launch and first-touch surfaces read as Synaptix
- no critical functional regressions
- no deep architecture churn introduced

---

## 6. Phase 2 - Mode and Theme Foundation

### Objective
Introduce the multi-audience presentation system that allows Synaptix to serve K-12, teens, and adults under one master brand.

### Product rationale
Synaptix is strongest for teens and adults by default. Kids support must come from adaptive presentation, not a separate app.

### Frontend work
Add:
- `SynaptixMode` enum
- mode provider
- theme provider
- mode mapping helper from age/profile
- optional copy provider
- additive Synaptix theme extension layer

Suggested folder additions:
- `lib/synaptix/mode/`
- `lib/synaptix/theme/`
- `lib/synaptix/brand/`
- `lib/synaptix/copy/`
- `lib/synaptix/widgets/`

### Mode definitions
- kids
- teen
- adult

### Theme behavior goals
Kids:
- brighter
- simpler
- friendlier
- larger touch targets
- reduced density

Teen:
- strongest Synaptix visual identity
- neon/glow moderation
- competition-forward
- social-energy feel

Adult:
- restrained
- cleaner
- data-aware
- premium/mastery feel

### Backend work
Minimal backend work in this phase. Only add support if needed for:
- preferred mode persistence
- preferred home surface persistence
- reduced motion / tone preference storage

### Deliverables
- Synaptix mode state model
- theme switching architecture
- age group -> default mode mapping logic
- documented mode behavior rules

### Dependencies
Requires Phase 0 inventory and Phase 1 visible rebrand baseline.

### Risks
- duplicating the old theme system instead of extending it
- binding mode permanently to age without override support

### Exit criteria
- one codebase can render mode-aware UI differences
- no need for separate app variants
- current theme system remains operational

---

## 7. Phase 3 - Shell and Navigation Upgrade

### Objective
Transform the current menu/shell into a real platform home experience: the Synaptix Hub.

### Product rationale
The current `game_menu_screen.dart` is the highest-value opportunity for communicating the platform shift.

### Frontend work
Create or refactor the home shell into **Synaptix Hub**.

Hub sections should include:
- welcome header
- daily challenge / mission strip
- current rank or progress snapshot
- quick-launch cards
- economy HUD
- continue-playing CTA
- mode-aware featured modules

Suggested launch cards:
- Arena
- Labs
- Pathways
- Circles
- Profile/Journey
- Rewards/Store
- Seasonal/Events

### Router work
Do not aggressively rewrite route paths yet.  
Instead:
- update route display names
- update nav grouping
- build a clearer information architecture
- preserve underlying routing contracts where possible

### Backend work
Minimal. Potential additions:
- preferred home surface in user profile
- hub analytics entry events
- future recommendation hooks

### Deliverables
- Synaptix Hub design and implementation
- updated nav labels/grouping
- improved shell-level platform framing

### Dependencies
Requires Phase 2 mode/theme foundation.

### Risks
- shell redesign that ignores current route structure
- overcomplicated first implementation of the hub
- breaking existing navigation assumptions

### Exit criteria
- users land on a real Synaptix home experience
- the app no longer feels like a loose collection of screens
- navigation reads as a platform, not a list of isolated modules

---

## 8. Phase 4 - Core Feature Surface Rebrand

### Objective
Convert the major product surfaces into the Synaptix platform vocabulary while preserving logic and data contracts.

### Surfaces in this phase
1. Leaderboards and rank -> Arena
2. Arcade and mini-games -> Labs
3. Skill tree -> Pathways
4. Profile progression -> Journey
5. Messaging and group features -> Circles
6. Admin -> Command

---

### 8.1 Arena
#### Frontend work
Update:
- headers
- section titles
- tier language
- competition framing
- mode-aware wording

Possible copy rules:
- kids: “Top Players”
- teens/adults: “Arena Ladder”, “Tier”, “Division”

#### Backend work
Update only product-facing docs/admin labels where useful.

#### Deliverables
- leaderboard surfaces read as Synaptix Arena
- current ranking systems still work unchanged under the hood

---

### 8.2 Labs
#### Frontend work
Rebrand arcade and practice surfaces as Labs:
- arcade hub
- mini-games
- daily loops
- practice leaderboards

#### Backend work
Optional dashboard/reporting label alignment only.

#### Deliverables
- practice and mini-game surfaces now fit the broader Synaptix brand

---

### 8.3 Pathways
#### Frontend work
Upgrade the skill tree as a flagship Synaptix feature.

Guidance:
- keep current controller/provider/loader logic if sound
- re-theme branch names
- re-theme node descriptions
- preserve hex/spider design direction and refine it into neural-path language

Recommended branch groups:
- Cognition
- Strategy
- Momentum
- Recall
- Precision
- Insight
- Support
- Enhancements

#### Backend work
Minimal unless skill-tree labels are delivered dynamically and need product-facing mapping.

#### Deliverables
- skill tree becomes Synaptix Pathways
- progression feels central to the product identity

---

### 8.4 Journey
#### Frontend work
Reframe profiles around:
- Journey
- Milestones
- Performance
- Progress
- preferred style
- pathways status

#### Backend work
Optional additive profile preference fields.

#### Deliverables
- profile area expresses mastery and progression instead of just stats

---

### 8.5 Circles
#### Frontend work
Group chat/messages/community surfaces move under the Circles frame.

Keep usability-first labels where needed:
- “Messages”
- “Chats”
- “Groups”

But place them under a Circles umbrella in IA and shell language.

#### Backend work
No deep changes required initially.

#### Deliverables
- social/community feels like part of the same platform family

---

### 8.6 Command
#### Frontend/admin work
Rebrand admin shells and headers as Synaptix Command.

Keep logic stable. Change:
- top-level labels
- dashboard grouping
- section headings
- visual shell treatment
- copy and IA

#### Backend/dashboard work
Apply corresponding rebrand to:
- Blazor operator dashboard
- Vue/web dashboard surfaces where product-visible

#### Deliverables
- operator/admin surfaces align with the rest of the product identity

### Dependencies
Requires Phases 1 through 3.

### Risks
- too much terminology replacement harming clarity
- mixed old/new language across adjacent screens
- over-theming admin before finishing IA consistency

### Exit criteria
- Arena, Labs, Pathways, Journey, Circles, and Command are all present
- users can understand the new product model without confusion
- logic and controllers remain stable underneath

---

## 9. Phase 5 - Backend Product-Language Alignment

### Objective
Align backend-visible product language with Synaptix while preserving code and contract stability.

### Scope
This phase is about what operators, developers, and docs readers see—not a solution-wide rename.

### API work
Update:
- Swagger/OpenAPI titles
- descriptions
- feature summaries
- examples where product vocabulary matters

Keep stable:
- endpoint paths
- DTO fields
- domain model names
- service contracts

### Application and domain work
Generally keep stable. Only add:
- new preference flags if needed
- analytics dimensions
- additive display-level terminology mappings

### Operator dashboard work
Bring Blazor and other dashboards into shared Synaptix vocabulary:
- Arena
- Labs
- Pathways
- Circles
- Command

### Sidecar/docs work
Update service descriptions and documentation where they reference old product language.

### Deliverables
- backend docs and operator surfaces support Synaptix consistently
- frontend and backend language no longer feel disconnected

### Dependencies
Best done after Phase 4, so the frontend product language is settled first.

### Risks
- mixing implementation-level names with product-facing names
- renaming too deeply in backend projects during a docs/copy phase

### Exit criteria
- docs and dashboards reflect Synaptix consistently
- no contract-breaking backend churn introduced

---

## 10. Phase 6 - Analytics and Telemetry Upgrade

### Objective
Make the Synaptix rebrand measurable.

### Rationale
A platform rebrand should not just “look” different. The system should be able to measure whether users understand and use Arena, Labs, Pathways, Circles, and the new Hub.

### Recommended additive analytics dimensions
- `synaptix_mode`
- `audience_segment`
- `home_surface`
- `arena_entry`
- `labs_entry`
- `pathways_opened`
- `circles_engagement`
- `journey_viewed`
- `command_surface`
- `brand_version`

### Frontend work
Add instrumentation to:
- hub cards
- onboarding mode mapping
- arena entry
- labs entry
- pathways open
- journey open
- circles engagement

### Backend work
Add or align:
- event taxonomy docs
- analytics labels
- dashboard dimensions
- admin analytics wording

### Deliverables
- measurable new platform surfaces
- analytics that distinguish old vs. new product framing if needed

### Dependencies
Best done after core surfaces have been rebranded.

### Risks
- introducing analytics churn too early
- inconsistent event naming across frontend/backend

### Exit criteria
- product teams can measure adoption of the new surfaces
- analytics terminology matches product terminology

---

## 11. Phase 7 - Stabilization and Regression Hardening

### Objective
Stabilize the migration, remove inconsistencies, and harden the system before considering deeper cleanup.

### Frontend QA checklist
- app launch
- auth/bootstrap
- onboarding flow
- hub rendering
- mode selection and mapping
- leaderboard/Arena
- arcade/Labs
- skill tree/Pathways
- profile/Journey
- messages/groups/Circles
- admin/Command
- settings and persistence
- economy labels

### Backend QA checklist
- Swagger docs render correctly
- dashboards load correctly
- admin copy is consistent
- no accidental contract breaks
- event dimensions appear as expected
- no namespace-related build regressions

### Cross-layer QA
- frontend labels match backend dashboards/docs
- no mixed Trivia Tycoon / Synaptix copy in key flows
- operator surfaces use the same vocabulary as the app

### Deliverables
- regression checklist completion
- terminology consistency pass
- bug log and fix pass
- deferred rename list re-evaluated

### Dependencies
Requires Phases 1 through 6.

### Risks
- shipping with mixed vocab
- skipping admin QA because the user-facing app looks correct
- failing to test mode-specific differences

### Exit criteria
- no major functional regressions
- no major brand inconsistency in core flows
- system is stable enough to decide whether deeper technical renames are worth it

---

## 12. Phase 8 - Optional Deep Technical Rename

### Objective
Decide whether a deeper technical rename is still worth doing once the product migration is already successful.

### What may be renamed here
Frontend:
- package root
- import namespaces
- folder/file naming cleanup

Backend:
- project names
- solution names
- namespaces
- deployment/service names
- JWT issuer/audience names
- CI/CD pipeline names
- telemetry service names

### Why this phase is optional
By the time Phase 7 is complete, the product already reads as Synaptix.  
A deep technical rename should only happen if it offers enough long-term maintenance value to justify the risk.

### Preconditions
- stable release candidate
- no major outstanding rebrand bugs
- codemod or rename plan prepared
- build/test coverage acceptable
- rollback strategy documented

### Deliverables
- technical rename decision memo
- if approved: namespace/project rename implementation plan
- if rejected: permanent “product name vs technical root” guidance

### Risks
- broad compile/runtime breakage
- broken tooling/scripts
- dashboard/deployment naming inconsistencies
- test churn without user-facing value

### Exit criteria
One of:
1. technical rename approved with explicit plan, or
2. technical rename intentionally deferred indefinitely

---

## 13. Recommended implementation order inside each major surface

### Home / Hub
1. rename visible shell copy
2. add mode-aware header
3. add quick-launch sections
4. add progress and economy summary
5. add analytics instrumentation

### Onboarding
1. keep current structure stable
2. adjust copy
3. map age group -> default mode
4. store preferred mode/home surface
5. instrument onboarding decisions

### Arena
1. update copy and headers
2. update rank/tier naming
3. add mode-aware labels
4. align analytics/admin wording

### Labs
1. update copy
2. align hub launch card and shell labels
3. update practice/challenge naming
4. align backend reports if needed

### Pathways
1. keep current graph logic
2. re-theme branch names
3. re-theme node descriptions
4. refine visual treatment
5. add progression analytics

### Journey / Circles
1. update IA labels
2. refine profile sections
3. wrap social features in Circles framing
4. add profile preference persistence

### Command
1. update admin shell branding
2. reorganize dashboard grouping language
3. align product-facing docs
4. verify operator workflows still behave correctly

---

## 14. Recommended staffing / AI delegation model

### AI System A - Inventory and rename control
Handles:
- phase 0 inventories
- rename matrix
- risk register
- deferred rename log

### AI System B - Frontend shell and theme migration
Handles:
- phases 1 to 4 frontend work
- Synaptix mode/theme system
- Hub
- Arena/Labs/Pathways/Journey/Circles UI

### AI System C - Backend/operator alignment
Handles:
- phase 5 backend docs/operator changes
- Swagger titles/descriptions
- dashboard shell alignment
- event taxonomy docs

### AI System D - QA and telemetry
Handles:
- phase 6 analytics
- phase 7 regression hardening
- terminology consistency checks

---

## 15. Final phase recommendation

The safest and most effective order is:

1. Phase 0 - Audit and Freeze
2. Phase 1 - Brand Surface Reframe
3. Phase 2 - Mode and Theme Foundation
4. Phase 3 - Shell and Navigation Upgrade
5. Phase 4 - Core Feature Surface Rebrand
6. Phase 5 - Backend Product-Language Alignment
7. Phase 6 - Analytics and Telemetry Upgrade
8. Phase 7 - Stabilization and Regression Hardening
9. Phase 8 - Optional Deep Technical Rename

This order gives you the strongest balance of:
- brand clarity
- implementation safety
- multi-audience support
- frontend/backend consistency
- long-term maintainability

---

## Appendix A - Phase deliverables summary

| Phase | Key Deliverables |
|---|---|
| 0 | rename matrix, inventories, risk register |
| 1 | visible Synaptix brand shell |
| 2 | mode/theme architecture |
| 3 | Synaptix Hub and nav IA |
| 4 | Arena, Labs, Pathways, Journey, Circles, Command |
| 5 | backend docs/dashboard language alignment |
| 6 | analytics dimensions and event updates |
| 7 | regression checklist and terminology consistency pass |
| 8 | technical rename decision and optional execution plan |

## Appendix B - Stop conditions

Pause the migration if any of the following happen:
- auth/bootstrap breaks
- onboarding persistence becomes unstable
- route regressions appear across core flows
- mixed old/new branding becomes widespread and untracked
- a deep namespace rename begins before Phase 7 without explicit approval
