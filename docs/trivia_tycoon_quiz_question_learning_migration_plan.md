# Trivia Tycoon / Synaptix Migration Plan

## Purpose

This migration plan turns the current backend/frontend pivot into an executable path. It is designed to help you:

- preserve the legacy **quiz** surface without letting it remain the primary gameplay contract,
- stabilize **questions** as the canonical question-content API,
- keep **learning modules** as the mastery path,
- introduce a future **study / Quizlet-like** surface cleanly,
- reduce frontend contract drift,
- and refactor in phases without breaking your current playable flows.

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

- **Do not let “quiz” remain the catch-all term** for gameplay, learning, and study.
- **Questions** should be the shared backend data surface.
- **Play** should become the user-facing formal game mode.
- **Learn** should remain module-driven and explanation-friendly.
- **Study** should become the future Quizlet-style area instead of mutating the main gameplay contract.

### Transitional rule

Keep the existing `/quiz/*` behavior **only as a legacy adapter** during migration.  
Do not keep expanding it as the long-term source of truth.

---

## 2. Current-state diagnosis

## Backend

The backend is already moving in the correct direction:

- `QuestionsEndpoints.Map(app)` and `LearningModulesEndpoints.Map(app)` are both registered in `Program.cs`, meaning the app already exposes them as first-class surfaces.
- `QuestionsEndpoints` exposes a grouped `/questions` route with:
  - `GET /questions/set`
  - `POST /questions/check`
  - `POST /questions/check-batch`
- The question set endpoint is explicitly described as gameplay-safe and **does not expose correct answers**.
- `LearningModulesEndpoints` exposes a grouped `/modules` route with:
  - `GET /modules`
  - `GET /modules/{id}`
  - `GET /modules/{id}/lessons`
  - `POST /modules/{id}/complete`
- The lessons endpoint explicitly exposes lesson questions with correct answers in a **learning context**.

## Frontend

The frontend is only partially aligned with that backend shape.

### Good signs

- `QuestionHubService` already prefers `/questions/set`.
- `LearningRepository` is already aligned to `/modules`.
- Router already has `/learn-hub`, module detail, lesson, and complete screens.

### Main problems

1. **Legacy contract still alive in transport layer**
   - `ApiService.fetchQuestions()` still calls `/quiz/play`.
   - `TycoonApiClient.getQuizQuestions()` still calls `/quiz/play`.
   - `QuestionHubService` still falls back to the legacy transport.

2. **UI naming still says “quiz” even where it now means play**
   - Primary routes still use `/quiz/play`.
   - Category/class/daily/monthly flows launch `/quiz/play`.
   - Multiplayer matchmaking also pushes `/quiz/play`.

3. **Duplicate adapted question screen implementations**
   - `lib/screens/question/adapted_question_screen.dart`
   - `lib/screens/question/question_view_screen.dart`
   Both define `AdaptedQuestionScreen`, which creates cleanup and routing ambiguity.

4. **Mode boundaries are unclear**
   - Formal play, question retrieval, and legacy quiz naming are still mixed together.
   - Learn already has a separate surface, but the overall information architecture still feels “quiz-centric.”

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

This map separates **canonical**, **transitional**, and **future** surfaces.

## 4.1 Canonical backend surfaces

### Questions (canonical content surface)

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

## 4.2 Transitional legacy surface

### Quiz (legacy compatibility only)

#### Keep temporarily
- `/quiz/play`

#### Required rule
Treat `/quiz/play` as one of these only:
- **Option A:** backward-compatible alias to the new question retrieval contract, or
- **Option B:** temporary facade until all frontend callers move to `QuestionHubService`

#### Do not do
- do not add new gameplay features here
- do not make it the primary API for new screens
- do not make study/learning and gameplay both depend on it long-term

#### Sunset recommendation
Once the migration is complete:
- return deprecation headers from `/quiz/play`
- log callers still using it
- remove it after all clients have been migrated and verified

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
- `QuestionScreen` (quiz hub / play hub candidate)
- `PlayQuizScreen`
- `AdaptedQuestionScreen`
- category/class/daily/monthly quiz launch screens
- multiplayer matchmaking -> launch flow

#### Recommended target routes
- `/play`
- `/play/category/:categoryId`
- `/play/daily`
- `/play/monthly`
- `/play/class/:classLevel`
- `/play/session/:mode`

#### Transitional route aliases
Keep temporarily:
- `/quiz`
- `/quiz/play`
- `/quiz/start/:gameMode`

But internally redirect them into Play-oriented builders/services.

### Learn

#### Current routes already present
- `/learn-hub`
- `/learn-hub/module/:moduleId`
- `/learn-hub/module/:moduleId/lessons`
- `/learn-hub/module/:moduleId/complete`

#### Recommendation
Keep these and strengthen them as the official mastery surface.

