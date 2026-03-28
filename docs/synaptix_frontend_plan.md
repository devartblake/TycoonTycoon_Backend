# Synaptix Frontend Migration Plan (Flutter App)

**Scope:** All Flutter/Dart frontend work for the Trivia Tycoon -> Synaptix rebrand
**Codebase:** Flutter app (`lib/` directory and related assets)
**Companion doc:** `docs/synaptix_backend_plan.md` (backend work in the TycoonTycoon_Backend repo)

> **Governing rule:** Ship the visible product rebrand first. Delay deep technical renames until the product layer is coherent and tested.

---

## Global Frontend Principles

1. Do not begin with a global search/replace on `trivia_tycoon` or `Tycoon.*`.
2. Preserve runtime behavior before improving architecture purity.
3. Keep persistence keys, DTO names, and route path constants stable unless there is explicit migration logic.
4. Treat this as a platform rebrand, not a theme swap.
5. Make the UI read as Synaptix before touching deep namespace concerns.
6. Keep a migration log for every visible rename and every deferred technical rename.

---

## FE Packet A — Audit + Brand Surface Reframe (Phases 0–1)

### FE-A1: Frontend Audit (Phase 0)

**Objective:** Create a complete inventory of all product-facing brand strings before changing anything.

**Scope:**
- App title strings
- Splash strings and loading copy
- Onboarding copy (age group step, difficulty step, categories step, completion step)
- Settings/about labels
- Menu/home labels
- Leaderboard/rank labels
- Arcade/mini-game labels
- Skill tree labels (branch names, node descriptions)
- Economy terms (XP, coins, gems, energy, lives, store items)
- Profile/community labels
- Admin shell labels
- Dialogs, toasts, modals, banners, empty states

**Deliverables:**
- `frontend_surface_inventory.md` — screen-by-screen copy audit
- `synaptix_rename_matrix.md` — old term -> new term mapping
- Asset replacement list (logos, splash images, badge art, rank visuals)
- Route label map (current display names -> Synaptix display names)
- Persistence risk list (keys that must NOT be renamed)
- Deferred technical rename list (class names, package root, file names)

**Exit criteria:**
- All major frontend surfaces inventoried
- Rename matrix complete
- Deferred technical renames documented

---

### FE-A2: Brand Surface Reframe (Phase 1)

**Objective:** Make the app visibly read as Synaptix at first touch without destabilizing architecture.

**Target files:**
- `lib/widgets/app_logo.dart`
- `lib/screens/splash_variants/main_splash.dart`
- `lib/main.dart`
- `lib/screens/menu/game_menu_screen.dart`
- `lib/core/navigation/app_router.dart` (display labels only)
- Any app title/about/settings screens that visibly say "Trivia Tycoon"

**Work items:**

1. **App logo** (`lib/widgets/app_logo.dart`):
   - "Trivia Tycoon" -> "Synaptix"
   - Tagline "Challenge Your Mind" -> "Train. Compete. Grow."
   - Keep `tTriviaGameImage` asset unchanged for now
   - Keep fallback `Icons.psychology` (fits Synaptix)
   - Do NOT rename widget classes yet

2. **Splash** (`lib/screens/splash_variants/main_splash.dart`):
   - Loading copy -> "Loading your Synaptix experience..."
   - Keep splash timing and layout unchanged
   - Footer ("Powered by Theoretical Minds Technology") can remain

3. **Main app** (`lib/main.dart`):
   - Recovery dialog: "Welcome Back!" -> "Welcome back to Synaptix"
   - Keep `TriviaTycoonApp` class name stable
   - Add TODO comment: `// TODO(Synaptix Phase 8): Rename TriviaTycoonApp`
   - Keep `userAgeGroupProvider` override logic intact

4. **Game menu** (`lib/screens/menu/game_menu_screen.dart`):
   - Replace placeholder with a basic Synaptix Hub shell
   - AppBar title: "Synaptix Hub"
   - Welcome text, basic hub cards (Arena, Labs, Pathways, Circles)
   - Intentionally modest — full Hub build is in FE-B2

5. **Router** (`lib/core/navigation/app_router.dart`):
   - Update comments only; add Synaptix Phase 1 note
   - Do NOT rename route paths or GoRoute names
   - Defer substantive router work to FE-B2

6. **Safe search/replace** across Flutter UI widgets:
   - "Trivia Tycoon" -> "Synaptix"
   - "Trivia Game" -> "Synaptix Hub" (only in menu/home shell)
   - **NEVER** bulk replace: `package:trivia_tycoon`, route paths, DTO names, persistence keys

