# Synaptix Unified Personalization Layer Plan

## Purpose
A unified backend-driven personalization system connecting gameplay, learning, study, store, notifications, missions, and ML.

## Core Principle
.NET Backend = authoritative rules  
FastAPI Sidecar = intelligence  
Flutter = UI

## Key Domain
PlayerMindProfile

## Responsibilities
- Track behavior (confidence, pace, risk, etc.)
- Drive adaptive gameplay
- Enable safe personalization

## Use Cases
- Adaptive questions
- Learning/study recommendations
- Personalized missions
- Store personalization (non-exploitative)
- Notification tone/timing
- Matchmaking improvements
- Coach AI suggestions

## Architecture
Flutter → .NET → Personalization → Sidecar → .NET → UI

## APIs
GET /personalization/home/{playerId}  
GET /personalization/recommendations/{playerId}  
GET /coach/{playerId}/daily-brief  

## Guardrails
- No pay-to-win manipulation
- No frustration exploitation
- Notification limits
- Fair gameplay enforcement

## Outcome
A fun, adaptive, and fair experience.
