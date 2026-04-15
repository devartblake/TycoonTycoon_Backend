# Question Flow Frontend/Backend Handoff

**Date:** 2026-04-15  
**Audience:** Backend / Platform Team  
**Purpose:** Align the current Flutter frontend question flow with the backend question contracts so category, class, daily, single-player, and multiplayer quiz experiences use the intended endpoints and response envelopes consistently.

---

## Executive Summary

The frontend question system is now organized around a single repository path:

- `QuestionRepository`
- `QuestionRepositoryImpl`
- `QuestionHubService`

This stack is used by:

- `QuestionScreen` hub
- category quiz entry flow
- class quiz entry flow
- daily quiz entry flow
- gameplay question loading
- per-answer validation
- end-of-quiz batch reconciliation

The frontend is **backend-first** and still supports **local fallback** when:

- a backend endpoint is unavailable
- a backend contract is missing required fields
- the backend returns an unexpected envelope

The frontend now also exposes visible and logged source status:

- `backend`
- `localFallback`

This means the backend team can treat this document as the current truth for how the frontend expects question endpoints to behave.

---

## Frontend Architecture

### Primary files involved

- [question_repository.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/repositories/question_repository.dart)
- [question_repository_impl.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/repositories/question_repository_impl.dart)
- [question_hub_service.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/services/question_hub_service.dart)
- [question_response_contract.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/services/question_response_contract.dart)
- [question_providers.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/providers/question_providers.dart)
- [quiz_state.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/state/quiz_state.dart)
- [question_screen.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/question/question_screen.dart)
- [category_quiz_screen.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/question/categories/category_quiz_screen.dart)
- [class_quiz_screen.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/question/categories/class_quiz_screen.dart)
- [daily_quiz_screen.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/question/categories/daily_quiz_screen.dart)
- [question_view_screen.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/question/question_view_screen.dart)
- [app_router.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/navigation/app_router.dart)

### Local fallback files

- [question_loader_service.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/services/question_loader_service.dart)
- [question_asset_index_loader.dart](/c:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/data/question_asset_index_loader.dart)
- `assets/questions/question_paths_index.json`

---

## Frontend Flow Map

### 1. Question hub / discovery flow

Used by:

- `QuestionScreen`
- all-categories browsing
- all-classes browsing
- category/class stat cards

Frontend reads:

- available categories
- question stats
- category stats
- class stats
- dataset info

### 2. Category quiz flow

Used by:

- `AllCategoriesScreen`
- `CategoryQuizScreen`

Frontend behavior:

- loads a large category-specific question set from the repository
- computes counts by difficulty/media type locally
- lets the user filter by difficulty and media
- launches gameplay with a **curated question list**

Important note:

- the category screen no longer depends on `/quiz/play` to infer the correct category from route params
- it now passes the selected `QuestionModel` list directly into the quiz experience

### 3. Class quiz flow

Used by:

- `AllClassesScreen`
- `ClassQuizScreen`

Frontend behavior:

- loads class stats and available categories for the class
- builds the class subject list from backend-returned `availableCategories` when possible
- falls back to `QuizCategoryManager.getCategoriesForClass(classLevel)` if backend class stats do not provide categories
- fetches questions for the selected class subject and launches gameplay with a **curated question list**

Important note:

- legacy fake subject IDs have been removed from the main class launch path
- class subject selection now expects real `QuizCategory`-aligned identifiers

### 4. Daily quiz flow

Used by:

- `DailyQuizWidget`
- `DailyQuizScreen`

Frontend behavior:

- requests daily questions from the repository
- shows preview cards for today’s question set
- launches gameplay with the exact daily question list returned by the repository

### 5. Gameplay answer validation flow

Used by:

- `AdaptedQuestionScreen`
- `AdaptedQuizNotifier`

Frontend behavior:

- loads the question set
- validates each selected answer through backend when available
- reconciles final answers through batch validation at the end of the quiz

### 6. Multiplayer warmup flow

Used by:

- multiplayer matchmaking/prefetch paths

Frontend behavior:

- prefetches multiplayer questions through repository-backed mixed quiz paths
- still depends on the same repository/hub layer for question retrieval

---

## Backend Endpoints the Frontend Currently Uses

## A. Question retrieval

### Primary category / general retrieval

**Primary endpoint**

`GET /questions/set`

**Used for**

- category quiz question retrieval
- mixed quiz retrieval
- daily fallback retrieval

**Current query params used by frontend**

- `category`
- `count`
- `difficulty`

**Expected success payload**