**Do NOT change:**
- Package root imports (`package:trivia_tycoon/...`)
- Route path constants
- DTO/model property names
- Persistence/storage keys
- Internal class names (except where noted)

**Exit criteria:**
- App launch and first-touch surfaces read as Synaptix
- Logo and splash are aligned
- Menu/home entry no longer looks placeholder or old-brand
- No package roots or backend namespaces were renamed
- Codebase remains stable and ready for FE Packet B

---

## FE Packet B — Mode/Theme Foundation + Hub (Phases 2–3)

### FE-B1: Mode and Theme Foundation (Phase 2)

**Objective:** Introduce the multi-audience presentation system (Kids / Teen / Adult) under one master brand.

**Design rule:** Do NOT replace the current `AppTheme` model — extend it.

**New files to create:**

1. `lib/synaptix/mode/synaptix_mode.dart`
   ```dart
   enum SynaptixMode { kids, teen, adult }
   ```

2. `lib/synaptix/mode/synaptix_mode_mapper.dart`
   - Maps saved age group string to `SynaptixMode`
   - kids/child/elementary/k-5 -> kids
   - teen/teens/middle/middle school -> teen
   - adult/default -> adult

3. `lib/synaptix/mode/synaptix_mode_provider.dart`
   - Riverpod `StateProvider<SynaptixMode>` (default: teen)

4. `lib/synaptix/theme/synaptix_theme_extension.dart`
   - `SynaptixTheme extends ThemeExtension<SynaptixTheme>`
   - Properties: primarySurface, accentGlow, useHighEnergyMotion, useSoftCorners, cardRadius

5. `lib/synaptix/theme/synaptix_theme_presets.dart`
   - Kids: bright, soft corners, cardRadius 20, high-energy motion
   - Teen: dark navy, neon accent, cardRadius 14, high-energy
   - Adult: charcoal, muted cyan, cardRadius 12, restrained motion

**Modifications to existing files:**

6. `lib/core/theme/themes.dart`
   - Add documentation comment only (SynaptixTheme is additive, not a replacement)
   - Do NOT remove current `ThemeType` values

7. `lib/core/services/settings/player_profile_service.dart`
   - Add additive fields: `synaptixMode`, `preferredHomeSurface`, `reducedMotion`, `tonePreference`
   - Do NOT rename or repurpose existing keys

8. `lib/main.dart`
   - Bootstrap mode after age group loads: `mapAgeGroupToSynaptixMode(savedAgeGroup)`
   - Push into provider override or state initialization
   - Do NOT rewrite bootstrap structure

**Mode behavior rules:**

| Mode | Characteristics |
|---|---|
| Kids | Larger cards, softer corners, simpler labels, brighter surfaces, fewer metrics, larger touch targets |
| Teen | Strongest Synaptix identity, action-forward, neon/accent, competition-forward, social-energy |
| Adult | Cleaner layout, restrained animation, tighter hierarchy, mastery/ranking emphasis |

**Exit criteria:**
- `SynaptixMode` enum exists
- Mode mapping from age group exists
- Mode provider exists
- Theme extension presets exist (kids/teen/adult)
- No existing theme system has been broken
- Profile settings can store preferred mode additively

---

### FE-B2: Shell and Navigation Upgrade (Phase 3)

**Objective:** Transform the menu into a real Synaptix Hub with mode-aware information architecture.

**Primary targets:**
- `lib/screens/menu/game_menu_screen.dart`
- `lib/core/navigation/app_router.dart` (display labels only)
- Supporting hub widgets

**Hub structure (required sections):**
1. Welcome header (mode-aware greeting)
2. Daily challenge / mission strip
3. Continue playing card
4. Quick-launch grid (Arena, Labs, Pathways, Circles, Journey, Store/Rewards)
5. Progress snapshot
6. Economy/reward access
7. Mode-aware emphasis area

**Mode-aware card emphasis:**
- Kids: Play + Labs + Journey + Rewards
- Teen: Arena + Pathways + Labs + Circles
- Adult: Arena + Journey + Pathways + Labs

**Supporting widgets to create:**
- `lib/synaptix/widgets/synaptix_hub_header.dart`
- `lib/synaptix/widgets/synaptix_hub_card.dart`
- `lib/synaptix/widgets/synaptix_progress_snapshot.dart`
- `lib/synaptix/widgets/synaptix_mode_banner.dart`

