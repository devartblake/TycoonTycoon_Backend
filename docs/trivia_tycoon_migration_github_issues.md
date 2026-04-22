# Trivia Tycoon / Synaptix Migration Package - GitHub Issues Bundle

This package converts the migration recommendation into ready-to-create issue records.

## Status update
The backend migration threshold has been reached:

- `/questions/*` is the live gameplay question contract
- `/modules/*` is the live learning contract
- `/quiz/*` is no longer exposed by the backend API

The issues below should therefore be interpreted as cleanup and forward-migration work, not a recommendation to restore a backend `/quiz` shim.

## Recommended milestones
- **Quiz/Question/Learning Migration**
- **Study Surface Introduction**
- **Legacy Quiz Sunset**

## Suggested sprint framing
- **Sprint 1:** Stabilize canonical contracts and remove ambiguous frontend usage.
- **Sprint 2:** Introduce Study surface and align DTOs/telemetry.
- **Sprint 3:** Sunset remaining legacy quiz references and frontend route debt.

## Issues

### TT-MIG-001 - Deprecate direct /quiz/play usage in frontend transport layer
- Type: frontend
- Priority: P0
- Sprint: Sprint 1
- Effort: M
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: migration, frontend, api-contract, legacy-cleanup
- Summary: Remove direct legacy gameplay fetches from ApiService and TycoonApiClient so QuestionHubService becomes the canonical gameplay question transport.
- Acceptance criteria:
  - No new gameplay screen calls `/quiz/play` directly through ApiService or TycoonApiClient.
  - QuestionHubService remains the only approved gameplay question fetch path.
  - Any legacy fallback code is removed or explicitly marked for deletion.
  - Telemetry differentiates `/questions/set` success from local fallback or stale-route handling if still present.
- Target files:
  - `lib/core/services/api_service.dart`
  - `lib/core/networking/tycoon_api_client.dart`
  - `lib/game/services/question_hub_service.dart`
- Dependencies: None

### TT-MIG-002 - Create explicit frontend route taxonomy for Play, Learn, and Study
- Type: frontend
- Priority: P0
- Sprint: Sprint 1
- Effort: M
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: navigation, frontend, ux, migration
- Summary: Rename player-facing navigation so formal gameplay is presented as Play, learning stays Learn, and Quizlet-like behavior moves toward Study.
- Acceptance criteria:
  - Router distinguishes gameplay routes from learning routes and legacy quiz routes.
  - Primary entry points use Play/Learn/Study language instead of overloading Quiz.
  - Legacy quiz naming is removed or clearly marked transitional in frontend-only code paths.
- Target files:
  - `lib/core/navigation/app_router.dart`
  - `lib/screens/menu/game_menu_screen.dart`
- Dependencies: TT-MIG-001

### TT-MIG-003 - Unify duplicated AdaptedQuestionScreen implementations
- Type: frontend
- Priority: P0
- Sprint: Sprint 1
- Effort: S
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: frontend, cleanup, question-flow
- Summary: Collapse the two AdaptedQuestionScreen definitions into a single canonical screen and keep one import path.
- Acceptance criteria:
  - Only one canonical AdaptedQuestionScreen class remains.
  - `app_router.dart` references a single screen file.
  - No duplicate symbols or drift between question presentation variants.
- Target files:
  - `lib/screens/question/adapted_question_screen.dart`
  - `lib/screens/question/question_view_screen.dart`
  - `lib/core/navigation/app_router.dart`
- Dependencies: None

### TT-MIG-004 - Refactor gameplay launchers to use canonical play flow
- Type: frontend
- Priority: P0
- Sprint: Sprint 1
- Effort: M
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: frontend, play-mode, routing
- Summary: Update all category, class, daily, and summary launchers so they enter the new play flow rather than hardcoding legacy quiz play routes.
- Acceptance criteria:
  - Category, class, daily, multiplayer, and replay actions all resolve through the canonical play route.
  - Launcher screens pass consistent payloads for category, difficulty, count, and mode.
  - No launcher directly depends on legacy naming semantics.
