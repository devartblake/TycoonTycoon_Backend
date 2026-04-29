# Synaptix Unified Personalization Layer — Next Steps

## Workstreams
1. Database Schema
2. C# Services
3. Sidecar APIs
4. Admin Dashboard
5. PR Structure

## Database Tables
- player_mind_profiles
- player_behavior_events
- personalization_recommendations

## Services
- IPersonalizationService
- IPlayerMindProfileService
- GuardrailService
- SidecarClient

## Sidecar APIs
POST /personalization/score-player  
POST /personalization/recommendation-candidates  

## Admin Features
- Archetype tracking
- Churn/frustration metrics
- Recommendation performance
- Store conversion tracking

## PR Order
1. DB + Models
2. Services
3. APIs
4. Sidecar
5. Gameplay Integration
6. Engagement Integration
7. Admin Dashboard

## Frontend Impact
Use:
GET /personalization/home/{playerId}  
GET /coach/{playerId}/daily-brief  

Frontend displays results only (no logic).

## Final Architecture
Flutter → Backend → Personalization → Sidecar → Backend → UI
