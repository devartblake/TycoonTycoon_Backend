# Trivia Tycoon / Synaptix Migration Package — File-by-File Patch Order

This patch order is sequenced to reduce breakage while you move from legacy quiz semantics toward a clean Play / Learn / Study split.

## Status update
The backend has now passed the migration threshold for legacy quiz transport:

- `/questions/*` is the supported gameplay question API
- `/modules/*` is the supported learning API
- `/study-sets/*` and `/study-sessions/*` now exist as the dedicated Study API family
- `/quiz/*` is no longer mapped in the backend API

From this point forward, patches should assume **option 2** from the migration decision:
remove remaining frontend and documentation dependence on `/quiz/*` rather than restoring a backend compatibility shim.

## Guiding rule
Patch transport and routing seams first, then screen launchers, then new surfaces, then deprecations.

## Phase 0 — Freeze and annotate contracts

### 0.1 Backend contract comments and API notes
- `Tycoon.Backend.Api/Features/Questions/QuestionsEndpoints.cs`
- `Tycoon.Backend.Api/Features/LearningModules/LearningModulesEndpoints.cs`
- `Tycoon.Backend.Api/Program.cs`

**Patch goal**
- Add explicit comments that `/questions` is the gameplay question API.
- Add explicit comments that `/modules` is the learning API.
- Mark `/quiz` as removed from the backend and reserved only as legacy terminology in docs/frontend cleanup.

**Why first**
- It prevents future frontend code from reintroducing ambiguity.

## Phase 1 — Canonicalize frontend transport

### 1.1 Remove direct legacy fetches from transport utilities
- `lib/core/services/api_service.dart`
- `lib/core/networking/tycoon_api_client.dart`

**Patch goal**
- Stop exposing `/quiz/play` as the default gameplay fetch contract.
- Either remove these methods or mark them legacy-only.
- Introduce comments pointing gameplay loading toward `QuestionHubService`.

### 1.2 Harden the canonical orchestration service
- `lib/game/services/question_hub_service.dart`
- `lib/game/repositories/question_repository_impl.dart`
- `lib/game/providers/question_providers.dart`

**Patch goal**
- Keep `/questions/set` as the primary path.
- Remove legacy `/quiz/play` fallback entirely or quarantine it behind a dead-code cleanup path scheduled for deletion.
- Normalize response mapping and add source telemetry.

**Exit criterion**
- Every gameplay fetch path can be traced through `QuestionHubService`.

## Phase 2 — Remove duplicate question-screen ownership

### 2.1 Collapse duplicate screen definitions
- `lib/screens/question/adapted_question_screen.dart`
- `lib/screens/question/question_view_screen.dart`

**Patch goal**
- Keep one canonical `AdaptedQuestionScreen`.
- Convert the other file into either a wrapper, redirect import, or delete target.

### 2.2 Update router imports and builders
- `lib/core/navigation/app_router.dart`

**Patch goal**
- Ensure all question play builders import and instantiate the same screen class.

**Exit criterion**
- Only one runtime implementation owns the gameplay question UI.

## Phase 3 — Split player-facing route semantics

### 3.1 Introduce Play-first route naming
- `lib/core/navigation/app_router.dart`

**Patch goal**
- Add canonical routes such as `/play`, `/play/question`, or `/play/start/:gameMode`.
- Add canonical routes such as `/play`, `/play/question`, or `/play/start/:gameMode`.
- Leave `/learn-hub/...` intact.
- Reserve `/study` or `/study-hub` for the Quizlet-like flow.

### 3.2 Update menu entry points
- `lib/screens/menu/game_menu_screen.dart`
- Any top-menu widgets or feature cards that still promote generic “Quiz” language.

**Patch goal**
- Main navigation should read as Play / Learn / Study.
- Old quiz labels should be transitional and internal where needed.

## Phase 4 — Rewire gameplay launchers

### 4.1 Standardize launchers onto canonical play routes
- `lib/screens/question/question_screen.dart`
- `lib/screens/question/categories/category_quiz_screen.dart`
- `lib/screens/question/categories/class_quiz_screen.dart`
- `lib/screens/question/categories/daily_quiz_screen.dart`
- `lib/screens/question/score_summary_screen.dart`
- `lib/screens/multiplayer/multiplayer_game_matchmaking_screen.dart`