The frontend accepts any of:

- top-level array
- `{ "items": [...] }`
- `{ "questions": [...] }`
- `{ "data": [...] }`

Each item must map cleanly to `QuestionModel`.

**Question item fields expected by frontend**

- `id`
- `category`
- `question`
- `answers`
- `correctAnswer`
- `type`
- `difficulty`
- optional media/power-up fields:
  - `imageUrl`
  - `videoUrl`
  - `audioUrl`
  - `audioTranscript`
  - `audioDuration`
  - `powerUpHint`
  - `powerUpType`
  - `showHint`
  - `reducedOptions`
  - `multiplier`
  - `isBoostedTime`
  - `isShielded`
  - `tags`

**Answer item fields expected**

- `text`
- `isCorrect`

**Frontend contract note**

If the payload is not a list or list-envelope, the frontend treats the contract as invalid and falls back locally.

---

### Legacy quiz retrieval fallback

**Legacy endpoint**

`GET /quiz/play`

**Used as**

- fallback when `/questions/set` fails for category retrieval

**Current frontend usage**

The frontend uses `ApiService.fetchQuestions(...)` with:

- `amount`
- `category`
- `difficulty`

**Expected payload**

`List<Map<String, dynamic>>`

This is treated as a legacy shape and converted directly to `QuestionModel`.

**Important**

This route is still supported by the frontend for resilience, but `/questions/set` is now the preferred contract.

---

### Mixed quiz retrieval

**Endpoints used**

- `GET /questions/set`
- fallback: `GET /quiz/mixed`
- fallback: `GET /questions/mixed`

**Query params used**

- `count`
- `categories`
- `difficulties`
- `balanceDifficulties`

**Expected envelope for `/quiz/mixed` and `/questions/mixed`**

```json
{
  "items": [ ... ],
  "meta": { ... }
}
```

Accepted collection keys:

- `items`
- `questions`
- `data`

For mixed/daily secondary endpoints, `meta` is currently required by the frontend parser.

---

### Daily quiz retrieval

**Endpoints used**

- `GET /questions/set?count=<n>`
- fallback: `GET /quiz/daily?count=<n>`

**Expected envelope for `/quiz/daily`**

```json
{
  "items": [ ... ],
  "meta": { ... }
}
```

Accepted collection keys:

- `items`
- `questions`
- `data`

**Important**

The frontend daily flow now previews and launches the exact daily question list returned by the repository.

---

## B. Discovery / stats / metadata

### Available categories

**Endpoint**

`GET /quiz/categories`

**Accepted collection keys**

- `items`
- `categories`
- `data`

**Accepted item shapes**

1. Plain string:

```json
"science"
```

2. Object with one of:

- `name`
- `slug`
- `category`

Example:

```json
{
  "name": "science"
}
```

**Frontend output**

All values are converted through `QuizCategoryManager.fromString(...)`.

If categories do not map to the frontend enum, they will be ignored.

---

### Global question stats

**Endpoints attempted in order**

- `GET /quiz/stats`
- `GET /questions/stats`

**Expected object**

Must contain at least one of:

- `totalQuestions`
- `questionCount`
- `total`

Optional fields can include:

- `categoryCount`
- `categories`
- `totalCategories`
- `source`

---

### Category stats

**Endpoints attempted in order**

- `GET /quiz/categories/{categorySlug}/stats`
- `GET /questions/categories/{categorySlug}/stats`

**Expected object**

Must contain at least one of:

- `questionCount`
- `totalQuestions`
- `total`

Optional / useful fields:

- `difficulty`
- `category`
- `source`

**Current frontend usage**

- category cards
- category launch screen counts
- category difficulty display

---

### Class stats

**Endpoints attempted in order**

- `GET /quiz/classes/{classId}/stats`
- `GET /questions/classes/{classId}/stats`

**Expected payload**

The frontend currently parses this as a **collection-style response** because it expects category membership to come back through:

- `availableCategories`
- `categories`
- `items`

Minimum useful object example:

```json
{
  "questionCount": 120,
  "subjectCount": 6,
  "availableCategories": [
    { "name": "science" },
    { "name": "mathematics" },
    { "name": "history" }
  ]
}
```

**Frontend note**

If the backend returns only:

```json
{ "questionCount": 120 }
```

the frontend treats that as insufficient for the class contract and falls back to local class category assumptions.

**Recommended backend contract**

Always return:

- `questionCount`
- `subjectCount`
- `availableCategories`

---

### Dataset info

**Endpoints attempted in order**

