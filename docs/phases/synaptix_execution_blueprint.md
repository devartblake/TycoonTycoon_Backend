# Synaptix Execution Blueprint
## Platform-level rebrand and system architecture upgrade for the current Trivia Tycoon Flutter project

**Prepared for:** downstream AI implementation system  
**Input basis:** uploaded `lib.zip` project structure and prior Synaptix strategy discussions  
**Current codebase brand:** `Trivia Tycoon` / package imports `package:trivia_tycoon/...`  
**Target product brand:** `Synaptix`

---

## 1. Executive summary
This blueprint defines how to evolve the current Trivia Tycoon codebase into **Synaptix**, a broader cognitive competition platform that can support K-12 learners, teens, and adults without fragmenting the application into separate products.

This should **not** be treated as a simple visual rename. The rebrand has to operate at four layers at once:

- Brand layer - move from a generic trivia-game identity to a more defensible platform identity.
- Experience layer - support age-aware and mode-aware presentation while keeping one shared app and codebase.
- System layer - align navigation, theme architecture, economy naming, skill tree semantics, and onboarding to the new platform model.
- Migration layer - preserve current package stability during implementation, then optionally perform deeper namespace/package renames later.

> Do not start by globally renaming every `trivia_tycoon` import. First ship the product rebrand in UI/UX/content/theme/navigation, then optionally do a second-pass technical namespace cleanup after the product layer is stable.

## 2. Current project observations from the uploaded `lib` directory
The uploaded codebase is already larger than a standard trivia app. It contains major subsystems that justify the Synaptix platform direction.

### 2.1 Core product areas already present
- `lib/screens/question/...` - standard quiz and question flows
- `lib/screens/mini_games/...` - puzzle and mini-game systems
- `lib/arcade/...` - arcade hub, arcade missions, arcade leaderboards, daily bonus
- `lib/screens/leaderboard/...` - leaderboard, tier rank, animated leaderboard widgets
- `lib/screens/profile/...` - profile tabs, enhanced profile screens, avatar systems
- `lib/screens/messages/...` and `lib/screens/group_chat/...` - social/community surface area
- `lib/admin/...` - substantial admin dashboard and content management layer
- `lib/core/services/...` and `lib/core/manager/service_manager.dart` - centralized service architecture
- `lib/game/controllers/skill_tree_controller.dart`, `lib/game/data/skill_tree_loader.dart`, `lib/game/models/skill_tree_graph.dart` - skill tree infrastructure
- `lib/game/analytics/...` and `lib/admin/widgets/analytics/...` - analytics foundations

### 2.2 Evidence that the app is already multi-audience capable
- `main.dart` loads a saved age group before the first frame.
- `PlayerProfileService` stores `ageGroup`, preferred categories, premium status, avatar, and role information.
- Onboarding includes `age_group_step.dart`, `difficulty_step.dart`, `categories_step.dart`, and completion flow.
- The application already has route, service, and provider complexity consistent with a platform, not a single-mode casual game.

### 2.3 Current theme architecture realities
- `lib/core/theme/themes.dart` defines `ThemeType { main, allStar, competition }`
- `lib/core/theme/styles.dart` contains reusable typography, spacing, shadows, and corner tokens.
- Theme logic exists, but it is not yet structured around a master brand + audience modes architecture.
- Synaptix should extend the existing theme/token approach rather than replace it with an unrelated design system.

### 2.4 Current weak points relevant to the rebrand
- `screens/menu/game_menu_screen.dart` is currently minimal and can become a key Synaptix home/hub surface.
- Some naming remains tightly tied to “trivia” instead of a broader cognitive platform.
- Theme and experience logic appear more screen-specific than mode-driven.
- There is likely a high count of hard-coded strings and import paths referencing `trivia_tycoon`.

## 3. Rebrand objective
The objective is to reposition the product as:

> Synaptix - a cognitive competition and progression platform built on trivia, puzzle play, adaptive challenge, and social mastery.

This changes the strategic framing from:
- old framing: “a trivia game with extra features”
- new framing: “a smart play platform with trivia as one of several core modes”

The uploaded project already includes:
- trivia
- missions
- arcade
- tiers/ranks
- profiles
- social/messaging
- admin analytics
- skill trees

The codebase is already structurally closer to Synaptix than to a narrow brand like “Trivia Tycoon.”

## 4. Brand architecture
### 4.1 Master brand
**Synaptix**

This is the app-level, platform-level, and identity-level brand.

