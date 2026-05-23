# Synaptix Packet C Implementation Guide
## Phases 4-5: Core Feature Surface Rebrand + Backend Product-Language Alignment

**Packet scope:** convert the major feature surfaces into the Synaptix platform vocabulary and align backend-visible language without destabilizing contracts  
**Covers:** Phase 4 and Phase 5  
**Assumption:** Packet B is complete or sufficiently in place  
**Do not touch yet:** package roots, backend namespaces, DTO names, API routes, database tables, migrations, deep project/file renames

---

## 1. Packet C purpose

Packet C is where the product starts to feel fully rebranded.

Packet B created the foundation:
- Synaptix brand shell
- Synaptix mode/theming foundation
- Synaptix Hub
- stable router posture

Packet C converts the **major product surfaces** into the Synaptix vocabulary and then aligns backend-visible language so the platform reads consistently across:
- app UI
- admin/operator dashboards
- API docs
- product-facing backend descriptions

This packet should make the product feel like one coherent system rather than a renamed shell sitting on top of old feature names.

---

## 2. Packet C success criteria

Packet C is successful when all of the following are true:

- leaderboard/rank surfaces visibly read as **Arena**
- arcade and mini-game surfaces visibly read as **Labs**
- skill tree visibly reads as **Pathways**
- profile progression visually reads as **Journey**
- social/grouping surfaces can be understood under **Circles**
- admin/operator surfaces visibly read as **Command**
- backend-visible product language no longer feels disconnected from frontend language
- no route, DTO, or namespace churn has been introduced

---

## 3. Phase 4 - Core Feature Surface Rebrand

### 3.1 Objective
Convert the major product surfaces into Synaptix-facing feature identities while preserving their logic, routes, controllers, and data contracts.

### 3.2 Surfaces in scope
1. Leaderboards and rank -> Arena
2. Arcade and mini-games -> Labs
3. Skill tree -> Pathways
4. Profile progression -> Journey
5. Messaging and group surfaces -> Circles
6. Admin -> Command

### 3.3 Core rule
**Rename surfaces, not systems.**

At this stage:
- rename UI labels
- rename screen headers
- rename card titles
- rename top-level section framing
- rename product-facing descriptions

Do **not**:
- rename controllers unless there is no risk
- rename routes
- rename providers
- rename DTOs
- rename backend namespaces
- rename persistence keys

---

## 4. Phase 4 file targets

### Frontend - priority targets
- `lib/screens/leaderboard/...`
- `lib/arcade/...`
- `lib/screens/mini_games/...`
- `lib/game/controllers/skill_tree_controller.dart`
- `lib/game/providers/skill_tree_provider.dart`
- `lib/game/data/skill_tree_loader.dart`
- `lib/screens/profile/...`
- `lib/screens/messages/...`
- `lib/screens/group_chat/...`
- `lib/admin/...`

### Frontend - supporting files
- shell cards in Synaptix Hub
- feature-launch labels
- progress and rewards widgets
- empty states and section headers

### Backend-visible targets
- Swagger/OpenAPI descriptions
- operator dashboard headings
- Vue/web dashboard visible labels
- backend-facing docs that describe the product areas

---

## 5. Phase 4A - Arena rebrand

### 5.1 Product role
Arena is the competitive face of Synaptix:
- leaderboard
- rank
- tier
- standings
- progression through competition

### 5.2 Frontend target files
- `lib/screens/leaderboard/leaderboard_screen.dart`
- `lib/screens/leaderboard/tier_rank_screen.dart`
- `lib/screens/leaderboard/widgets/...`
- any shell card or home-launch labels that currently say leaderboard/rank

### 5.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Leaderboard | Arena / Leaderboard |
| Rank | Tier / Standing |
| Tier Rank | Arena Tier / Division Rank |
| Top Players | Top Players / Arena Leaders |

### 5.4 Labeling rule
Keep “Leaderboard” where users need immediate clarity, especially inside tables and rankings.  
Use “Arena” for:
- headers
- shell cards
- page framing
- platform language

### 5.5 Example header patch
```diff
- Text('Leaderboard')
+ Text('Arena')
```

### 5.6 Example card patch
```diff
- subtitle: 'See who is on top'
+ subtitle: 'Climb the Arena ladder'
```