- Target files:
  - `lib/screens/question/question_screen.dart`
  - `lib/screens/question/categories/category_quiz_screen.dart`
  - `lib/screens/question/categories/class_quiz_screen.dart`
  - `lib/screens/question/categories/daily_quiz_screen.dart`
  - `lib/screens/question/score_summary_screen.dart`
  - `lib/screens/multiplayer/multiplayer_game_matchmaking_screen.dart`
- Dependencies: TT-MIG-002, TT-MIG-003

### TT-MIG-005 - Harden QuestionHubService as the canonical gameplay question orchestrator
- Type: cross-stack
- Priority: P0
- Sprint: Sprint 1
- Effort: M
- Owner: frontend+backend
- Milestone: Quiz/Question/Learning Migration
- Labels: cross-stack, questions, service-layer
- Summary: Make QuestionHubService the only orchestrator for gameplay question retrieval, normalization, and telemetry, with no backend `/quiz` dependency.
- Acceptance criteria:
  - QuestionHubService normalizes `/questions/set` responses for all play modes.
  - Any remaining fallback logic is explicit, temporary, and does not depend on backend `/quiz` routes.
  - Repository/provider layer consumes only the canonical service interface.
- Target files:
  - `lib/game/services/question_hub_service.dart`
  - `lib/game/repositories/question_repository_impl.dart`
  - `lib/game/providers/question_providers.dart`
- Dependencies: TT-MIG-001

### TT-MIG-006 - Formalize backend endpoint roles for Questions, Learning Modules, and removed legacy Quiz
- Type: backend
- Priority: P0
- Sprint: Sprint 1
- Effort: S
- Owner: backend
- Milestone: Quiz/Question/Learning Migration
- Labels: backend, api-contract, architecture
- Summary: Document and enforce the role of `/questions` for gameplay content, `/modules` for learning content, and `/quiz` as removed legacy backend surface rather than an active contract.
- Acceptance criteria:
  - Endpoint intent is documented in code comments and API docs.
  - `/questions` remains the gameplay content API and `/modules` remains the learning API.
  - `/quiz` is explicitly marked removed from the backend API, with no ambiguous ownership.
- Target files:
  - `Tycoon.Backend.Api/Features/Questions/QuestionsEndpoints.cs`
  - `Tycoon.Backend.Api/Features/LearningModules/LearningModulesEndpoints.cs`
  - `Tycoon.Backend.Api/Program.cs`
- Dependencies: None

### TT-MIG-007 - Add backend study-set surface for Quizlet-like behavior
- Type: backend
- Priority: P1
- Sprint: Sprint 2
- Effort: L
- Owner: backend
- Milestone: Quiz/Question/Learning Migration
- Labels: backend, study, new-feature, api
- Summary: Introduce a dedicated study surface for flashcards, self-test sessions, or custom sets instead of overloading gameplay quiz endpoints.
- Acceptance criteria:
  - A new route family exists for study sets or study sessions.
  - Study responses may expose answer/explanation data appropriate for self-paced practice.
  - Gameplay and study APIs no longer share ambiguous naming.
- Target files:
  - `Tycoon.Backend.Api/Features/StudySets/*`
  - `Tycoon.Backend.Application/*Study*`
  - `Tycoon.Shared.Contracts.Dtos/*Study*`
- Dependencies: TT-MIG-006

### TT-MIG-008 - Create frontend Study hub and placeholder deck flows
- Type: frontend
- Priority: P1
- Sprint: Sprint 2
- Effort: L
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: frontend, study, new-feature, ux
- Summary: Add a dedicated Study surface for future flashcards, custom sets, favorites, and weak-area practice.
- Acceptance criteria:
  - App router exposes Study entry points separately from Play and Learn.
  - UI scaffolding supports future study-set browsing and session launch.
  - Legacy quiz discovery screens are either redirected or clearly labeled as transitional.