Use it in:
- app name in UI
- splash system
- app icon / logo system
- top-level navigation labels
- store positioning
- social/community positioning
- premium and economy naming
- platform messaging
- future educational or competitive extensions

### 4.2 Sub-brand / mode naming framework
These should be implemented as product surfaces, not as separate apps.

| Layer | Proposed Name | Purpose |
|---|---|---|
| Master brand | Synaptix | umbrella identity |
| Youth-friendly surface | Synaptix Junior or Synaptix Play | softer kids-facing entry |
| Competitive surface | Synaptix Arena | PvP, tiers, ranked play |
| Growth surface | Synaptix Pathways | skill tree / progression |
| Practice surface | Synaptix Labs | training, mini-games, drills |
| Social/community | Synaptix Circles | messages, groups, social |
| Admin/ops | Synaptix Command | operator/admin dashboard |

### 4.3 Why this structure is preferable
It lets the app retain one codebase while changing the language by screen and audience segment. It also avoids forcing the teen/adult tone onto younger users at every touchpoint.

## 5. Audience strategy and alignment
The user concern was correct: “Synaptix” feels strongest for teens and adults by default. The solution is not to abandon the name; it is to build a mode-adaptive experience layer.

### 5.1 Segment fit summary
| Segment | Fit with “Synaptix” | What must change |
|---|---|---|
| K-5 / early learners | moderate | simplify language, brighter theme, friendly iconography, softer copy |
| Middle school / teens | excellent | use full neural/futuristic brand language |
| Adults | excellent | use darker, cleaner, strategic presentation |

### 5.2 Product rule
Keep one master brand. Adapt the presentation and copy by mode and age group.

### 5.3 Implication for implementation
The app should not ask “Which app do you want?”
It should ask:
- who is playing?
- what experience do they want?
- what level of challenge/tone is appropriate?

That data already fits the current onboarding and `PlayerProfileService` model.

## 6. Rebrand principles for the AI implementation system
The downstream AI should follow these principles throughout the redesign:

- Preserve functional architecture first.
- Change naming, visual language, copy, and theme behavior in a staged way.
- Use the existing service/provider/router structure as the primary skeleton.
- Avoid unnecessary folder churn early.
- Keep current DTO and backend contract names stable unless there is strong value in changing them.
- Prefer adapters, wrappers, and extension layers over destructive rewrites.
- Respect existing admin, analytics, missions, and arcade systems as first-class parts of the new platform.

## 7. Recommended migration strategy
### 7.1 Phase 0 - freeze and map
Before implementation:
- inventory all product-facing brand strings
- inventory logo/asset references
- inventory route titles
- inventory theme entry points
- inventory economy labels
- inventory skill tree labels
- inventory splash and onboarding copy
- inventory package/import root references to `trivia_tycoon`

Deliverable:
- a rename map CSV or markdown table
- a screen-by-screen copy replacement checklist
- an asset replacement checklist

### 7.2 Phase 1 - product-layer rebrand only
Change:
- app-facing name strings
- splash and onboarding branding
- menu/home copy
- screen titles
- high-level route labels
- visible economy labels
- skill tree naming
- leaderboard/rank terminology where needed

Do not change:
- package import root
- internal filenames unless needed
- API DTO names unless exposed in UI
- core persistence keys unless there is a migration plan

### 7.3 Phase 2 - theme and UI system integration
Introduce:
- Synaptix theme extension
- mode-aware palettes
- audience-mode typography/spacing variants
- neural + honeycomb visual language
- adaptive screen wrappers

### 7.4 Phase 3 - feature semantics upgrade
Convert “trivia app semantics” into “cognitive platform semantics” across:
- missions
- arcade
- skill tree
- profile
- rank/tier
- social
- admin analytics

### 7.5 Phase 4 - optional technical namespace cleanup
Only after stability:
- rename package root from `trivia_tycoon` if desired
- rename folders/files where it improves maintainability
- migrate assets to Synaptix naming conventions
- introduce codemods if needed

## 8. Namespace and package rename policy
### 8.1 Recommended position
Do not globally rename `package:trivia_tycoon/...` in the first implementation pass.

### 8.2 Why
The uploaded project is large. A deep package/root rename would create broad churn across:
- imports
- generated files
- route files
- provider references
- tests
- scripts
- documentation
- CI/build tooling
- possibly native package IDs later

That is a separate engineering concern from the product rebrand.

