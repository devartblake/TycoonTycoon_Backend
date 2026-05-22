# Synaptix Unified Personalization Layer — Next Steps

> **Status as of 2026-04-29: ALL ITEMS COMPLETE** ✅

## Implementation Status

| Workstream | Status |
|---|---|
| 1. Database Schema | ✅ Complete — `player_mind_profiles`, `player_behavior_events`, `personalization_recommendations`, `personalization_rules` |
| 2. C# Services | ✅ Complete — `IPersonalizationService`, `IPlayerMindProfileService`, `PersonalizationGuardrailService`, `PersonalizationSidecarClient` |
| 3. Sidecar APIs | ✅ Complete — `POST /personalization/score-player`, `POST /personalization/recommendation-candidates` |
| 4. Admin Dashboard | ✅ Complete — 8 admin endpoints (`/admin/personalization/*`) |
| 5. Gameplay Integration | ✅ Complete — question_answered, match_completed, learning_module_completed, store_item_purchased, notification_opened/dismissed |

## Database Tables ✅
- `player_mind_profiles` — 21 columns, 4 indexes
- `player_behavior_events` — composite index on (player_id, occurred_at DESC)
- `personalization_recommendations` — accept/dismiss lifecycle
- `personalization_rules` — unique index on rule_key

## Services ✅
- `IPersonalizationService` / `PersonalizationService` — home recommendations, coach brief
- `IPlayerMindProfileService` / `PlayerMindProfileService` — get/create, record event, recalculate
- `IPersonalizationGuardrailService` / `PersonalizationGuardrailService` — 4 rules (store_offer, notification, ranked_difficulty, opt-out)
- `IPersonalizationSidecarClient` / `PersonalizationSidecarClient` — typed HTTP client with 5s timeout

## Sidecar APIs ✅
- `POST /personalization/score-player` — miss-rate + slow-rate frustration model, archetype classification, churn risk accumulation
- `POST /personalization/recommendation-candidates` — 4 candidate types based on profile signals

## Admin Features ✅
- Archetype tracking (`GET /admin/personalization/archetypes`)
- Churn/frustration metrics (`GET /admin/personalization/summary`)
- Recommendation performance (`GET /admin/personalization/recommendations/performance`)
- Store conversion tracking (via `store_item_purchased` behavior events)
- Player profile management (get, recalculate, reset)
- Guardrail rule management (list, upsert)

## Gameplay Hooks ✅
- `QuestionAnsweredMissionJob` → `question_answered` event
- `MissionProgressService.ApplyMatchCompletedAsync` → `match_completed` event
- `CompleteModuleHandler` → `learning_module_completed` event
- `StoreEndpoints.Purchase` → `store_item_purchased` event; `GetSpecialOffers` → guardrail check (frustration ≥ 0.75 suppresses offers)
- `PlayerInboxService.MarkReadAsync` → `notification_opened`; `DeleteAsync` → `notification_dismissed`

## Frontend Impact ✅

> ⚠️ **Path shapes below were superseded by the April 30 2026 backend handoff.** The live Flutter implementation uses the correct paths listed immediately after.

~~`GET /personalization/home/{playerId}`~~  
~~`POST /personalization/recommendations/{id}/accept`~~  
~~`POST /personalization/recommendations/{id}/dismiss`~~

**Current correct paths (as implemented in `synaptix_api_client.dart`):**
```
GET  /personalization/{playerId}/home
GET  /personalization/{playerId}/recommendations
POST /personalization/{playerId}/events   — replaces accept/dismiss; send BehaviourEventDto with eventType: 'recommendation_accepted' or 'recommendation_dismissed'
GET  /coach/{playerId}/daily-brief
POST /coach/{playerId}/feedback
```
Frontend displays results only (no logic). All scoring, guardrails, and recommendations are backend-authoritative.

## Final Architecture ✅
```
Flutter → Backend → PersonalizationService → Sidecar → Backend → Flutter UI
                  ↓                           ↑
              GuardrailService            score-player / recommendation-candidates
                  ↓
              PlayerMindProfile (PostgreSQL)
              PlayerBehaviorEvents (PostgreSQL)
```
