# Flutter Handoff: Unified Personalization + A/B Experiments (Issue 14)

**Date:** 2026-04-30  
**Backend Contact:** TycoonTycoon Backend Team  
**Branch:** `main` (all endpoints live)  
**Base URL:** `https://api.tycoontycoon.com` (staging: `https://staging-api.tycoontycoon.com`)

---

## Overview

The backend now ships two complementary systems that Flutter consumes to personalise every session:

1. **Unified Personalization Layer** — the backend owns all behavioural scoring, guardrails, and recommendation logic. Flutter is purely a renderer: it reads the profile, renders the coach brief, and fires behaviour events.
2. **A/B Testing Framework** — deterministic, consistent hashing assigns players to experiment variants server-side. Flutter reads its variant assignments at session start and gates UI accordingly.

> **Contract rule:** Flutter never runs personalisation logic itself. All decisions (which variant, which recommendation, whether a store offer is visible) come from the backend.

---

## Authentication

All player-facing endpoints require a valid JWT in the `Authorization: Bearer <token>` header.

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

The `playerId` in the JWT must match the `playerId` path/body parameter. The backend returns `403` on mismatch.

---

## Part 1 — Personalization Endpoints

### 1.1 Get Player Mind Profile

Returns the full behavioural profile for a player. Poll at session start and after significant events.

```
GET /personalization/{playerId}/profile
Authorization: Bearer <token>
```

**Response 200:**
```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "confidenceLevel": 0.72,
  "riskTolerance": 0.55,
  "preferredPace": "steady",
  "learningStyle": "visual",
  "competitivePreference": "solo",
  "socialPreference": "low",
  "churnRiskScore": 0.18,
  "frustrationRiskScore": 0.22,
  "rewardSensitivityScore": 0.65,
  "storeAffinityScore": 0.40,
  "notificationFatigueScore": 0.30,
  "archetype": "steady_learner",
  "categoryStrengths": { "science": 0.85, "history": 0.70 },
  "categoryWeaknesses": { "arts": 0.35 },
  "preferences": {},
  "guardrails": {},
  "personalizationEnabled": true,
  "sidecarScoringEnabled": true,
  "lastCalculatedAt": "2026-04-30T08:00:00Z"
}
```

**Key fields for Flutter:**

| Field | How Flutter uses it |
|-------|---------------------|
| `archetype` | Controls home screen layout variant |
| `churnRiskScore` | If `>= 0.8` show retention nudge |
| `frustrationRiskScore` | If `>= 0.75` disable hard-mode suggestions |
| `notificationFatigueScore` | If `>= 0.7` reduce push badge frequency locally |
| `preferredPace` | `"fast"` / `"steady"` / `"slow"` — controls question timer default |
| `personalizationEnabled` | If `false` render default UI everywhere |

---

### 1.2 Get Home Personalization

Returns the full home-screen recommendation bundle. Call on every app foreground.

```
GET /personalization/{playerId}/home
Authorization: Bearer <token>
```

**Response 200:**
```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "recommendedMode": "study",
  "recommendedCategory": "science",
  "recommendedDifficulty": "medium",
  "recommendations": [
    {
      "id": "a1b2c3d4-...",
      "type": "category_challenge",
      "source": "sidecar",
      "priority": 1,
      "score": 0.91,
      "payload": { "categoryKey": "science", "difficultyKey": "medium" },
      "guardrails": {},
      "expiresAt": "2026-04-30T23:59:59Z"
    }
  ],
  "coachBrief": {
    "title": "Keep it up!",
    "message": "You're on a 3-day streak in Science. One more session seals the weekly badge.",
    "recommendedAction": "Start Science Challenge",
    "targetRoute": "/play/science/medium",
    "tone": "encouraging"
  },
  "guardrails": {}
}
```

**Rendering rules:**

- Render `coachBrief` as a banner on the home screen. Tap calls `POST /coach/{playerId}/feedback` with `{ "briefId": "<id>", "feedback": "dismiss" }` or `"engage"`.
- `recommendedMode` / `recommendedCategory` / `recommendedDifficulty` pre-fill the game launcher defaults.
- `recommendations` is ordered by `priority` ASC. Render the first 3 as action cards.

---

### 1.3 Record Behaviour Event

Fire-and-forget. Tell the backend what the player just did. The backend updates scores asynchronously.

```
POST /personalization/{playerId}/events
Authorization: Bearer <token>
Content-Type: application/json

{
  "eventType": "question_answered",
  "eventSource": "quiz",
  "category": "science",
  "difficulty": "Hard",
  "mode": "solo",
  "metadata": {
    "correct": true,
    "timeMs": 4200,
    "questionId": "abc123"
  },
  "occurredAt": "2026-04-30T09:15:00Z"
}
```