### 8.3 Safer sequence
1. Ship product brand as Synaptix in UI.
2. Stabilize screens, assets, themes, and routing.
3. Then decide whether package root rename is worth the risk.

## 9. Design system blueprint
### 9.1 New top-level design system concept
Build a Synaptix UI system as an extension of the current theme/token architecture.

Recommended new layer:
- `SynaptixMode` enum
- `SynaptixTheme` ThemeExtension
- `SynaptixBrandTokens`
- `SynaptixSurfaceStyle`
- `SynaptixMotionTokens`

These should coexist with current `AppTheme`, `styles.dart`, and helper utilities until migration is complete.

### 9.2 Proposed audience modes
| Mode | Description | Primary use |
|---|---|---|
| kids | bright, simple, playful, low-density | K-5 and early learners |
| teen | neon, energetic, competitive, social | default/mainstream experience |
| adult | minimal, strategic, cleaner, data-aware | older players / practice / prestige |

### 9.3 Mapping from current age data
The current project already stores age group. Use a mapping layer:
- `child`, `kids`, `elementary` -> `SynaptixMode.kids`
- `teens` -> `SynaptixMode.teen`
- `adult` -> `SynaptixMode.adult`

Do not bind mode permanently to age only. Allow manual override in settings later.

### 9.4 Color strategy
- Kids: bright coral / gold / mint / sky blue / white backgrounds
- Teen: electric blue / neon purple / cyan glow / deep navy
- Adult: charcoal / cobalt / muted cyan / graphite / subtle glow

### 9.5 Typography strategy
The current codebase already has `OpenSans` and `Faustina`.
Recommended approach:
- Kids mode: prioritize highly readable friendly sans usage
- Teen mode: combine clean sans with bold display accents
- Adult mode: more restrained use of current serif/sans combinations where it improves premium tone

## 10. Screen-system transformation plan
### 10.1 Entry points
#### `lib/main.dart`
Current behavior:
- initializes app
- loads auth state
- loads saved age group
- runs app
- supports crash recovery

Synaptix responsibilities to add:
- initialize selected Synaptix mode from age group/profile
- initialize brand theme layer
- ensure splash and recovery dialogs reflect Synaptix identity
- future: load profile-specific home surface

#### `lib/core/navigation/app_router.dart`
Current behavior:
- central route orchestration for large app surface

Synaptix plan:
- keep router structure
- update route titles and entry labels
- introduce more explicit experience surfaces:
  - home
  - arena
  - pathways
  - labs
  - circles
  - profile
  - command/admin

### 10.2 Home / menu hub
#### `lib/screens/menu/game_menu_screen.dart`
This file is currently underutilized and should become the first major Synaptix opportunity.

It should evolve into a Synaptix Hub Screen with:
- mode-aware welcome header
- quick launch cards
- daily challenge / mission strip
- current rank/tier summary
- economy HUD
- progress summary
- audience-specific CTA emphasis

Recommended hub card groups:
- Continue Playing
- Daily Challenge
- Arena
- Pathways
- Labs / Mini-games
- Community / Circles
- Events / Seasonal
- Rewards / Store

### 10.3 Onboarding
Relevant files:
- `screens/onboarding/...`
- especially `age_group_step.dart`, `difficulty_step.dart`, `categories_step.dart`, `completion_step.dart`

Synaptix onboarding should:
- keep age group selection
- use age group to propose a visual mode
- introduce the product as a learning/competition platform, not only a trivia game
- explain the available pathways:
  - compete
  - practice
  - grow
  - explore
- preserve category selection and difficulty preferences

### 10.4 Leaderboards and rank
Relevant files:
- `screens/leaderboard/leaderboard_screen.dart`
- `screens/leaderboard/tier_rank_screen.dart`
- `screens/leaderboard/widgets/...`
- `game/controllers/leaderboard_controller.dart`

Synaptix plan:
- rename the surface to **Synaptix Arena**
- shift copy from “leaderboard only” to “competitive ladder”
- retain current categories, but consider relabels where beneficial
- integrate tier identity more strongly into UI headers and progression widgets
- introduce mode-sensitive language:
  - kids: “Top Players”
  - teens/adults: “Arena Ladder”, “Division”, “Tier”

### 10.5 Skill tree
Relevant files:
- `game/controllers/skill_tree_controller.dart`
- `game/data/skill_tree_loader.dart`
- `game/models/skill_tree_graph.dart`
- `game/providers/skill_tree_provider.dart`
- `core/theme/hex_spider_theme.dart`
- `core/theme/skill_category_colors.dart`

