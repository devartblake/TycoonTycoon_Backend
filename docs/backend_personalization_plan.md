# Synaptix Backend Personalization Hardening Plan

## Objective
Stabilize and productionize the Unified Personalization Layer.

## Core Principle
.NET Backend = Authority
Sidecar = Intelligence

## Observability
- Add personalization_audit_log
- Track metrics: acceptance, churn, frustration, conversion
- Structured logs for decisions

## Explainability
- GET /personalization/debug/{playerId}
- Add reason field to recommendations

## Feature Flags
- Personalization:Enabled
- Personalization:UseSidecar
- Personalization:AdaptiveQuestions
- Personalization:AdaptiveMissions
- Personalization:AdaptiveStore
- Personalization:AdaptiveNotifications
- Personalization:CoachEnabled

## Guardrails
- No monetization exploitation
- No ranked manipulation
- No excessive notifications
- No Sidecar authority

## Admin Tuning
Endpoints:
- /admin/personalization/summary
- /admin/personalization/archetypes
- /admin/personalization/player/{playerId}
- /admin/personalization/rules

## A/B Testing
Track retention, engagement, conversion

## .NET 10 Checklist
- Docker updated
- EF migrations valid
- Auth stable
- SignalR working
- CORS verified

## Goal
Observable, explainable, controllable, safe system