**Router guidance:**
- Do NOT rename route paths (`/leaderboard`, `/arcade`, `/profile`, etc.)
- Update display-facing labels only:
  - leaderboard route display -> "Arena"
  - arcade route display -> "Labs"
  - skill tree route display -> "Pathways"
  - messages/group chat display -> "Circles"
  - profile display -> "Journey"
  - admin display -> "Command"

**Exit criteria:**
- Users land on a real Synaptix Hub
- Quick-launch cards are visible and coherent
- Header and progress language adapt by mode
- App reads as a platform shell, not a placeholder game menu
- No route path regressions introduced

---

## FE Packet C — Core Feature Surface Rebrand (Phase 4)

**Objective:** Convert the 6 major product surfaces into Synaptix vocabulary. Rename surfaces, not systems.

**Core rule:** Change UI labels, headers, card titles, section framing, product-facing descriptions. Do NOT rename controllers, routes, providers, DTOs, or persistence keys.

### FE-C1: Arena (Leaderboards / Rank)

**Target files:** `lib/screens/leaderboard/...`

| Current | Synaptix-facing |
|---|---|
| Leaderboard | Arena / Leaderboard (keep "Leaderboard" inside tables) |
| Rank | Tier / Standing |
| Tier Rank | Arena Tier / Division Rank |
| Top Players | Top Players / Arena Leaders |

**Mode-aware labels:**
- Kids: "Top Players"
- Teens/Adults: "Arena Ladder", "Tier", "Division"

**Do NOT change:** sorting logic, filtering logic, controller logic, provider names, rank calculations.

---

### FE-C2: Labs (Arcade / Mini-games)

**Target files:** `lib/arcade/...`, `lib/screens/mini_games/...`

| Current | Synaptix-facing |
|---|---|
| Arcade | Labs |
| Mini Games | Labs Challenges / Training Modules |
| Daily Bonus | Daily Signal / Daily Reward |
| Local Leaderboard | Practice Board / Labs Leaderboard |

**Do NOT change:** mini-game logic, score calculation, arcade services/providers, reward logic.

---

### FE-C3: Pathways (Skill Tree)

**Target files:**
- `lib/game/controllers/skill_tree_controller.dart`
- `lib/game/providers/skill_tree_provider.dart`
- `lib/game/data/skill_tree_loader.dart`
- `lib/game/models/skill_tree_graph.dart`

| Current | Synaptix-facing |
|---|---|
| Skill Tree | Pathways / Neural Pathways |
| Skill | Node / Path |
| Unlock | Activate |
| Upgrade | Enhance |
| Branch | Track |
| Skill Category | Pathway Track |

**Recommended branch labels:** Cognition, Strategy, Momentum, Recall, Precision, Insight, Support, Enhancements

**Add:** Display label mappers (`toSynaptixPathLabel(internalName)`)
**Do NOT change:** core node-unlock logic, graph structure, loader contracts, persistence.

---

### FE-C4: Journey (Profile)

**Target files:** `lib/screens/profile/...`

| Current | Synaptix-facing |
|---|---|
| Profile | Journey (headers/shell cards) — keep "Profile" where conventional clarity needed |
| Stats | Performance |
| Achievements | Milestones |
| Progress | Journey Progress |

---

### FE-C5: Circles (Social)

**Target files:** `lib/screens/messages/...`, `lib/screens/group_chat/...`

| Current | Synaptix-facing |
|---|---|
| Messages | Messages (keep for clarity) |
| Group Chat | Group Chat / Circles |
| Friends | Circles |
| Groups | Circles |

Use "Circles" for shell cards, section grouping, nav framing. Keep conventional labels (Messages, Chats, Groups) at the detail level.

---

### FE-C6: Command (Admin)

**Target files:** `lib/admin/...`

| Current | Synaptix-facing |
|---|---|
| Admin Dashboard | Synaptix Command |
| Admin | Command |

Brand the top-level shell; keep inner admin tools conventional for operator usability.
**Do NOT change:** admin service wiring, backend calls, encryption logic, import/export, analytics calculations.

---

### FE-C Implementation Order
1. Update shell launch labels in Synaptix Hub
2. Arena headers and cards
3. Labs headers and cards
4. Pathways headers and display label mappers
5. Journey framing in profile
6. Circles framing in social surfaces
7. Command shell branding in admin
8. Empty states, subtitles, and secondary copy pass
9. Consistency scan across adjacent screens

**Exit criteria:**
- All 6 surfaces visibly read as part of the same Synaptix product family
- Shell launch cards use the new language
- No obvious mixed branding across adjacent screens
- Underlying route, provider, and data logic remain stable