Synaptix rebrand for skill tree:
- feature name: **Pathways** or **Neural Pathways**
- node names should shift from generic skills to cognitive/progression language
- visuals should combine current hex/spider exploration with neural node connections

Recommended branches:
- Cognition
- Strategy
- Momentum
- Support
- Recall
- Precision
- Insight
- Enhancements

### 10.6 Arcade / mini-games
Relevant files:
- `lib/arcade/...`
- `lib/screens/mini_games/...`

This area should become **Synaptix Labs**.

Recommended brand mapping:
- Arcade Hub -> Synaptix Labs
- Daily Bonus -> Labs Reward / Daily Signal
- Local Arcade Leaderboard -> Labs Leaderboard or Practice Board
- Memory/Pattern/Quick Math -> training modules or challenges

### 10.7 Profiles and social
Relevant files:
- `lib/screens/profile/...`
- `lib/screens/messages/...`
- `lib/screens/group_chat/...`
- `lib/core/services/social/...`

Recommended naming:
- friends/groups/community -> **Circles**
- profile progress -> **Journey**
- achievements -> **Milestones**
- stats -> **Performance**

### 10.8 Admin
Relevant files:
- `lib/admin/...`

Recommended brand:
- **Synaptix Command**

Keep admin functionality stable and primarily rebrand navigation, labels, theming, and layout cohesion first.

## 11. Economy rename blueprint
Recommended product-facing terms:

| Current / generic | Synaptix-facing term | Notes |
|---|---|---|
| XP | Neural XP or XP | keep internal `xp` model names if needed |
| Coins | Credits | easy consumer-facing rename |
| Gems | Synapse Shards | premium currency |
| Energy | Focus or Cognitive Energy | depends on tone |
| Lives | Attempts or Lives | use mode-dependent wording |
| Store | Exchange or Store | “Store” is acceptable if kept |
| Rewards | Rewards / Unlocks / Milestones | context-specific |
| Daily Bonus | Daily Signal / Daily Reward | optional thematic rename |

Recommendation:
Keep internal model/property names stable where possible. Change visible labels first.

## 12. Terminology mapping dictionary
| Existing Concept | Synaptix-facing Concept |
|---|---|
| Trivia Tycoon | Synaptix |
| Trivia game | cognitive challenge platform / play platform |
| Game menu | Synaptix Hub |
| Leaderboard | Arena Ladder / Leaderboard |
| Tier rank | Arena Tier / Division Rank |
| Skill tree | Pathways / Neural Pathways |
| Arcade | Labs |
| Mini-games | Training Modules / Labs Challenges |
| Missions | Missions / Signals / Milestones |
| Profile progression | Journey |
| Friends / groups | Circles |
| Admin dashboard | Synaptix Command |
| Power-ups | Enhancements |
| Gems | Synapse Shards |
| XP | Neural XP |
| Store items | Enhancements / Unlocks / Packs |

## 13. Theme and component implementation blueprint
### 13.1 New state model
Introduce:
- `SynaptixMode`
- `synaptixModeProvider`
- `synaptixThemeProvider`
- optional `synaptixBrandCopyProvider`

### 13.2 New component family
Recommended components:
- `SynaptixShell`
- `SynaptixHeader`
- `SynaptixCard`
- `SynaptixButton`
- `SynaptixSection`
- `SynaptixStatPill`
- `SynaptixModeBanner`
- `SynaptixHUD`
- `SynaptixEmptyState`
- `SynaptixSkillNode`

### 13.3 Existing token reuse
The implementation AI should reuse:
- spacing from `styles.dart`
- current shadows/corners where still valid
- existing typography scaffolding
- current theme helpers and mappers where useful

### 13.4 Motion language
Recommended motion language:
- signal pulses
- node activation ripple
- subtle energy flow in progress bars
- glow-on-focus for teen mode
- softer bounce/tap feedback for kids mode
- restrained transitions for adult mode

## 14. Skill tree redesign blueprint
### 14.1 Strategic position
The skill tree is not a side feature. Under Synaptix it becomes a central differentiator.

### 14.2 Recommended visual treatment
Combine:
- honeycomb geometry
- spider-web path planning
- neural connection glow
- category-color logic

### 14.3 Interaction requirements
- zoom / pan via `InteractiveViewer`
- tap node to inspect skill
- animate unlock paths
- show branch previews
- allow future respec/reset
- support mode-specific simplification