- `GET /quiz/datasets/info`
- `GET /questions/datasets/info`

**Expected object**

Must contain at least one of:

- `name`
- `version`
- `datasetName`

Useful optional fields:

- `questionCount`
- `totalQuestions`
- `source`
- `meta`

---

## C. Answer validation

### Single-answer validation

**Endpoint**

`POST /questions/check`

**Request body**

```json
{
  "questionId": "question-id",
  "answer": "selected answer text",
  "selectedAnswer": "selected answer text"
}
```

The frontend sends both `answer` and `selectedAnswer` today for compatibility.

**Expected response**

Must contain a boolean in one of:

- `isCorrect`
- `correct`
- `is_valid`
- `valid`

Optional / strongly recommended:

- `questionId`
- `correctAnswer`
- `expectedAnswer`
- `source`

Preferred example:

```json
{
  "questionId": "question-id",
  "isCorrect": true,
  "correctAnswer": "Paris",
  "source": "backend"
}
```

**Frontend fallback behavior**

If the endpoint fails or the boolean field is missing, the frontend validates locally using `QuestionModel.correctAnswer`.

---

### Batch answer validation

**Endpoint**

`POST /questions/check-batch`

**Request body**

```json
{
  "answers": [
    {
      "questionId": "q1",
      "answer": "A",
      "selectedAnswer": "A"
    },
    {
      "questionId": "q2",
      "answer": "B",
      "selectedAnswer": "B"
    }
  ]
}
```

**Expected response**

Accepted collection keys:

- `items`
- `results`
- `answers`
- `data`

Each result item should contain:

- `questionId`
- one of: `isCorrect`, `correct`, `is_valid`, `valid`
- optional `correctAnswer`
- optional `source`

Preferred example:

```json
{
  "items": [
    {
      "questionId": "q1",
      "isCorrect": true,
      "correctAnswer": "A",
      "source": "backend"
    }
  ]
}
```

**Frontend fallback behavior**

If the batch payload is invalid or missing question identifiers, the frontend falls back to local evaluation for the final reconciliation pass.

---

## Current Frontend Route-to-API Alignment

## 1. `/quiz`

Screen:

- `QuestionScreen`

Reads:

- `getDailyQuestions()`
- `getAvailableCategories()`
- `getQuestionStats()`
- `getCategoryStats(...)`
- `getClassStats(...)`
- `getDatasetInfo()`

Backend dependencies:

- `/questions/set`
- `/quiz/categories`
- `/quiz/stats`
- `/questions/stats`
- `/quiz/categories/{slug}/stats`
- `/questions/categories/{slug}/stats`
- `/quiz/classes/{classId}/stats`
- `/questions/classes/{classId}/stats`
- `/quiz/datasets/info`
- `/questions/datasets/info`

---

## 2. `/category-quiz/:category`

Screens:

- `AllCategoriesScreen`
- `CategoryQuizScreen`

Reads:

- category discovery
- category stats
- category question set

Backend dependencies:

- `/quiz/categories`
- `/quiz/categories/{slug}/stats`
- `/questions/categories/{slug}/stats`
- `/questions/set`
- fallback `/quiz/play`

---

## 3. `/class-quiz/:classLevel`

Screens:

- `AllClassesScreen`
- `ClassQuizScreen`

Reads:

- class stats
- class categories
- selected category question set

Backend dependencies:

- `/quiz/classes/{classId}/stats`
- `/questions/classes/{classId}/stats`
- `/quiz/categories/{slug}/stats`
- `/questions/categories/{slug}/stats`
- `/questions/set`
- fallback `/quiz/play`

---

## 4. `/daily-quiz`

Screens:

- `DailyQuizWidget`
- `DailyQuizScreen`

Reads:

- daily question set

Backend dependencies:

- `/questions/set`
- fallback `/quiz/daily`

---

## 5. `/quiz/play`

Screens:

- `PlayQuizScreen`
- `AdaptedQuestionScreen`

Current behavior:

- if called without payload, the user sees game mode selection
- if called with a launch payload from category/class/daily, the route now goes directly into `AdaptedQuestionScreen`

Launch payload can include:

- `questions`
- `category`
- `subject`
- `categories`
- `classLevel`
- `questionCount`
- `displayTitle`

**Important**

Category/class/daily flows now pass curated `QuestionModel` lists into gameplay so the frontend does not have to rebuild the intended question set from route params.

This fixed the prior mismatch where:

- category flow used `categories`
- class flow used `subject`
- daily flow was still incomplete

---

## Current Contract Gaps / Risks