- Target files:
  - `lib/core/navigation/app_router.dart`
  - `lib/screens/study_hub/*`
  - `lib/screens/menu/game_menu_screen.dart`
- Dependencies: TT-MIG-002, TT-MIG-007

### TT-MIG-009 - Align DTOs and mappers across gameplay and learning question shapes
- Type: cross-stack
- Priority: P1
- Sprint: Sprint 2
- Effort: M
- Owner: frontend+backend
- Milestone: Quiz/Question/Learning Migration
- Labels: cross-stack, dto, contracts
- Summary: Ensure the frontend has distinct but compatible models for gameplay questions versus learning lesson questions with explanations and answers exposed.
- Acceptance criteria:
  - Gameplay DTOs do not require correct answers in payloads.
  - Learning DTOs support correct answers, explanations, and ordering.
  - Frontend mapping layer makes the mode distinction explicit.
- Target files:
  - `lib/core/dto/learning_dto.dart`
  - `lib/game/models/*question*`
  - `Tycoon.Shared.Contracts.Dtos/*Question*`
  - `Tycoon.Shared.Contracts.Dtos/*Learning*`
- Dependencies: TT-MIG-006

### TT-MIG-010 - Add telemetry for route usage, fallback activation, and mode entry
- Type: cross-stack
- Priority: P1
- Sprint: Sprint 2
- Effort: S
- Owner: frontend+backend
- Milestone: Quiz/Question/Learning Migration
- Labels: analytics, cross-stack, migration
- Summary: Instrument the migration so you can observe remaining frontend legacy semantics and where users enter Play, Learn, and Study.
- Acceptance criteria:
  - Frontend emits entry events for Play, Learn, and Study.
  - QuestionHubService records stale-route or local-fallback activations if they still exist.
  - Backend usage dashboards can distinguish `/questions`, `/modules`, and future study traffic without implying `/quiz` is still live.
- Target files:
  - `lib/game/services/question_hub_service.dart`
  - `lib/core/manager/analytics/analytics_stream_manager.dart`
  - `Tycoon.Backend.Api/*analytics*`
- Dependencies: TT-MIG-001, TT-MIG-006

### TT-MIG-011 - Remove or redirect legacy quiz UI after migration cutoff
- Type: frontend
- Priority: P2
- Sprint: Sprint 3
- Effort: M
- Owner: frontend
- Milestone: Quiz/Question/Learning Migration
- Labels: frontend, deprecation, cleanup
- Summary: After telemetry and new surfaces are stable, retire ambiguous quiz-first UI entry points and redirect users to Play or Study equivalents.
- Acceptance criteria:
  - Legacy quiz routes either redirect or are removed.
  - User-facing labels no longer rely on overloaded quiz terminology.
  - Regression tests confirm preserved gameplay and learning access.
- Target files:
  - `lib/core/navigation/app_router.dart`
  - `lib/screens/question/*`
  - `lib/screens/study_hub/*`
- Dependencies: TT-MIG-008, TT-MIG-010

### TT-MIG-012 - Confirm backend /quiz retirement and remove stale references
- Type: backend
- Priority: P2
- Sprint: Sprint 3
- Effort: M
- Owner: backend
- Milestone: Quiz/Question/Learning Migration
- Labels: backend, deprecation, legacy
- Summary: Since frontend dependency has crossed the migration threshold, confirm backend `/quiz` retirement and remove stale docs, tests, and assumptions that still describe it as live.
- Acceptance criteria:
  - No production gameplay depends on `/quiz` endpoints.
  - Docs and handoffs no longer imply `/quiz` is an active backend contract.
  - Backend tests cover the final supported semantics: `/questions/*` for gameplay and `/modules/*` for learning.
- Target files:
  - `docs/*migration*`
  - `Tycoon.Backend.Api/Program.cs`
  - `integration tests`
- Dependencies: TT-MIG-010, TT-MIG-011
