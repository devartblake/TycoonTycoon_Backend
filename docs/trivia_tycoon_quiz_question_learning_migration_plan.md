# Trivia Tycoon / Synaptix Migration Plan

## Purpose

This migration plan turns the current backend/frontend pivot into an executable path. It is designed to help you:

- retire the legacy backend **quiz** surface without letting it re-enter the architecture,
- stabilize **questions** as the canonical gameplay content API,
- keep **learning modules** as the mastery path,
- introduce a future **study / Quizlet-like** surface cleanly,
- reduce frontend contract drift,
- and refactor in phases without breaking current playable flows.

## Status update

The backend has already crossed the migration threshold:

- `QuestionsEndpoints.Map(app)` and `LearningModulesEndpoints.Map(app)` are the active public content surfaces
- `/questions/*` is the live gameplay question contract
- `/modules/*` is the live learning contract
- `/quiz/*` is no longer mapped in `Tycoon.Backend.Api`

From this point forward, the plan assumes **option 2**:
do not restore a backend `/quiz` shim. Remove stale dependencies instead.

---

## 1. Executive recommendation

### Recommended product model

Use four distinct layers going forward:

1. **Questions** = canonical content layer
   Reusable source of question sets, answer checking, and category/difficulty filtering.

2. **Play** = formal gameplay layer
   Competitive quiz matches, sessions, rounds, scoring, rewards, matchmaking, ranked/casual flows.

3. **Learn** = guided mastery layer
   Modules, ordered lessons, explanations, completion rewards, progressive educational flow.

4. **Study** = Quizlet-like rehearsal layer
   Flashcards, self-test decks, favorites, weak-area review, custom sets, practice bundles.

### What should change conceptually

- Do not let `quiz` remain the catch-all term for gameplay, learning, and study.
- `Questions` should be the shared backend data surface.
- `Play` should become the user-facing formal game mode.
- `Learn` should remain module-driven and explanation-friendly.
- `Study` should become the future Quizlet-style area instead of mutating the main gameplay contract.

### Updated transitional rule

The backend legacy `/quiz/*` bridge is considered finished.

- Do not reintroduce `/quiz/*` as a backend compatibility layer unless a new ADR explicitly approves it.
- Treat any remaining `/quiz/*` references as frontend cleanup debt or documentation debt.

---

## 2. Current-state diagnosis

## Backend

The backend endpoint separation is already in the correct state:

- `QuestionsEndpoints` exposes:
  - `GET /questions/set`
  - `POST /questions/check`
  - `POST /questions/check-batch`
- The question set endpoint is gameplay-safe and does not expose correct answers.
- `LearningModulesEndpoints` exposes:
  - `GET /modules`
  - `GET /modules/{id}`
  - `GET /modules/{id}/lessons`
  - `POST /modules/{id}/complete`
- The lessons endpoint intentionally exposes correct answers in a learning context.
- `/quiz/*` is not exposed by the backend API anymore.

## Frontend

The frontend is only partially aligned with that backend shape.

### Good signs

- `QuestionHubService` already prefers `/questions/set`.
- `LearningRepository` is already aligned to `/modules`.
- Router already has `/learn-hub`, module detail, lesson, and complete screens.

### Main problems

1. **Legacy contract assumptions still exist in the transport layer**
   - `ApiService.fetchQuestions()` may still call `/quiz/play`.
   - `TycoonApiClient.getQuizQuestions()` may still call `/quiz/play`.
   - `QuestionHubService` may still carry legacy fallback assumptions.

2. **UI naming still says `quiz` even where it now means play**
   - Primary routes may still use quiz-first naming.
   - Category/class/daily/monthly flows may still launch quiz-named routes.
   - Multiplayer matchmaking may still push quiz-named route builders.

3. **Duplicate adapted question screen implementations**
   - `lib/screens/question/adapted_question_screen.dart`
   - `lib/screens/question/question_view_screen.dart`
   Both define `AdaptedQuestionScreen`, which creates cleanup and routing ambiguity.

4. **Mode boundaries are still unclear**
   - Formal play, question retrieval, and legacy quiz naming are still mixed together.
   - Learn already has a separate surface, but the overall information architecture can still feel quiz-centric.

---

## 3. Target-state architecture

## Domain model

### A. Questions domain
**Purpose:** shared content retrieval and grading

Use for:
- category question sets
- difficulty-based retrieval
- gameplay-safe question DTOs
- answer validation
- future study deck generation

