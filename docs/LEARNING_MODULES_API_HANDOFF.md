# Learning Modules — API Handoff

> **Audience:** Flutter frontend team  
> **Backend branch:** `claude/review-handoff-plan-4DmaL`  
> **Base URL:** `http(s)://<host>:5000`  
> **OpenAPI docs:** `/swagger` (dev only)

> **Scope note:** This document is only for guided learning modules.
> For the dedicated Study surface covering favorites, flashcards, self-test sessions,
> custom sets, and due-review flows, use
> [study_frontend_backend_handoff_2026-04-18.md](/C:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/study_frontend_backend_handoff_2026-04-18.md).

---

## Overview

The Learning Hub lets players study trivia content in a guided format and earn XP / Coins on completion. The backend exposes four public player-facing endpoints plus an admin surface.

### Flutter screen → endpoint mapping

| Flutter screen | Endpoint(s) |
|----------------|-------------|
| `learn_hub_screen.dart` — Catalogue | `GET /modules` |
| `module_detail_screen.dart` — Overview | `GET /modules/{id}` |
| `lesson_screen.dart` — Lesson flow | `GET /modules/{id}/lessons` |
| `module_complete_screen.dart` — Reward | `POST /modules/{id}/complete` |

---

## Public Endpoints

### `GET /modules` — Browse published modules

**Auth:** None  
**Query params:**

| Param | Type | Description |
|-------|------|-------------|
| `playerId` | `uuid` (optional) | When provided, each item carries `isCompleted: true/false` for that player |
| `category` | `string` (optional) | Filter by category (exact match, case-sensitive) |
| `difficulty` | `int` (optional) | `1`=Easy, `2`=Medium, `3`=Hard, `4`=Expert |

**Example request:**
```
GET /modules?playerId=a1b2c3d4-…&difficulty=2
```

**Response `200 OK`:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Science Basics",
    "description": "Foundational science questions.",
    "category": "Science",
    "difficulty": 1,
    "lessonCount": 10,
    "rewardXp": 500,
    "rewardCoins": 100,
    "isCompleted": false
  }
]
```

**Notes:**
- Results ordered by `difficulty ASC`, `title ASC`
- `isCompleted` is always `false` when `playerId` is omitted
- Difficulty enum serialises as integer — map with: `1=Easy, 2=Medium, 3=Hard, 4=Expert`

---

### `GET /modules/{id}` — Module overview

**Auth:** None

**Response `200 OK`:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Science Basics",
  "description": "Foundational science questions.",
  "category": "Science",
  "difficulty": 1,
  "lessonCount": 10,
  "rewardXp": 500,
  "rewardCoins": 100
}
```

**Response `404 Not Found`:**
```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Module not found.",
    "details": {}
  }
}
```

---

### `GET /modules/{id}/lessons` — Ordered lesson list

**Auth:** None

> **Note on `correctOptionId`:** This field is intentionally exposed on learning endpoints. The learning context is educational — players are supposed to learn the right answers, unlike in competitive gameplay sessions where they are withheld.

**Response `200 OK`:**
```json
[
  {
    "lessonId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "order": 1,
    "questionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "questionText": "What is the powerhouse of the cell?",
    "questionCategory": "Biology",
    "options": [
      { "id": "A", "text": "Nucleus" },
      { "id": "B", "text": "Mitochondria" },
      { "id": "C", "text": "Ribosome" },
      { "id": "D", "text": "Golgi Apparatus" }
    ],
    "correctOptionId": "B",
    "explanation": "Mitochondria produce ATP through cellular respiration, earning the nickname 'powerhouse of the cell'."
  }
]
```

**Response `404 Not Found`:** Same shape as above.

**Notes:**
- Results are ordered by `lesson.order ASC`
- `explanation` may be `null` if the content team didn't add one for this lesson
- `options` are ordered by `optionId` (alphabetically: A, B, C, D)
- `correctOptionId` matches one of the `options[].id` values

**Recommended Flutter lesson flow:**
1. Display `questionText` + `options` (hide which is correct)
2. Player taps an option
3. Reveal: highlight correct option in green, player's wrong selection in red
4. Show `explanation` text below (if non-null)
5. Show "Next" button → advance to `order + 1`

---

### `POST /modules/{id}/complete` — Grant completion reward

**Auth:** None (idempotent — safe to call multiple times)  
**Query params:**