### 5.7 What not to change
- sorting logic
- filtering logic
- controller logic
- provider names
- rank calculations

### 5.8 Deliverables
- competitive surfaces visually read as Arena
- tier and ladder language feels more intentional
- no underlying ranking logic is disturbed

---

## 6. Phase 4B - Labs rebrand

### 6.1 Product role
Labs is the practice, experimentation, and mini-game surface of Synaptix.

### 6.2 Frontend target files
- `lib/arcade/...`
- `lib/screens/mini_games/...`
- hub launch cards that say Arcade or Mini Games
- daily activity surfaces that belong to practice/training loops

### 6.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Arcade | Labs |
| Mini Games | Labs Challenges / Training Modules |
| Daily Bonus | Daily Signal / Daily Reward |
| Local Leaderboard | Practice Board / Labs Leaderboard |

### 6.4 Rule
Use “Labs” for:
- surface framing
- shell cards
- section headers

Keep “Mini Game” if needed in lower-level UX where clarity matters.

### 6.5 Example header patch
```diff
- Text('Arcade')
+ Text('Labs')
```

### 6.6 Example supporting copy
```diff
- 'Play quick mini-games and earn rewards.'
+ 'Train, experiment, and improve through Labs challenges.'
```

### 6.7 What not to change
- mini-game logic
- score calculation
- arcade services/providers
- reward logic unless the label is purely visible

### 6.8 Deliverables
- practice surfaces feel native to Synaptix
- arcade/mini-game systems no longer feel like a separate product

---

## 7. Phase 4C - Pathways rebrand

### 7.1 Product role
Pathways is the progression flagship of Synaptix.

This is one of the highest-value rebrand targets because the skill tree already aligns naturally with the Synaptix identity.

### 7.2 Frontend target files
- `lib/game/controllers/skill_tree_controller.dart`
- `lib/game/providers/skill_tree_provider.dart`
- `lib/game/data/skill_tree_loader.dart`
- `lib/game/models/skill_tree_graph.dart`
- skill-tree UI widgets and any surface labels that say skill tree

### 7.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Skill Tree | Pathways / Neural Pathways |
| Skill | Node / Path |
| Unlock | Activate |
| Upgrade | Enhance |
| Branch | Track |
| Skill Category | Pathway Track |

### 7.4 Implementation stance
Do not rewrite the graph model in this phase.
Do:
- change headers
- change product-facing labels
- change descriptions
- add label mappers where needed
- preserve graph logic and loaders

### 7.5 Recommended branch labels
- Cognition
- Strategy
- Momentum
- Recall
- Precision
- Insight
- Support
- Enhancements

### 7.6 Example UI patch
```diff
- Text('Skill Tree')
+ Text('Pathways')
```

### 7.7 Example provider helper
```dart
String toSynaptixPathLabel(String internalName) {
  return pathwayDisplayMap[internalName] ?? internalName;
}
```

### 7.8 What not to change
- core node-unlock logic
- graph structure
- loader contracts
- persistence unless labels are separate from stored values

### 7.9 Deliverables
- the progression system visibly becomes Pathways
- the feature feels central to the Synaptix identity
- no graph/controller regressions are introduced

---

## 8. Phase 4D - Journey rebrand

### 8.1 Product role
Journey is the player-facing progression and identity frame around profile data.

### 8.2 Frontend target files
- `lib/screens/profile/...`
- profile header widgets
- stats/performance tiles
- reward/badge/milestone surfaces attached to profile

### 8.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Profile | Journey |
| Stats | Performance |
| Achievements | Milestones |
| Progress | Journey Progress |

### 8.4 Rule
Keep “Profile” where the UX needs conventional clarity, but use “Journey” for:
- section framing
- shell card label
- page headers
- progress messaging

### 8.5 Example header patch
```diff
- Text('Profile')
+ Text('Journey')
```

### 8.6 Deliverables
- profile area feels growth-oriented, not just informational
- profile/identity surfaces align with the platform theme of mastery

---

## 9. Phase 4E - Circles rebrand

### 9.1 Product role
Circles is the umbrella social/community frame.

### 9.2 Frontend target files
- `lib/screens/messages/...`
- `lib/screens/group_chat/...`
- any shell labels or nav groupings for social features

### 9.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Messages | Messages |
| Group Chat | Group Chat / Circles |
| Friends | Circles |
| Groups | Circles |

