# Synaptix Unified Personalization Layer — GitHub Issues

## Issue 1: Add personalization database tables and EF entities

**Priority:** P0

**Labels:** backend, personalization, database, ef-core

## Goal
Add persistence for the Unified Personalization Layer.

## Scope
Create:
- `player_mind_profiles`
- `player_behavior_events`
- `personalization_recommendations`
- `personalization_rules`

## Backend files
- `Tycoon.Backend.Domain/Personalization/PlayerMindProfile.cs`
- `Tycoon.Backend.Domain/Personalization/PlayerBehaviorEvent.cs`
- `Tycoon.Backend.Domain/Personalization/PersonalizationRecommendation.cs`
- `Tycoon.Backend.Infrastructure/Persistence/Configurations/*Personalization*.cs`
- EF migration for the new tables

## Acceptance Criteria
- Migration creates all tables and indexes.
- EF model maps JSONB columns correctly.
- App starts with schema gate enabled.
- No behavior changes to gameplay yet.


---

## Issue 2: Implement PlayerMindProfileService and behavior event ingestion

**Priority:** P0

**Labels:** backend, personalization, application-service

## Goal
Create the service responsible for profile creation, behavior event recording, and profile recalculation.

## Scope
Add:
- `IPlayerMindProfileService`
- `PlayerMindProfileService`
- DTOs for profile and behavior events

## Acceptance Criteria
- `GetOrCreateAsync(playerId)` creates default profile if missing.
- `RecordEventAsync(playerId, event)` stores behavior event.
- `RecalculateAsync(playerId)` updates profile fields using local rules first.
- Service is registered in DI.


---

## Issue 3: Add public personalization API endpoints

**Priority:** P0

**Labels:** backend, api, personalization

## Goal
Expose player-facing personalization endpoints.

## Routes
- `GET /personalization/profile/{playerId}`
- `POST /personalization/profile/{playerId}/event`
- `POST /personalization/profile/{playerId}/recalculate`
- `GET /personalization/home/{playerId}`
- `GET /personalization/recommendations/{playerId}`
- `POST /personalization/recommendations/{recommendationId}/accept`
- `POST /personalization/recommendations/{recommendationId}/dismiss`

## Acceptance Criteria
- Endpoints require auth.
- Event ingestion returns 202.
- Home endpoint returns recommendations and coach brief shell.
- Accept/dismiss updates recommendation record.


---

## Issue 4: Implement PersonalizationGuardrailService

**Priority:** P0

**Labels:** backend, personalization, guardrails, safety

## Goal
Add production safety rules around ToM personalization.

## Required rules
- Suppress paid offers when frustration risk is high.
- Suppress notifications when notification fatigue is high.
- Block ranked difficulty manipulation.
- Prevent Sidecar from directly granting rewards.
- Respect personalization opt-out.

## Acceptance Criteria
- Guardrail service returns allow/block decisions.
- Applied rules are included in recommendation guardrail payload.
- Unit tests cover blocked store offer, blocked notification, and ranked fairness lock.


---

## Issue 5: Add FastAPI Sidecar personalization scoring endpoints

**Priority:** P1

**Labels:** sidecar, fastapi, ml, personalization

## Goal
Add Sidecar endpoints that score player behavior and return recommendation candidates.

## Routes
- `POST /personalization/score-player`
- `POST /personalization/recommendation-candidates`
- `POST /personalization/category-profile`
- `POST /personalization/notification-score`
- `POST /personalization/mission-fit`

## Acceptance Criteria
- Pydantic schemas validate all request/response bodies.
- Scoring endpoint returns churn/frustration/confidence/archetype.
- Candidate endpoint returns typed recommendation candidates.
- Sidecar never mutates backend state directly.


---

## Issue 6: Implement .NET PersonalizationSidecarClient

**Priority:** P1

**Labels:** backend, sidecar, http-client, personalization

## Goal
Connect the .NET backend to the FastAPI Sidecar.

## Scope
Add:
- `IPersonalizationSidecarClient`
- `PersonalizationSidecarClient`
- options config: `SidecarPersonalization:BaseUrl`, timeout, enabled flag

## Acceptance Criteria
- Uses IHttpClientFactory.
- Has timeout and fallback behavior.
- If Sidecar fails, backend falls back to local rules.
- Sidecar output is stored in `sidecar_scores_json`.


---

## Issue 7: Implement PersonalizationService home and recommendations payload

**Priority:** P1

**Labels:** backend, personalization, api

## Goal
Build the main orchestrator that returns safe recommendations to the frontend.

## Scope
- Load PlayerMindProfile.
- Ask Sidecar for candidate recommendations when enabled.
- Apply guardrails.
- Persist active recommendations.
- Return home payload.