| Param | Type | Required | Description |
|-------|------|----------|-------------|
| `playerId` | `uuid` | Yes | The player claiming the reward |

**Example request:**
```
POST /modules/3fa85f64-5717-4562-b3fc-2c963f66afa6/complete?playerId=a1b2c3d4-…
```
*(No body required)*

**Response `200 OK` — first completion:**
```json
{
  "moduleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "playerId": "a1b2c3d4-…",
  "status": "Completed",
  "rewardXp": 500,
  "rewardCoins": 100,
  "balanceXp": 3500,
  "balanceCoins": 740
}
```

**Response `200 OK` — repeat call (idempotent):**
```json
{
  "moduleId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "playerId": "a1b2c3d4-…",
  "status": "AlreadyCompleted",
  "rewardXp": 500,
  "rewardCoins": 100,
  "balanceXp": 3500,
  "balanceCoins": 740
}
```

**Response `404 Not Found`:**
```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Module not found.",
    "details": {}
  }
}
```

**`status` field values:**

| Value | Meaning |
|-------|---------|
| `"Completed"` | First-time completion — reward was just granted |
| `"AlreadyCompleted"` | Player already completed this module — no double-grant |
| `"ModuleNotFound"` | Sent as a `404` response, not in the body |

**Notes:**
- Call this endpoint after the player finishes the last lesson
- Show the actual `rewardXp` / `rewardCoins` from the response on the completion screen
- `balanceXp` / `balanceCoins` are the player's wallet balances _after_ the grant
- Safe to retry on network failure — the server is idempotent

---

## Difficulty Enum Reference

| Integer | Label | UI colour suggestion |
|---------|-------|---------------------|
| `1` | Easy | Green |
| `2` | Medium | Yellow |
| `3` | Hard | Orange |
| `4` | Expert | Red |

---

## Admin Endpoints

All admin endpoints require:
- `X-Admin-Ops-Key: <key>` header
- Valid admin JWT with role `admin` + audience `admin-app`

### Create module
```
POST /admin/modules
Content-Type: application/json

{
  "title": "Science Basics",
  "description": "Foundational science questions.",
  "category": "Science",
  "difficulty": 1,
  "rewardXp": 500,
  "rewardCoins": 100
}
```
Response `201 Created`: `{ "id": "<guid>" }`

### Publish / Unpublish
```
PATCH /admin/modules/{id}/publish
PATCH /admin/modules/{id}/unpublish
```
Response `200 OK`: `{ "id": "<guid>", "isPublished": true }`

### Add a lesson to a module
```
POST /admin/modules/{id}/lessons
Content-Type: application/json

{
  "questionId": "<guid>",   // must be an existing Question in the system
  "order": 1,               // 1-based; must be unique within this module
  "explanation": "..."      // optional; shown after the player answers
}
```
Response `201 Created`: `{ "lessonId": "<guid>" }`

### Remove a lesson
```
DELETE /admin/modules/{id}/lessons/{lessonId}
```
Response `204 No Content`

### Update module fields
```
PATCH /admin/modules/{id}
Content-Type: application/json

{
  "title": "...", "description": "...", "category": "...",
  "difficulty": 2, "rewardXp": 750, "rewardCoins": 150
}
```
Response `200 OK`: admin module object

### List all modules (including unpublished)
```
GET /admin/modules?category=Science&isPublished=true
```

---

## Database Migration

After pulling this branch, run the EF Core migration to create the three new tables (`learning_modules`, `module_lessons`, `module_completions`):

```bash
# Via Docker (recommended)
make migrate

# Or locally (requires .NET 9 SDK + running PostgreSQL)
dotnet ef migrations add AddLearningModules \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
dotnet ef database update \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
```

---

## Error Response Shape

All errors follow:
```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Human-readable message.",
    "details": {}
  }
}
```

| HTTP status | `code` |
|-------------|--------|
| `404` | `NOT_FOUND` |
| `400` | `VALIDATION_ERROR` |
| `429` | `RATE_LIMITED` |

---

## Questions shared with gameplay

The `Question` records surfaced through lessons are the **same questions** used in competitive matches. Content is not duplicated — `ModuleLesson` just references the `Question.Id` with an optional per-lesson `Explanation`. This means:

- The same question can appear in multiple modules with different explanations
- Questions must have `Status = "Approved"` to be useful (the API does not filter by status on the lesson endpoint — admin team should only link approved questions)
- Any question edits made via `/admin/questions` will immediately reflect in lessons