### 9.4 Rule
Keep conventional low-level labels:
- Messages
- Chats
- Groups

But place them under a visible Circles umbrella in:
- shell cards
- section grouping
- nav framing
- headers where appropriate

### 9.5 Example shell-card patch
```diff
- title: 'Messages'
+ title: 'Circles'
- subtitle: 'Chat with friends'
+ subtitle: 'Connect with your circles'
```

### 9.6 Deliverables
- social surfaces feel integrated into Synaptix rather than bolted on
- usability stays intact because direct labels remain familiar where necessary

---

## 10. Phase 4F - Command rebrand

### 10.1 Product role
Command is the operator/admin frame for the platform.

### 10.2 Frontend/admin target files
- `lib/admin/...`
- admin dashboard shell widgets
- admin section labels
- admin entry cards/buttons

### 10.3 Display mapping
| Current | Synaptix-facing |
|---|---|
| Admin Dashboard | Synaptix Command |
| Admin | Command |
| Analytics | Analytics |
| Notifications | Notifications |
| Content | Content |
| Security | Security |

### 10.4 Rule
Do not over-brand the inner admin tools.  
The top-level shell should say Synaptix Command.  
Inner tools can remain conventional for clarity.

### 10.5 Example header patch
```diff
- Text('Admin Dashboard')
+ Text('Synaptix Command')
```

### 10.6 What not to change
- admin service wiring
- backend calls
- encryption logic
- import/export logic
- analytics calculations

### 10.7 Deliverables
- admin/operator surfaces align with the product family
- no admin functionality is destabilized

---

## 11. Phase 4 implementation order

Another AI should execute Phase 4 in this order:

1. update shell launch labels in Synaptix Hub
2. Arena headers and cards
3. Labs headers and cards
4. Pathways headers and display label mappers
5. Journey framing in profile
6. Circles framing in social surfaces
7. Command shell branding in admin
8. empty states, subtitles, and secondary copy pass
9. consistency scan across adjacent screens

---

## 12. Phase 4 deliverables

At the end of Phase 4, another AI should be able to say:

- Synaptix Hub launches Arena, Labs, Pathways, Journey, Circles, and Command
- each surface visibly reads as part of the same product family
- low-level logic is intact
- no route/path/provider namespace churn occurred
- the app’s feature map now matches the Synaptix brand language

---

## 13. Phase 4 risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| over-renaming conventional labels | hurts usability | keep familiar labels where users need them |
| changing logic instead of labels | creates regressions | restrict work to display layer unless absolutely necessary |
| mixed old/new language across neighboring screens | weakens coherence | run consistency pass after each surface |
| over-branding admin inner tools | hurts operator usability | brand the shell, keep tools conventional |

---

## 14. Phase 4 exit criteria

Phase 4 is complete when:
- Arena, Labs, Pathways, Journey, Circles, and Command all exist visibly
- shell launch cards use the new language
- adjacent screens do not have obvious mixed branding
- underlying route, provider, and data logic remain stable

---

## 15. Phase 5 - Backend Product-Language Alignment

### 15.1 Objective
Align the backend’s product-facing language with Synaptix without doing a deep technical rename.

### 15.2 Scope
This phase affects what people see in:
- API docs
- dashboards
- operator surfaces
- product docs/readmes used in practice
- analytics/admin wording

It does **not** perform:
- namespace renaming
- endpoint path changes
- DTO renaming
- solution/project renaming
- migration renaming

---

## 16. Phase 5 backend targets

### Primary targets
- Swagger/OpenAPI configuration
- `Tycoon.OperatorDashboard/...`
- `Synaptix.OperatorDashboard.Vue/...`
- `Tycoon.OperatorDashboard.Web/...`
- backend docs headings that operators or implementation AIs use directly

### Secondary targets
- event taxonomy docs
- admin analytics labels
- internal docs that describe the platform surfaces

---

## 17. Phase 5A - Swagger/OpenAPI branding

### 17.1 Goal
Make the API documentation visibly align with Synaptix.

### 17.2 Example target
```csharp
options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Synaptix API",
    Version = "v1",
    Description = "Platform API for Synaptix gameplay, progression, live competition, missions, and player systems."
});
```