Optional rename later if you rebrand UX:
- `/learn`
- `/learn/module/:moduleId`
- `/learn/module/:moduleId/lessons`
- `/learn/module/:moduleId/complete`

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
You do not need full custom deck authoring on day one.
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
- remove legacy fallback as final step, not first step
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
- do not merge into quiz/play screens

## 6.2 Refactor or deprecate

### `lib/core/services/api_service.dart`
**Problem:** direct `fetchQuestions()` still points to `/quiz/play`

#### Action
- deprecate `fetchQuestions()`
- replace with generic `getQuestionSet()` or remove direct question fetching from this layer
- route question retrieval through `QuestionHubService` or a dedicated question API client

### `lib/core/networking/tycoon_api_client.dart`
**Problem:** `getQuizQuestions()` still points to `/quiz/play`

#### Action
- deprecate or rename to `getQuestionSet()`
- update path to `/questions/set`
- keep temporary adapter only if another legacy consumer still depends on the current signature

### `lib/screens/question/question_view_screen.dart`
### `lib/screens/question/adapted_question_screen.dart`
**Problem:** duplicate `AdaptedQuestionScreen` implementations

#### Action
- choose one canonical file
- merge missing behavior from the other
- remove duplicate export/import path usage
- update router imports to point to one class only

## 6.3 Rename for clarity

### Current route-driven naming to rethink
- `QuestionScreen` -> could become `PlayHubScreen`
- `PlayQuizScreen` -> could become `PlaySessionLauncherScreen` or stay temporary
- `FavoritesQuizScreen` -> likely becomes `FavoriteStudySetScreen` or `FavoriteQuestionsScreen`, depending on UX intent

### Rule of thumb
- If the screen is about **competition**, name it **play**.
- If it is about **repetition or mastery**, name it **learn** or **study**.
- If it is about raw content, reserve **question** for internal/service/domain naming.

---

## 7. Detailed phased refactor checklist

This checklist is designed to avoid destabilizing the current app.

## Phase 0 — Freeze semantics and contracts

### Goal
Stop the architecture from drifting further while you refactor.

### Tasks
- [ ] Decide final terminology:
  - [ ] **Play** = formal gameplay
  - [ ] **Learn** = modules/lessons
  - [ ] **Study** = Quizlet-like review
  - [ ] **Questions** = canonical content layer
- [ ] Write a short backend/frontend contract note in the repo docs.
- [ ] Mark `/quiz/play` as **legacy** in code comments and internal docs.
- [ ] Mark `QuestionHubService` as the preferred gameplay question source.
- [ ] Identify every frontend caller still using `/quiz/play` directly.

### Deliverables
- migration glossary
- route naming standard
- short ADR / architecture note

---

## Phase 1 — Stabilize backend contracts

### Goal
Make backend intent explicit before frontend cleanup.

### Tasks
- [ ] Confirm `/questions/set`, `/questions/check`, `/questions/check-batch` payload contracts.
- [ ] Confirm `/modules`, `/modules/{id}`, `/modules/{id}/lessons`, `/modules/{id}/complete` payload contracts.
- [ ] Add response documentation/comments clarifying:
  - [ ] questions endpoint does not expose correct answers
  - [ ] learning lessons may expose correct answers
- [ ] Add deprecation comments or response headers to `/quiz/play`.
- [ ] Add telemetry/logging for legacy `/quiz/play` usage.
- [ ] Decide whether `/quiz/play` returns the same shape as `/questions/set` or remains a transformed legacy shape.

### Deliverables
- stable DTO contract list
- deprecation policy for legacy quiz endpoint
- API usage telemetry for legacy callers

---

## Phase 2 — Unify frontend question loading

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
  2. optional legacy adapter (temporary)
  3. local bundled question source

### Deliverables
- single gameplay question retrieval pipeline
- reduced transport-layer duplication
- no new direct `/quiz/play` usage

---

## Phase 3 — Clean up play routing and screen ownership

### Goal
Make the UI read like product surfaces instead of legacy implementation details.

### Tasks
- [ ] Introduce new route aliases for play:
  - [ ] `/play`
  - [ ] `/play/...`
- [ ] Keep `/quiz/*` as temporary redirects or aliases.
- [ ] Update menu labels from “Quiz” to “Play” where the user is entering competitive gameplay.
- [ ] Consolidate `AdaptedQuestionScreen` into one canonical implementation.
- [ ] Make one launcher/orchestrator screen responsible for converting route params into question session state.
- [ ] Remove ambiguous duplicate imports in the router.

### Deliverables
- Play-oriented route surface
- one canonical gameplay question screen
- fewer duplicate builders and launch branches

---

## Phase 4 — Harden the Learn domain

### Goal
Turn learning into a polished, explicitly separate product area.