### B. Play domain
**Purpose:** competitive or structured gameplay

Use for:
- single-player runs
- daily challenge
- class/category runs
- ranked/casual/multiplayer sessions
- score submission
- reward calculation

### C. Learn domain
**Purpose:** module-based mastery flow

Use for:
- browse modules
- open module detail
- ordered lessons
- explanations and correct answers
- completion and rewards

### D. Study domain
**Purpose:** Quizlet-like practice and recall

Use for:
- flashcards
- self-test decks
- saved sets
- weak-area review
- favorites review
- teacher/admin-curated study sets

---

## 4. Backend endpoint map

This map separates **canonical** and **future** surfaces. The previous transitional `/quiz` backend surface is now retired.

## 4.1 Canonical backend surfaces

### Questions (canonical gameplay content surface)

#### Keep and standardize
- `GET /questions/set`
  - purpose: retrieve gameplay-safe question sets
  - query examples:
    - `category`
    - `difficulty`
    - `count`
  - should never return correct answers in gameplay mode

- `POST /questions/check`
  - purpose: validate one answer server-side
  - should return correctness and normalized grading result

- `POST /questions/check-batch`
  - purpose: validate multiple answers server-side
  - used for end-of-round or deferred grading workflows

#### Recommended additions
- `GET /questions/categories`
  - purpose: canonical category catalog for play/study/learn filters

- `GET /questions/metadata`
  - purpose: return supported difficulties, languages, tags, availability flags

- `POST /questions/preview-set`
  - purpose: internal/admin or future study-set builder support

### Learning modules (canonical mastery surface)

#### Keep and standardize
- `GET /modules`
  - browse published modules
  - optional `playerId`, `category`, `difficulty`

- `GET /modules/{id}`
  - module overview

- `GET /modules/{id}/lessons`
  - ordered lesson content
  - may include correct answers because it is not competitive play

- `POST /modules/{id}/complete`
  - module completion + reward grant
  - must remain idempotent

#### Recommended additions
- `GET /modules/recommended`
  - personalized next modules

- `GET /modules/progress/{playerId}`
  - module progress summary

- `POST /modules/{id}/lesson/{lessonId}/checkpoint`
  - lesson progress checkpoint if you want granular saves later

## 4.2 Removed legacy backend surface

### Quiz

- `/quiz/*` is not part of the supported backend API
- do not add new gameplay features there
- do not document it as a still-live compatibility surface
- do not make Study depend on reviving the old quiz contract shape

If a future Study product needs its own API, give it its own route family.

## 4.3 Future study surface

### Study / flashcards / decks

Recommended future route group:
- `GET /study-sets`
- `GET /study-sets/{id}`
- `POST /study-sets`
- `PATCH /study-sets/{id}`
- `GET /study-sets/recommended`
- `POST /study-sessions`
- `POST /study-sessions/{id}/progress`
- `GET /study-sessions/{id}/summary`

Alternative naming if preferred:
- `/flashcards/*`
- `/decks/*`
- `/practice-sets/*`

### Recommendation

Use **`/study-sets`** if you want the broadest flexibility.
It supports flashcards, review bundles, and self-test sets without locking the feature into one interaction pattern.

## 4.4 Formal play lifecycle surface

Questions should not own the whole game lifecycle.

Recommended long-term play endpoints:
- `POST /play/sessions`
- `GET /play/sessions/{id}`
- `POST /play/sessions/{id}/start`
- `POST /play/sessions/{id}/submit`
- `GET /play/sessions/{id}/results`
- `POST /play/matchmaking/enqueue`

You may already cover parts of this with matches/matchmaking features.
If so, the frontend naming should still change to **Play** even if the backend is backed by `matches` instead of `play`.

---

## 5. Frontend screen map

This section converts the backend separation into a cleaner player-facing navigation model.

## 5.1 Recommended top-level navigation

Replace the quiz-centric mental model with:

- **Play**
- **Learn**
- **Study**

Optional supporting destinations:
- **Arcade**
- **Rank / Leaderboards**
- **Profile**

## 5.2 Screen map by domain

### Play

#### Purpose
Formal gameplay and challenge entry.

#### Current likely screens involved
- `QuestionScreen` as a play hub candidate
- `PlayQuizScreen` or its renamed successor
- `AdaptedQuestionScreen`
- category/class/daily/monthly launch screens
- multiplayer matchmaking launch flow