### 17.3 Additional doc alignment
Where feature summaries or tag descriptions are visible, use platform-facing language such as:
- Arena
- Labs
- Pathways
- Circles
- Command

without changing endpoint names or code namespaces.

### 17.4 Deliverables
- API docs no longer feel visually disconnected from the app rebrand

---

## 18. Phase 5B - Operator dashboard alignment

### 18.1 Goal
Align operator-facing surfaces with the product’s new vocabulary.

### 18.2 Blazor dashboard
Update:
- top shell heading -> Synaptix Command
- nav grouping labels
- dashboard landing copy
- top-level section framing

Keep:
- routes
- services
- auth wiring
- project names
- namespaces

### 18.3 Vue/web dashboard
Update:
- shell title
- sidebar labels where product-facing
- dashboard welcome copy
- visible group headings

Do not:
- refactor the dashboard framework
- replace layout systems
- rename project roots

### 18.4 Deliverables
- operator surfaces read as part of Synaptix rather than a separate Tycoon-branded tool

---

## 19. Phase 5C - Backend docs and descriptions

### 19.1 Goal
Align product-facing descriptions without destabilizing technical docs.

### 19.2 Recommended updates
- project-level headings that reference the product
- implementation docs that describe feature areas
- operator-facing setup docs
- readme sections where the product is described at a business level

### 19.3 Rule
If the text is implementation-critical and tied to code naming, leave it alone unless clarity improves.  
If the text is product-facing or operator-facing, align it to Synaptix.

---

## 20. Phase 5D - Analytics/admin terminology alignment

### 20.1 Goal
Make backend-facing analytics wording match what the frontend now calls the surfaces.

### 20.2 Examples
- “Leaderboard events” can be described as Arena events in dashboards or docs
- “Arcade usage” can be described as Labs usage in visible reports
- “Skill tree interactions” can be described as Pathways interactions in admin-facing language

### 20.3 Rule
This is a display-language alignment pass, not an event-schema rewrite.
Event schema work belongs more naturally in Packet D.

---

## 21. Phase 5 deliverables

At the end of Phase 5, another AI should be able to say:

- frontend and backend-facing product language match
- Swagger docs visibly say Synaptix API
- operator dashboards read as Synaptix Command
- backend docs do not undermine the frontend rebrand
- no endpoint or namespace churn has been introduced

---

## 22. Phase 5 risks

| Risk | Why it matters | Mitigation |
|---|---|---|
| renaming technical identifiers during docs pass | creates unnecessary breakage | limit to visible copy and titles |
| incomplete dashboard alignment | makes backend feel disconnected | do shell/header/nav pass on all operator surfaces |
| over-editing implementation docs | reduces technical clarity | only align product-facing descriptions where safe |
| starting telemetry schema work too early | increases scope | keep Phase 5 focused on visible language only |

---

## 23. Phase 5 exit criteria

Phase 5 is complete when:
- Swagger/OpenAPI title and major descriptions align with Synaptix
- operator dashboards visibly use Synaptix Command framing
- backend docs no longer contradict the frontend rebrand
- no deep backend rename has been attempted

---

## 24. Packet C implementation order

Another AI should execute Packet C in this exact order:

1. update Synaptix Hub launch labels
2. rebrand Arena surfaces
3. rebrand Labs surfaces
4. rebrand Pathways surfaces
5. rebrand Journey framing
6. rebrand Circles framing
7. rebrand Command shell in frontend admin
8. scan for adjacent copy consistency
9. update Swagger/OpenAPI title and descriptions
10. update Blazor dashboard shell branding
11. update Vue/web dashboard shell branding
12. align backend-facing docs and operator copy
13. do a final frontend/backend terminology consistency pass

---

## 25. Packet C handoff checklist

Before moving to Packet D, confirm:

- Arena is visible
- Labs is visible
- Pathways is visible
- Journey framing exists
- Circles framing exists
- Command framing exists
- Swagger says Synaptix API
- operator dashboards use Synaptix Command
- no route paths were changed
- no package roots or namespaces were renamed

---

## 26. Recommended next packet after C

Once Packet C is complete, move to **Packet D**:
- Phase 6: Analytics and Telemetry Upgrade
- Phase 7: Stabilization and Regression Hardening

That packet is where the rebrand becomes measurable, consistent, and release-ready.