### 14.4 Data strategy
Keep current `skill_tree_loader`, DTO mappers, providers, and controller contracts if possible.
Add:
- product-facing label map
- icon map
- audience-specific node description map
- category grouping metadata if needed

## 15. Onboarding and profile behavior blueprint
### 15.1 Initial mapping logic
At first launch:
1. capture age group
2. capture interests/categories
3. map to default Synaptix mode
4. recommend one or more starting surfaces

### 15.2 Persisted profile additions
Recommended additions:
- preferred Synaptix mode
- preferred home surface
- onboarding completion version
- last selected theme intensity
- accessibility / reduced motion preference
- optional tone preference: playful / balanced / competitive

### 15.3 Backward compatibility
Any new profile keys should be additive.
Do not remove or repurpose old keys without migration logic.

## 16. Router and navigation blueprint
### 16.1 Router policy
Retain current route structure initially.
Prefer:
- new labels
- new headers
- new nav card titles
- new menu grouping

over:
- route path rewrites
- folder reshuffling
- route constant churn

### 16.2 Proposed user-facing nav model
Potential primary nav groups:
- Home
- Arena
- Labs
- Pathways
- Circles
- Profile

Potential admin group:
- Command

## 17. Asset, splash, and logo plan
### 17.1 Splash
Synaptix splash should support:
- master logo
- optional mode-aware accent treatments
- subtle neural pulse animation
- shorter, cleaner initial copy

### 17.2 Logo system
Recommended hierarchy:
- primary wordmark: Synaptix
- app icon: neural node / S-shaped synapse motif
- mode accent variants:
  - Junior / Play
  - Arena
  - Labs

### 17.3 Asset migration policy
Create a mapping document for:
- old logos
- old background assets that mention trivia explicitly
- badge art
- rank visuals
- splash assets
- store/reward assets

## 18. Copywriting strategy
### 18.1 Tone model
| Mode | Tone |
|---|---|
| kids | encouraging, bright, simple |
| teen | ambitious, energetic, competitive |
| adult | strategic, clean, mastery-oriented |

### 18.2 Copy rules
- avoid overusing “neural/synapse” in every sentence
- preserve familiar labels where clarity matters
- use platform language in headers and marketing surfaces
- keep gameplay language concrete

## 19. Backend and data contract impact
### 19.1 Recommendation
Keep backend contract names stable initially unless the backend is being actively versioned for the rebrand.

### 19.2 Safe first-pass changes
- change DTO display labels in UI
- change endpoint usage copy
- change screen labels
- change analytics event names only if event taxonomy review is planned

### 19.3 Potential future backend alignment
Later, a versioned backend event taxonomy could adopt Synaptix terms for:
- engagement events
- mode selection
- theme mode usage
- pathway unlocks
- arena actions
- labs performance
- circles engagement

## 20. Analytics implications
Recommended new analytics dimensions:
- synaptix_mode
- audience_segment
- home_surface_clicked
- pathway_engagement
- arena_conversion
- labs_retention
- circles_engagement
- premium_theme_usage

## 21. Accessibility and inclusivity requirements
Required:
- strong contrast in all modes
- readable typography at small sizes
- reduced motion support
- kid mode with larger hit targets
- no neon-only communication for state meaning
- support text labels alongside icons
- avoid over-dense dashboards in kids mode

## 22. File and directory recommendations
### 22.1 Additive folders recommended
Suggested additions inside the current `lib` tree:
- `lib/synaptix/brand/`
- `lib/synaptix/theme/`
- `lib/synaptix/widgets/`
- `lib/synaptix/copy/`
- `lib/synaptix/mode/`

### 22.2 Existing files that should become primary touchpoints
- `lib/main.dart`
- `lib/core/navigation/app_router.dart`
- `lib/core/theme/themes.dart`
- `lib/core/theme/styles.dart`
- `lib/core/manager/service_manager.dart`
- `lib/core/services/settings/player_profile_service.dart`
- `lib/screens/menu/game_menu_screen.dart`
- `lib/screens/onboarding/...`
- `lib/screens/leaderboard/...`
- `lib/arcade/ui/screens/arcade_hub_screen.dart`
- `lib/game/controllers/skill_tree_controller.dart`
- `lib/game/providers/skill_tree_provider.dart`
- `lib/screens/profile/...`
- `lib/admin/...`

