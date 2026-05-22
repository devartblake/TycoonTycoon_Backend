# Synaptix Backend Personalization Hardening Plan

> **Status as of 2026-05-01: ALL ITEMS COMPLETE** ✅

## Objective
Stabilize and productionize the Unified Personalization Layer.

## Core Principle
.NET Backend = Authority
Sidecar = Intelligence

## Observability ✅
- ✅ `PersonalizationAuditLog` entity + `personalization_audit_logs` table (migration `20260430120000_AddPersonalizationAuditLog`)
- ✅ `IPersonalizationAuditService` / `PersonalizationAuditService` — logs every recommendation decision (allowed + blocked) with JSONB input signals, candidate, guardrails, final decision
- ✅ `PersonalizationService` calls `LogDecisionAsync` unconditionally before branching allowed/blocked
- ✅ `GET /admin/personalization/summary` — churn/frustration counts, archetype distribution
- ✅ `GET /admin/personalization/recommendations/performance` — acceptance/dismissal rates per type

## Explainability ✅
- ✅ `GET /admin/personalization/debug/{playerId}` — returns profile + last-25 behavior events + last-25 audit entries
- ✅ `Reason` field added to `PersonalizationRecommendation` domain model + EF config + migration `20260501090000_AddReasonToPersonalizationRecommendation`
- ✅ `Reason` added to `PlayerRecommendationDto` — frontend receives explainability text
- ✅ `PersonalizationService` populates `Reason` from sidecar candidate

## Feature Flags ✅
All toggles implemented in `PersonalizationOptions` and bound to `"Personalization"` config section:
- ✅ `Personalization:Enabled`
- ✅ `Personalization:UseSidecar`
- ✅ `Personalization:AdaptiveQuestions`
- ✅ `Personalization:AdaptiveMissions`
- ✅ `Personalization:AdaptiveStore`
- ✅ `Personalization:AdaptiveNotifications`
- ✅ `Personalization:CoachEnabled`
- ✅ `Personalization:FrustrationPaidOfferSuppressionThreshold` (default: 0.75)
- ✅ `Personalization:NotificationFatigueThreshold` (default: 0.70)

## Guardrails ✅
All enforced in `PersonalizationGuardrailService` (thresholds driven by `IOptions<PersonalizationOptions>`):
- ✅ No monetization exploitation — paid offers suppressed when `FrustrationRiskScore >= threshold`
- ✅ No ranked manipulation — `ranked_difficulty_modifier` candidates blocked unconditionally
- ✅ No excessive notifications — suppressed when `NotificationFatigueScore >= threshold`
- ✅ No Sidecar authority — all decisions made by .NET backend; sidecar provides scores only
- ✅ Blocked recommendations go to audit log only; never persisted to `personalization_recommendations`

## Security ✅
- ✅ Ownership validation on all `PersonalizationEndpoints` (7 routes) and `CoachEndpoints` (2 routes) — JWT `sub`/`NameIdentifier` must match route `playerId`; returns `403 Forbidden` otherwise

## Admin Tuning ✅
All endpoints implemented:
- ✅ `GET /admin/personalization/summary`
- ✅ `GET /admin/personalization/archetypes`
- ✅ `GET /admin/personalization/player/{playerId}`
- ✅ `GET /admin/personalization/debug/{playerId}`
- ✅ `POST /admin/personalization/player/{playerId}/recalculate`
- ✅ `POST /admin/personalization/player/{playerId}/reset`
- ✅ `GET /admin/personalization/rules`
- ✅ `PUT /admin/personalization/rules/{ruleKey}`
- ✅ `GET /admin/personalization/recommendations/performance`

## A/B Testing
- Retention, engagement, conversion tracked via `store_item_purchased`, `notification_opened`, `notification_dismissed` behavior events

## .NET 10 Checklist ✅
- ✅ Docker updated — all Dockerfiles use `mcr.microsoft.com/dotnet/sdk:10.0` / `aspnet:10.0`
- ✅ EF migrations valid — 3 personalization migrations in sequence
- ✅ Auth stable
- ✅ Ownership validation added to personalization endpoints

## Goal ✅
Observable, explainable, controllable, safe system — achieved.