**Response:** `202 Accepted` (no body)

**Standard `eventType` values:**

| eventType | When to fire |
|-----------|--------------|
| `question_answered` | After each answer in any game mode |
| `match_completed` | After a multiplayer or solo match ends |
| `learning_module_completed` | After finishing a learning module |
| `store_item_purchased` | After a successful store purchase |
| `notification_opened` | When player taps a push notification |
| `notification_dismissed` | When player swipes away a push notification |
| `coach_feedback` | When player reacts to coach brief (see 1.6) |

---

### 1.4 Get Recommendations

Returns personalised action recommendations for the player.

```
GET /personalization/{playerId}/recommendations
Authorization: Bearer <token>
```

**Response 200:** Array of `PlayerRecommendationDto` (same shape as the `recommendations` array in `/home`)

---

### 1.5 Toggle Personalization

Player opt-out / opt-in. Respect `personalizationEnabled` from the profile.

```
POST /personalization/{playerId}/toggle
Authorization: Bearer <token>
Content-Type: application/json

{ "enabled": false }
```

**Response 200:**
```json
{ "personalizationEnabled": false }
```

Show this toggle in **Settings → Personalization**. When `false`, skip all recommendation rendering and show static defaults.

---

### 1.6 Coach Feedback

```
POST /coach/{playerId}/feedback
Authorization: Bearer <token>
Content-Type: application/json

{
  "briefId": "some-brief-id",
  "feedback": "engage"
}
```

Valid `feedback` values: `"engage"`, `"dismiss"`, `"helpful"`, `"not_helpful"`.

**Response:** `202 Accepted`

---

### 1.7 Get Daily Brief (standalone)

```
GET /coach/{playerId}/daily-brief
Authorization: Bearer <token>
```

Returns just the `CoachBriefDto` object (same shape as `coachBrief` inside `/home`).

---

## Part 2 — A/B Experiment Endpoints

### 2.1 Bootstrap All Assignments (call at session start)

Returns every experiment the player is currently enrolled in. Call **once** at session start and cache locally for the session.

```
GET /experiments/player/{playerId}
Authorization: Bearer <token>
```

**Response 200:**
```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "assignments": [
    {
      "experimentKey": "home_layout_v2",
      "variantKey": "variant_b",
      "isControl": false,
      "config": {
        "showBannerFirst": true,
        "cardStyle": "compact"
      }
    },
    {
      "experimentKey": "daily_challenge_cta",
      "variantKey": "control",
      "isControl": true,
      "config": {}
    }
  ]
}
```

**Integration pattern:**

```dart
// At session start (after login/splash)
final PlayerExperimentsDto experiments = await api.getPlayerExperiments(playerId);
ExperimentStore.seed(experiments.assignments);

// Later in a widget
final assignment = ExperimentStore.get('home_layout_v2');
if (assignment?.variantKey == 'variant_b') {
  return CompactHomeLayout(config: assignment!.config);
} else {
  return DefaultHomeLayout();
}
```

---

### 2.2 Get Single Experiment Assignment

Use when a specific feature needs to check its own experiment in isolation.

```
GET /experiments/player/{playerId}/{experimentKey}
Authorization: Bearer <token>
```

**Response 200 — enrolled:**
```json
{
  "enrolled": true,
  "experimentKey": "home_layout_v2",
  "assignment": {
    "experimentKey": "home_layout_v2",
    "variantKey": "variant_b",
    "isControl": false,
    "config": { "showBannerFirst": true }
  }
}
```

**Response 200 — not enrolled:**
```json
{
  "enrolled": false,
  "experimentKey": "home_layout_v2"
}
```

When `enrolled: false`, render the **control/default** experience.

---

### 2.3 Record Impression

Call when the player actually **sees** the variant UI for the first time in a session. Used for impression-rate analytics.

```
POST /experiments/player/{playerId}/{experimentKey}/impression
Authorization: Bearer <token>
```

**Response:** `202 Accepted`

```dart
// Call once per experiment per session, after the variant widget is first rendered
void _onExperimentVisible(String experimentKey) {
  api.recordImpression(playerId, experimentKey);
}
```

---

### 2.4 Record Outcome

Call when the player achieves the **conversion event** associated with an experiment (e.g., completed a purchase in a pricing experiment, started a match in a matchmaking experiment).

```
POST /experiments/player/{playerId}/{experimentKey}/outcome
Authorization: Bearer <token>
```

**Response:** `202 Accepted`

---

## Part 3 — Session Startup Sequence

Recommended order to minimise perceived load time:

```dart
Future<void> initSession(Guid playerId) async {
  // Fire both in parallel — they're independent
  final results = await Future.wait([
    api.getPlayerExperiments(playerId),   // GET /experiments/player/{id}
    api.getHomePersonalization(playerId), // GET /personalization/{id}/home
  ]);

  final experiments = results[0] as PlayerExperimentsDto;
  final home = results[1] as PlayerHomePersonalizationDto;

  ExperimentStore.seed(experiments.assignments);
  HomeStore.update(home);
}
```

Fetch the full profile (`GET /personalization/{id}/profile`) lazily in the background — it's only needed for the Settings screen and deep personalisation logic.

---

## Part 4 — Error Handling

All errors follow the standard envelope:

```json
{
  "type": "error_code",
  "title": "Human-readable title",
  "detail": "More detail if available",
  "status": 400
}
```

| Status | Meaning | Flutter action |
|--------|---------|----------------|
| `401` | Token missing / expired | Redirect to login |
| `403` | JWT playerId ≠ path playerId | Log warning, do not retry |
| `404` | Profile not yet created | Show defaults, fire `POST /personalization/{id}/profile/initialize` if available |
| `429` | Rate limited | Back off 30s, show cached data |
| `5xx` | Server error | Show cached/default data silently |

---

## Part 5 — Caching Strategy

| Data | Cache lifetime | Cache key |
|------|----------------|-----------|
| Experiment assignments | Session (until next login) | `experiments:{playerId}` |
| Home personalization | 5 minutes | `home:{playerId}` |
| Full profile | 10 minutes | `profile:{playerId}` |
| Coach brief | 1 hour | `brief:{playerId}` |

Use `HybridCache` or `flutter_cache_manager`. **Never cache behaviour event POSTs** — those are fire-and-forget.

---

## Part 6 — Experiment Config Fields

The `config` dictionary in an assignment carries variant-specific parameters. The backend admin sets these when creating an experiment. Flutter should read them via typed accessors:

```dart
extension AssignmentConfig on ExperimentAssignmentDto {
  bool getBool(String key, {bool fallback = false}) =>
      config[key] is bool ? config[key] as bool : fallback;

  String getString(String key, {String fallback = ''}) =>
      config[key] is String ? config[key] as String : fallback;

  int getInt(String key, {int fallback = 0}) =>
      config[key] is int ? config[key] as int : fallback;
}
```

---

## Part 7 — Known Experiment Keys (as of 2026-04-30)

These are defined by the backend admin. Flutter should treat all experiment keys as opaque strings and fall back to control when `enrolled: false` or the key is absent.

| Experiment Key | Description | Variant keys |
|----------------|-------------|--------------|
| *(seeded at launch)* | Created via admin panel | Defined per experiment |

When the backend team adds a new experiment, they will notify Flutter via Slack `#backend-experiments` with the key, variant names, and relevant `config` fields.

---

## Part 8 — Archetype Reference

The `archetype` field in `PlayerMindProfileDto` is one of:

| Value | Description | Typical home experience |
|-------|-------------|------------------------|
| `steady_learner` | Consistent, low churn risk | Streak emphasis, moderate challenge |
| `competitive_sprinter` | High engagement bursts, PvP preference | Leaderboard prominent, fast-paced |
| `casual_explorer` | Broad category interest, low commitment | Discovery cards, low-pressure mode |
| `knowledge_seeker` | Deep learning focus, high module completion | Module recommendations, study mode |
| `at_risk` | Elevated churn / frustration scores | Gentle nudges, easier challenges |
| `new_player` | < 7 days active | Onboarding track, coach brief always visible |

---

## Part 9 — Quick Reference

```
# Personalization
GET  /personalization/{playerId}/profile
GET  /personalization/{playerId}/home
POST /personalization/{playerId}/events
GET  /personalization/{playerId}/recommendations
POST /personalization/{playerId}/toggle
GET  /coach/{playerId}/daily-brief
POST /coach/{playerId}/feedback

# Experiments
GET  /experiments/player/{playerId}
GET  /experiments/player/{playerId}/{experimentKey}
POST /experiments/player/{playerId}/{experimentKey}/impression
POST /experiments/player/{playerId}/{experimentKey}/outcome
```

---

## Part 10 — Questions and Contacts

- **API questions / contract changes:** Open a ticket in `#backend-api` on Slack or create a GitHub issue tagged `flutter-contract`
- **New experiment setup:** Contact backend team in `#backend-experiments`
- **Swagger / OpenAPI spec:** Available at `https://api.tycoontycoon.com/swagger` (staging: `https://staging-api.tycoontycoon.com/swagger`)
- **Postman collection:** Shared in the `TycoonTycoon` Postman workspace under "Personalization & Experiments"