### 22.3 Files that should probably remain stable in phase 1
- low-level networking
- auth token store
- service initialization contracts
- DTO and repository layers
- question schema validation utilities
- encryption services
- admin backend wiring

## 23. Concrete implementation backlog for the downstream AI
### 23.1 Immediate backlog
1. Build rename dictionary and replacement map
2. Create Synaptix brand tokens and mode enum
3. Add providers for mode + theme
4. Upgrade splash and top-level app branding
5. Replace home/menu hub with Synaptix Hub
6. Update onboarding copy and mode selection logic
7. Re-theme leaderboard into Arena
8. Re-theme arcade into Labs
9. Re-theme skill tree into Pathways
10. Re-theme profile/community into Journey + Circles
11. Re-theme admin into Synaptix Command
12. Add analytics instrumentation for the new surfaces

### 23.2 Secondary backlog
1. Add settings toggle for preferred experience mode
2. Introduce adaptive home layouts per mode
3. Build more coherent economy iconography
4. Align seasonal/theme services with Synaptix branding
5. Add audience-aware empty states and tooltips
6. Create logo/icon asset pipeline
7. Audit rewards/store copy
8. Prepare optional package/import rename plan

## 24. Acceptance criteria
A rebrand implementation should only be considered successful if:

### Product
- the app reads as Synaptix from first launch through core play
- the experience feels coherent across trivia, arcade, profile, leaderboard, and admin
- kids are not alienated by overly technical branding
- teens/adults feel the product is modern and competitive

### System
- no critical regressions to routing, services, or persistence
- age group and profile data continue to load correctly
- existing providers and controllers remain understandable
- analytics events can distinguish old/new surfaces if needed

### UX
- one master brand, multiple presentation layers
- clear nav hierarchy
- visual consistency across major surfaces
- skill tree, arena, and labs look like one product family

## 25. Risks and mitigation
| Risk | Why it matters | Mitigation |
|---|---|---|
| global package rename too early | huge churn | keep `trivia_tycoon` import root initially |
| over-theming kids mode | hurts clarity | prioritize readability and simple UI |
| excessive terminology changes | confuses users | keep usability-first labels where needed |
| admin redesign scope blow-up | delays project | rebrand visuals and IA first, preserve logic |
| duplicate theme systems | tech debt | build adapters and migration notes |
| asset churn | missing references/broken screens | keep old assets until replacements verified |

## 26. Final recommendation
The correct path is:
1. Adopt Synaptix as the product brand now
2. Implement a layered rebrand using the existing app architecture
3. Use age group + profile state to drive audience-aware presentation
4. Reposition leaderboard, arcade, and skill tree as Arena, Labs, and Pathways
5. Delay deep namespace/package renames until the product layer is stable

## 27. Implementation note for the next AI system
The next AI system should treat this blueprint as a platform migration plan, not a simple theme pass.

Priority order:
1. protect functional stability
2. establish Synaptix master brand
3. implement audience-aware theming
4. modernize home/menu and skill-tree surfaces
5. align naming and copy across all major flows
6. only then consider deep file/package cleanup

## Appendix A - concise file relevance map
| File / area | Rebrand importance | Reason |
|---|---|---|
| `main.dart` | critical | first-run init, age group bootstrap, splash handoff |
| `core/navigation/app_router.dart` | critical | top-level information architecture |
| `core/theme/themes.dart` | critical | current theme foundation |
| `core/theme/styles.dart` | high | shared tokens |
| `core/manager/service_manager.dart` | high | service dependency map |
| `core/services/settings/player_profile_service.dart` | critical | age group and profile persistence |
| `screens/menu/game_menu_screen.dart` | critical | should become Synaptix Hub |
| `screens/onboarding/...` | critical | audience segmentation and framing |
| `screens/leaderboard/...` | high | Arena transformation |
| `arcade/...` | high | Labs transformation |
| `game/controllers/skill_tree_controller.dart` | critical | Pathways transformation |
| `screens/profile/...` | high | Journey / Circles transformation |
| `admin/...` | high | Command rebrand |

## Appendix B - recommended deliverables from the next AI system
The next AI system should ideally produce:
- a rename matrix
- a file-by-file implementation plan
- a mode-aware theme architecture patch
- updated home/menu design
- updated onboarding copy flow
- Arena/Labs/Pathways UI specs
- Synaptix logo and splash direction
- migration notes for package/path renames
- testing checklist for route/theme/profile regression