## 1. Class stats should return categories explicitly

This is the biggest remaining backend contract risk.

The frontend is much more reliable when class stats return:

- `questionCount`
- `subjectCount`
- `availableCategories`

If `availableCategories` is absent, the frontend falls back to hard-coded age/class assumptions.

**Backend recommendation**

Treat `availableCategories` as required for class stats responses.

---

## 2. `/quiz/categories` values must map to frontend enum names

The frontend only recognizes categories that can be resolved by `QuizCategoryManager.fromString(...)`.

Examples that work well:

- `science`
- `mathematics`
- `currentEvents`
- `socialStudies`
- `computerScience`
- `healthMedicine`

Examples that may require normalization:

- `english`
- `social_studies`
- `current_events`

These can still work if normalized consistently, but the safest backend approach is to use stable enum-like slugs.

---

## 3. `QuestionModel` answer shape must remain consistent

Preferred answer shape:

```json
{
  "text": "Paris",
  "isCorrect": true
}
```

If the backend uses a different answer schema, the frontend will need a DTO adapter before direct parsing.

---

## 4. Answer validation is still text-based

Today the frontend validates answers by sending answer text:

- `answer`
- `selectedAnswer`

This works, but it is weaker than answer-ID-based validation.

**Ideal backend contract for future hardening**

- each answer option has a stable `answerId`
- frontend submits `answerId`
- backend validates by ID instead of raw display text

This would reduce ambiguity and make answer randomization safer across clients.

---

## 5. Mixed/daily secondary endpoints currently require `meta`

The frontend parser for:

- `/quiz/mixed`
- `/questions/mixed`
- `/quiz/daily`

currently expects `meta` when parsing those collection envelopes.

If the backend does not want to return `meta`, the frontend can be relaxed, but that should be an intentional decision.

---

## Recommended Backend Contract Standard

To simplify the frontend and remove fallback branching, the backend should standardize on:

## Collections

```json
{
  "items": [ ... ],
  "meta": {
    "source": "backend",
    "count": 10
  }
}
```

## Objects

```json
{
  "questionCount": 120,
  "subjectCount": 6,
  "availableCategories": [
    { "name": "science" }
  ],
  "meta": {
    "source": "backend"
  }
}
```

## Validation responses

```json
{
  "questionId": "q1",
  "isCorrect": true,
  "correctAnswer": "A",
  "source": "backend"
}
```

## Suggested canonical endpoint set

- `GET /questions/set`
- `GET /quiz/categories`
- `GET /quiz/stats`
- `GET /quiz/categories/{slug}/stats`
- `GET /quiz/classes/{classId}/stats`
- `GET /quiz/datasets/info`
- `GET /quiz/daily`
- `GET /quiz/mixed`
- `POST /questions/check`
- `POST /questions/check-batch`

Legacy routes can remain temporarily, but the frontend is already optimized for this smaller canonical set.

---

## Frontend Fallback Behavior the Backend Team Should Know About

If a question endpoint fails or returns an invalid contract, the frontend may:

- use local asset-backed categories
- use local asset-backed question datasets
- use local answer validation
- show a banner indicating local fallback is active
- emit logs like:
  - `[QuestionHub] BACKEND ...`
  - `[QuestionHub] LOCAL_FALLBACK ...`

This helps QA, but it also means backend regressions may appear as “the app still works” unless the fallback banner/logs are checked.

---

## Backend Validation Checklist

- Confirm `/questions/set` is the preferred canonical question retrieval route.
- Confirm `/quiz/play` should remain as a legacy fallback or be retired.
- Confirm `/quiz/categories` returns category values that map to `QuizCategoryManager.fromString(...)`.
- Confirm `/quiz/classes/{classId}/stats` will always return `availableCategories`.
- Confirm `/quiz/daily` and `/quiz/mixed` return collection envelopes with `items` and `meta`.
- Confirm `/questions/check` and `/questions/check-batch` return stable boolean correctness fields.
- Confirm question payloads always include `answers[].text` and `answers[].isCorrect`.
- Confirm whether the backend wants to move to answer-ID-based validation in a follow-up contract.

---

## Recommended Next Joint Step

The cleanest next step is a backend/frontend contract lock for these five surfaces:

1. `GET /questions/set`
2. `GET /quiz/categories`
3. `GET /quiz/classes/{classId}/stats`
4. `POST /questions/check`
5. `POST /questions/check-batch`

If those five are stabilized, most of the frontend fallback complexity can eventually be reduced and the category/class/daily flows will remain aligned.