## Acceptance Criteria
- `GET /personalization/home/{playerId}` returns recommended mode, category, recommendations, coach brief, and guardrails.
- Paid offers are not returned when suppressed by guardrails.
- Recommendations include source and score.


---

## Issue 8: Add Coach API daily brief

**Priority:** P1

**Labels:** backend, coach, personalization

## Goal
Create lightweight coach recommendations based on the personalization profile.

## Routes
- `GET /coach/{playerId}/daily-brief`
- `POST /coach/{playerId}/feedback`

## Acceptance Criteria
- Daily brief returns title, message, action, route, and tone.
- Feedback is stored as a behavior event.
- Coach tone respects low-pressure/high-frustration guardrails.


---

## Issue 9: Integrate personalization into Questions, Learning, and Study recommendations

**Priority:** P2

**Labels:** backend, questions, learning, study, personalization

## Goal
Use personalization to recommend content without breaking gameplay fairness.

## Scope
- Questions: add adaptive strategy input for non-ranked play.
- Learning: recommend modules based on weak categories.
- Study: recommend study sets/flashcards from missed topics.

## Acceptance Criteria
- Ranked fairness is unchanged.
- Non-ranked/practice question sets can use adaptive strategy.
- Recommendation payload can include learning module and study set targets.


---

## Issue 10: Integrate personalization into Missions

**Priority:** P2

**Labels:** backend, missions, personalization

## Goal
Recommend missions based on player archetypes and recent behavior.

## Mission archetypes
- Confidence Builder
- Streak Seeker
- Explorer
- Comeback Player
- Collector
- Risk Taker
- Social Challenger
- Mastery Path

## Acceptance Criteria
- Mission recommendations appear in `/personalization/home/{playerId}`.
- High-frustration players receive low-pressure missions.
- Mission completion is tracked back into behavior events.


---

## Issue 11: Integrate personalization into Store with monetization guardrails

**Priority:** P2

**Labels:** backend, store, monetization, guardrails

## Goal
Personalize store recommendations safely.

## Rules
- Do not increase difficulty to sell power-ups.
- Do not target frustrated players with aggressive paid offers.
- Prefer free/low-cost support offers for struggling players.
- Log guardrails for every store recommendation.

## Acceptance Criteria
- Store recommendations can be generated as recommendation candidates.
- Guardrail service can suppress paid offers.
- Store offer recommendations include reason and applied guardrails.


---

## Issue 12: Integrate personalization into Notifications

**Priority:** P2

**Labels:** backend, notifications, personalization

## Goal
Use personalization for notification tone, timing, and suppression.

## Scope
- Add notification tone to recommendation payload.
- Respect fatigue score.
- Record notification open/dismiss events.

## Acceptance Criteria
- High-fatigue players receive fewer notification recommendations.
- Notification recommendation includes tone and intent.
- Notification interactions feed behavior events.


---

## Issue 13: Add Admin Personalization endpoints

**Priority:** P2

**Labels:** backend, admin, personalization, analytics

## Goal
Provide operator visibility and tuning controls.

## Routes
- `GET /admin/personalization/summary`
- `GET /admin/personalization/archetypes`
- `GET /admin/personalization/recommendations/performance`
- `GET /admin/personalization/player/{playerId}`
- `POST /admin/personalization/player/{playerId}/recalculate`
- `POST /admin/personalization/player/{playerId}/reset`
- `GET /admin/personalization/rules`
- `PUT /admin/personalization/rules`

## Acceptance Criteria
- Admin endpoints require admin ops key + admin role.
- Summary returns archetype counts and risk bands.
- Rules can be viewed and updated.


---

## Issue 14: Add Flutter personalization providers and UI hooks

**Priority:** P3

**Labels:** frontend, flutter, personalization

## Goal
Expose backend-approved personalization in Flutter.

## Frontend API calls
- `GET /personalization/home/{playerId}`
- `GET /personalization/recommendations/{playerId}`
- `GET /coach/{playerId}/daily-brief`
- `POST /personalization/recommendations/{recommendationId}/accept`
- `POST /personalization/recommendations/{recommendationId}/dismiss`

## Acceptance Criteria
- Frontend does not implement ToM logic directly.
- UI renders recommendations, coach brief, and suggested actions.
- Accept/dismiss actions are sent back to backend.


---

## Issue 15: Add tests for personalization guardrails and Sidecar fallback

**Priority:** P1

**Labels:** tests, backend, personalization

## Goal
Add automated coverage for safety-critical paths.

## Test cases
- High-frustration player suppresses paid store offer.
- High notification fatigue suppresses notification recommendation.
- Ranked difficulty candidate is blocked.
- Sidecar timeout falls back to local rules.
- Accept/dismiss recommendation updates timestamps.

## Acceptance Criteria
- Tests run in CI.
- Guardrail decisions are deterministic.
- Sidecar failure does not break personalization endpoint.


---