#### Recommended target routes
- `/play`
- `/play/category/:categoryId`
- `/play/daily`
- `/play/monthly`
- `/play/class/:classLevel`
- `/play/session/:mode`

#### Legacy route policy
- Do not depend on backend `/quiz/*`
- Frontend route aliases may exist temporarily, but they should resolve into Play-oriented builders and services

### Learn

#### Current routes already present
- `/learn-hub`
- `/learn-hub/module/:moduleId`
- `/learn-hub/module/:moduleId/lessons`
- `/learn-hub/module/:moduleId/complete`

#### Recommendation
Keep these and strengthen them as the official mastery surface.

### Study

#### New surface to add
Recommended routes:
- `/study`
- `/study/set/:setId`
- `/study/flashcards/:setId`
- `/study/test/:setId`
- `/study/favorites`
- `/study/weak-areas`

#### First release scope
Start with:
- favorites review
- weak-area review
- category study sets
- admin-curated starter sets

---

## 6. Mapping current frontend files to target roles

## 6.1 Keep and strengthen

### `lib/game/services/question_hub_service.dart`
**Role:** canonical gameplay question-fetch service

#### Action
- keep as primary retrieval path
- remove legacy backend `/quiz` fallback
- expand it into the single entry point for question-set loading

### `lib/core/repositories/learning_repository.dart`
**Role:** canonical learn-hub data access

#### Action
- keep as-is conceptually
- add richer progress/recommendation methods later

### `lib/screens/learn_hub/*`
**Role:** learn domain UI

#### Action
- keep and polish
- do not merge into play or study screens

## 6.2 Refactor or deprecate

### `lib/core/services/api_service.dart`
**Problem:** direct `fetchQuestions()` may still point to `/quiz/play`

#### Action
- deprecate `fetchQuestions()`
- replace with generic `getQuestionSet()` or remove direct question fetching from this layer
- route question retrieval through `QuestionHubService` or a dedicated question API client

### `lib/core/networking/tycoon_api_client.dart`
**Problem:** `getQuizQuestions()` may still point to `/quiz/play`

#### Action
- deprecate or rename to `getQuestionSet()`
- update path to `/questions/set`
- remove backend `/quiz` adapter assumptions

### `lib/screens/question/question_view_screen.dart`
### `lib/screens/question/adapted_question_screen.dart`
**Problem:** duplicate `AdaptedQuestionScreen` implementations

#### Action
- choose one canonical file
- merge missing behavior from the other
- remove duplicate export/import path usage
- update router imports to point to one class only

---

## 7. Detailed phased refactor checklist

## Phase 0 - Freeze semantics and contracts

### Goal
Stop the architecture from drifting further while you refactor.

### Tasks
- [ ] Decide final terminology:
  - [ ] **Play** = formal gameplay
  - [ ] **Learn** = modules/lessons
  - [ ] **Study** = Quizlet-like review
  - [ ] **Questions** = canonical content layer
- [ ] Write a short backend/frontend contract note in the repo docs.
- [ ] Mark `/quiz/*` as removed from backend contracts in code comments and internal docs.
- [ ] Mark `QuestionHubService` as the preferred gameplay question source.
- [ ] Identify every frontend caller still using `/quiz/*` directly.

### Deliverables
- migration glossary
- route naming standard
- short ADR / architecture note

## Phase 1 - Stabilize backend contracts

### Goal
Make backend intent explicit before frontend cleanup.

### Tasks
- [ ] Confirm `/questions/set`, `/questions/check`, `/questions/check-batch` payload contracts.
- [ ] Confirm `/modules`, `/modules/{id}`, `/modules/{id}/lessons`, `/modules/{id}/complete` payload contracts.
- [ ] Add response documentation/comments clarifying:
  - [ ] questions endpoint does not expose correct answers
  - [ ] learning lessons may expose correct answers
- [ ] Remove stale docs or code comments that still imply `/quiz/play` exists.
- [ ] Remove telemetry assumptions that depend on live backend `/quiz` traffic.

### Deliverables
- stable DTO contract list
- clear route ownership
- backend comments that match implemented reality

## Phase 2 - Unify frontend question loading

### Goal
Make one service the canonical gameplay question pipeline.