**Patch goal**
- Replace any remaining direct pushes/go calls to `/quiz/play` with the canonical play route.
- Pass one consistent launch payload shape for category, difficulty, count, and mode.

**Exit criterion**
- Gameplay entry is route-consistent across daily, class, category, replay, and multiplayer flows.

## Phase 5 — Align question and learning models

### 5.1 Distinguish gameplay question DTOs from learning lesson DTOs
- `lib/core/dto/learning_dto.dart`
- `lib/core/services/question/question_api_service.dart`
- `lib/core/services/question/question_service.dart`
- relevant gameplay models under `lib/game/...`

**Patch goal**
- Gameplay models should not assume correct answers are present in payloads.
- Learning lesson models should support correct answer, explanations, and ordered lesson context.

### 5.2 Add backend DTO clarity if needed
- `Tycoon.Shared.Contracts.Dtos/*Question*`
- `Tycoon.Shared.Contracts.Dtos/*Learning*`

**Patch goal**
- Make the transport difference explicit so the frontend is not forced to infer mode from shape drift.

## Phase 6 — Introduce Study as a first-class product surface

### 6.1 Backend Study API scaffolding
- `Tycoon.Backend.Api/Features/StudySets/*`
- matching application and DTO layers

**Patch goal**
- Create a dedicated surface for flashcards, self-test, favorites, weak-area practice, or custom sets.

**Status update**
- Backend Study scaffolding is now in place and includes:
  - generated category / weak-area / favorites / due-review sets
  - custom saved study sets
  - resumable study sessions
  - flashcard/self-test session mode persistence
  - explicit flashcard interaction state persistence
- Remaining work in this phase is frontend Study hub and route adoption.

### 6.2 Frontend Study hub scaffolding
- `lib/screens/study_hub/*`
- `lib/core/navigation/app_router.dart`
- `lib/screens/menu/game_menu_screen.dart`

**Patch goal**
- Add a Study entry point distinct from Play and Learn.
- Start with placeholder deck/session screens if backend is not finished yet.

## Phase 7 — Instrument the migration

### 7.1 Frontend telemetry
- `lib/game/services/question_hub_service.dart`
- `lib/core/manager/analytics/analytics_stream_manager.dart`

**Patch goal**
- Log mode entry: Play, Learn, Study.
- Log any stale frontend attempt to use removed legacy quiz flows as client-side migration debt, not as a live backend route family.

### 7.2 Backend telemetry
- analytics or logging pipeline in the backend

**Patch goal**
- Measure traffic split across `/questions`, `/modules`, and future `/study-*` surfaces.
- Do not model `/quiz/*` as an active backend traffic family in dashboards or route ownership notes.

## Phase 8 — Deprecate legacy quiz surfaces

### 8.1 Frontend deprecation
- `lib/core/navigation/app_router.dart`
- legacy quiz discovery and launcher screens under `lib/screens/question/*`

**Patch goal**
- Redirect or remove ambiguous quiz entry points once telemetry shows safe adoption.

### 8.2 Backend cleanup confirmation
- `Tycoon.Backend.Api/Program.cs`
- question/learning contract docs

**Patch goal**
- Keep `/quiz` removed from supported backend gameplay contracts.
- Prevent docs or comments from implying a still-supported backend compatibility facade unless a future Study surface is intentionally introduced under its own route family.

## Patch order summary
1. Backend comments and route intent annotations
2. `api_service.dart` and `tycoon_api_client.dart`
3. `question_hub_service.dart` + repository/provider cleanup
4. duplicate `AdaptedQuestionScreen` removal
5. `app_router.dart` canonical Play / Learn / Study routes
6. gameplay launcher rewiring
7. DTO and mapping alignment
8. Study hub backend + frontend scaffolding
9. telemetry
10. legacy quiz reference sunset

## Highest-risk files
- `lib/core/navigation/app_router.dart`
- `lib/game/services/question_hub_service.dart`
- `lib/core/services/api_service.dart`
- `lib/core/networking/tycoon_api_client.dart`
- `lib/screens/question/adapted_question_screen.dart`
- `lib/screens/question/question_view_screen.dart`

## Suggested guardrails
- Do not reintroduce `/quiz/play` unless a new explicit ADR chooses a compatibility facade.
- Remove fallback logic rather than preserving it indefinitely.
- Avoid renaming DTOs and routes in the same commit as major UI restructuring.
- Land router changes before broad launcher rewrites so redirects can protect older screens.