### Tasks
- [ ] Keep `learn-hub` visually separate from Play.
- [ ] Add learning progress summary to hub cards or module detail.
- [ ] Add recommended module logic.
- [ ] Add “continue learning” CTA from menu/home.
- [ ] Add reward transparency (XP/coins/gems, if applicable).
- [ ] Ensure lesson completion and module completion flows are idempotent and safe on retries.

### Deliverables
- stronger educational identity for Learn
- progress continuity
- better retention hooks

---

## Phase 5 — Introduce Study / Quizlet-like surface

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

### Deliverables
- first Quizlet-like surface
- clear distinction between Play and Study

---

## Phase 6 — Remove legacy quiz dependence

### Goal
Finish the migration without breaking old clients unexpectedly.

### Tasks
- [ ] verify no mobile/frontend flows call `/quiz/play` directly anymore
- [ ] verify analytics show no significant live usage of legacy endpoint
- [ ] remove fallback in `QuestionHubService`
- [ ] remove deprecated methods in `ApiService` and `TycoonApiClient`
- [ ] convert `/quiz/play` into redirect/shim or remove it entirely
- [ ] update docs, tests, and route maps

### Deliverables
- questions-backed gameplay pipeline only
- no duplicate question transport APIs
- clearer long-term maintainability

---

## 8. Detailed file-by-file refactor backlog

## Backend backlog

### A. Contracts and documentation
- [ ] Document `/questions/*` DTOs and response guarantees
- [ ] Document `/modules/*` DTOs and response guarantees
- [ ] Add deprecation notice for `/quiz/play`

### B. Legacy adapter
- [ ] If `/quiz/play` must remain, implement it as a thin adapter over canonical question retrieval
- [ ] Add deprecation header or server log event on each call

### C. Future study API
- [ ] Scaffold `StudySetsEndpoints`
- [ ] Add DTOs for study set summary, study item, study session progress
- [ ] Define whether study uses server-stored sets or generated views over question bank

## Frontend backlog

### A. Networking and services
- [ ] `QuestionHubService` becomes the only approved gameplay question source
- [ ] remove direct question fetch from `ApiService`
- [ ] remove or rename `getQuizQuestions()` in `TycoonApiClient`

### B. Routing
- [ ] add `/play` aliases
- [ ] redirect old `/quiz/*` routes where safe
- [ ] keep `/learn-hub` intact for now
- [ ] add `/study` when MVP starts

### C. UI cleanup
- [ ] merge duplicate `AdaptedQuestionScreen`
- [ ] rename labels/buttons that still say “quiz” but mean “play”
- [ ] separate play visual identity from learn/study identity

### D. Menu and discoverability
- [ ] expose Play / Learn / Study from home/menu
- [ ] add recommendation cards: next play mode, continue module, review weak areas

---

## 9. Recommended migration order by impact

## Highest impact / lowest regret
1. Standardize terminology
2. Make `/questions/*` the canonical gameplay contract everywhere
3. Consolidate gameplay question loading into one service
4. Clean router/screen duplication
5. Preserve `/modules/*` as a distinct learning path

## Medium-term
6. Shift user-facing labels from Quiz -> Play where appropriate
7. Add Play route aliases and deprecate quiz-first routing
8. Improve Learn progress UX

## Strategic next expansion
9. Build Study / Quizlet-like surface as a separate mode
10. Sunset legacy `/quiz/play`

---

## 10. Acceptance criteria

The migration should be considered successful when all of the following are true:

### Backend
- [ ] `/questions/*` is the canonical gameplay content surface
- [ ] `/modules/*` is the canonical learning surface
- [ ] `/quiz/play` is either deprecated, shimmed, or removed
- [ ] study endpoints are separated if introduced

### Frontend
- [ ] all gameplay question retrieval flows use the same canonical service
- [ ] no duplicate `AdaptedQuestionScreen` ownership remains
- [ ] user-facing IA clearly separates Play, Learn, and Study
- [ ] route names and button labels align with actual product meaning

### Product clarity
- [ ] players understand where to compete, where to learn, and where to review
- [ ] backend contracts match those expectations
- [ ] future features can be added without overloading “quiz” again

---

## 11. Suggested implementation note for your repo

Use this as the short internal rule:

> **Questions are the shared content layer. Play is competitive. Learn is guided mastery. Study is flexible rehearsal. Quiz is legacy terminology and should not be expanded as a long-term domain.**

---

## 12. Final recommendation

Do **not** delete the quiz concept abruptly.  
Instead:

- keep it as a migration bridge,
- move gameplay to **Play + Questions**,
- keep education in **Learn + Modules**,
- and launch the Quizlet-style idea as **Study**, not as another overloaded version of quiz.

That gives you a much cleaner long-term foundation for Synaptix / Trivia Tycoon while protecting the work you already did on both backend and frontend.