### Tasks
- [ ] Refactor all gameplay question retrieval to go through `QuestionHubService`.
- [ ] Update or deprecate `ApiService.fetchQuestions()`.
- [ ] Update or deprecate `TycoonApiClient.getQuizQuestions()`.
- [ ] Replace direct `/quiz/play` usage in category/class/daily/monthly launch flows.
- [ ] Replace direct `/quiz/play` usage in multiplayer prefetch/launch flows.
- [ ] Make fallback order explicit:
  1. `/questions/set`
  2. local bundled question source or explicit non-backend fallback

### Deliverables
- single gameplay question retrieval pipeline
- reduced transport-layer duplication
- no direct backend `/quiz` usage

## Phase 3 - Clean up play routing and screen ownership

### Goal
Make the UI read like product surfaces instead of legacy implementation details.

### Tasks
- [ ] Introduce new route aliases for play:
  - [ ] `/play`
  - [ ] `/play/...`
- [ ] Remove or redirect frontend `/quiz/*` routes where safe.
- [ ] Update menu labels from `Quiz` to `Play` where the user is entering competitive gameplay.
- [ ] Consolidate `AdaptedQuestionScreen` into one canonical implementation.
- [ ] Make one launcher/orchestrator screen responsible for converting route params into question session state.
- [ ] Remove ambiguous duplicate imports in the router.

### Deliverables
- Play-oriented route surface
- one canonical gameplay question screen
- fewer duplicate builders and launch branches

## Phase 4 - Harden the Learn domain

### Goal
Turn learning into a polished, explicitly separate product area.

### Tasks
- [ ] Keep `learn-hub` visually separate from Play.
- [ ] Add learning progress summary to hub cards or module detail.
- [ ] Add recommended module logic.
- [ ] Add "continue learning" CTA from menu/home.
- [ ] Add reward transparency.
- [ ] Ensure lesson completion and module completion flows are idempotent and safe on retries.

## Phase 5 - Introduce Study / Quizlet-like surface

### Goal
Create the new self-test and recall experience without corrupting gameplay architecture.

### MVP scope
- [ ] create `StudyHubScreen`
- [ ] create starter routes under `/study`
- [ ] support favorites-based review set
- [ ] support weak-area review set
- [ ] support category-based study set
- [ ] create flashcard mode
- [ ] create self-test mode

### Backend preparation
- [ ] decide whether MVP study sets are generated from existing questions or stored as explicit entities
- [ ] create a minimal `study-sets` contract if needed

## Phase 6 - Remove legacy quiz dependence

### Goal
Finish the migration without reintroducing removed backend contracts.

### Tasks
- [ ] verify no frontend/mobile flows call `/quiz/*` backend endpoints anymore
- [ ] remove fallback in `QuestionHubService`
- [ ] remove deprecated methods in `ApiService` and `TycoonApiClient`
- [ ] update docs, tests, and route maps
- [ ] reserve Study for any future rehearsal API instead of reviving quiz-first naming

### Deliverables
- questions-backed gameplay pipeline only
- no duplicate question transport APIs
- clearer long-term maintainability

---

## 8. Acceptance criteria

The migration should be considered successful when all of the following are true:

### Backend
- [ ] `/questions/*` is the canonical gameplay content surface
- [ ] `/modules/*` is the canonical learning surface
- [ ] `/quiz/*` is absent from the supported backend contract
- [ ] study endpoints are separated if introduced

### Frontend
- [ ] all gameplay question retrieval flows use the same canonical service
- [ ] no duplicate `AdaptedQuestionScreen` ownership remains
- [ ] user-facing IA clearly separates Play, Learn, and Study
- [ ] route names and button labels align with actual product meaning

### Product clarity
- [ ] players understand where to compete, where to learn, and where to review
- [ ] backend contracts match those expectations
- [ ] future features can be added without overloading `quiz` again

---

## 9. Suggested implementation note for your repo

Use this as the short internal rule:

> **Questions are the shared content layer. Play is competitive. Learn is guided mastery. Study is flexible rehearsal. Quiz is legacy terminology and is not part of the supported backend contract.**

---

## 10. Final recommendation

Do not restore `/quiz/*` just because the legacy name still appears in frontend code or docs.

Instead:

- keep gameplay on **Questions + Play**
- keep education on **Learn + Modules**
- launch the Quizlet-style idea as **Study**
- and remove remaining stale quiz-route assumptions as cleanup work

That gives you a cleaner long-term foundation for Synaptix / Trivia Tycoon while preserving the separation you already established in the backend.