---

## FE Packet D — Analytics + Stabilization (Phases 6–7)

### FE-D1: Analytics Instrumentation (Phase 6)

**Objective:** Make the Synaptix rebrand measurable.

**Core event pattern:**
```dart
trackEvent("synaptix_surface_opened", {
  "surface": "arena",
  "mode": mode.name,
  "entry_point": "hub_card",
});
```

**Instrument:**
- Hub card taps
- Onboarding mode mapping
- Arena entry
- Labs entry
- Pathways opened
- Journey viewed
- Circles engagement

**Additive dimensions:** `synaptix_mode`, `surface`, `entry_point`, `audience_segment`

**Rules:**
- Additive only — do NOT break existing analytics
- Do NOT rewrite event schema

**Cross-reference:** Backend should align analytics dimensions (see `synaptix_backend_plan.md` BE-D1)

---

### FE-D2: Stabilization and QA (Phase 7)

**QA checklist:**
- [ ] App launch
- [ ] Auth/bootstrap
- [ ] Onboarding flow (age group -> mode mapping)
- [ ] Hub rendering (all 3 modes)
- [ ] Mode selection and mapping
- [ ] Arena launch and navigation
- [ ] Labs launch and navigation
- [ ] Pathways launch and navigation
- [ ] Journey/profile load
- [ ] Circles/messages/groups
- [ ] Command/admin
- [ ] Settings and persistence
- [ ] Economy labels consistent

**Consistency pass:**
- [ ] No remaining "Trivia Tycoon" in high-visibility paths
- [ ] No mixed old/new language across adjacent screens
- [ ] Mode-specific differences render correctly
- [ ] Frontend labels match backend dashboards/docs (cross-check with backend plan)

**Exit criteria:**
- No major functional regressions
- No major brand inconsistency in core flows
- System stable enough to decide on Packet E

---

## FE Packet E — Optional Deep Technical Rename (Phase 8)

**Default recommendation: DEFER unless Packets A–D are stable.**

### Workstream 1: Frontend Symbol Cleanup (Low Risk)
- `TriviaTycoonApp` -> `SynaptixApp`
- Internal helper names tied to old branding
- Comments and README content

### Workstream 2: Package Root Rename (Higher Risk)
- `package:trivia_tycoon/...` -> `package:synaptix/...`
- Treat as a separate subproject — not a casual global replace
- Risks: wide import churn, broken generated references, broken tests

**Preconditions for Packet E:**
- Stable release candidate from Packet D
- No major outstanding rebrand bugs
- Build/test coverage acceptable
- Rollback strategy documented

---

## Economy Rename Reference

| Current / Generic | Synaptix-facing | Notes |
|---|---|---|
| XP | Neural XP or XP | Keep internal `xp` model names |
| Coins | Credits | Consumer-facing rename |
| Gems | Synapse Shards | Premium currency |
| Energy | Focus / Cognitive Energy | Depends on mode tone |
| Lives | Attempts / Lives | Mode-dependent wording |
| Store | Exchange / Store | "Store" is acceptable |
| Rewards | Rewards / Unlocks / Milestones | Context-specific |
| Daily Bonus | Daily Signal / Daily Reward | Optional thematic rename |
| Power-ups | Enhancements | — |

---

## Terminology Quick Reference

| Existing Concept | Synaptix Concept |
|---|---|
| Trivia Tycoon | Synaptix |
| Game menu | Synaptix Hub |
| Leaderboard | Arena |
| Tier rank | Arena Tier / Division Rank |
| Skill tree | Pathways / Neural Pathways |
| Arcade | Labs |
| Mini-games | Training Modules / Labs Challenges |
| Missions | Missions / Signals |
| Profile progression | Journey |
| Friends / groups | Circles |
| Admin dashboard | Synaptix Command |

---

## Cross-Reference: Backend Alignment Points

These are moments where the frontend plan depends on or should align with backend work (see `synaptix_backend_plan.md`):

| Frontend Phase | Backend Alignment |
|---|---|
| FE-B1 (Mode/Theme) | Backend may need additive profile fields for mode persistence |
| FE-C (Surface Rebrand) | No backend dependency — display-level only |
| FE-D1 (Analytics) | Analytics dimensions must match backend event taxonomy |
| FE-D2 (Stabilization) | Frontend labels should match backend dashboard/docs language |
| FE-E (Deep Rename) | Independent of backend Packet E |
