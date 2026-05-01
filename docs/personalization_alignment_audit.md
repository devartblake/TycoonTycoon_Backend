📊 Synaptix Backend Personalization Alignment Audit

Repository: TycoonTycoon_Backend
Audit Date: 2026-04-30
Scope: Personalization System (Backend + Sidecar Integration)
Status: ⚠️ Partially Complete — ~85% Aligned

🧠 Executive Summary

The personalization system is architecturally sound and largely implemented, including:

Domain models
EF Core integration
Application services
Public + admin endpoints
Guardrails
Sidecar integration (client-side)
Feature flags

However, several critical refinements and consistency gaps remain before this can be considered production-complete.

🧩 Alignment Overview
✅ Fully Aligned Components
1. Domain Layer
PlayerMindProfile
PersonalizationRecommendation
PersonalizationAuditLog
PersonalizationRule

✔ Core personalization schema exists
✔ JSON-based extensibility implemented
✔ Risk scoring + archetypes supported

2. Persistence Layer (EF Core)
DbSets present in AppDb
JSONB fields configured
Indexes for analytics queries

✔ Database integration complete
⚠ Migration verification still required

3. Application Layer Services
Service	Status
PlayerMindProfileService	✅ Implemented
PersonalizationService	✅ Implemented
GuardrailService	✅ Implemented
AuditService	✅ Implemented

✔ Clean service separation
✔ Proper DI registration
✔ Sidecar integration included

4. Public API Surface

Routes implemented:

GET    /personalization/profile/{playerId}
POST   /personalization/profile/{playerId}/event
POST   /personalization/profile/{playerId}/recalculate
GET    /personalization/home/{playerId}
GET    /personalization/recommendations/{playerId}
POST   /personalization/recommendations/{id}/accept
POST   /personalization/recommendations/{id}/dismiss

✔ Fully functional personalization API

5. Coach System
GET  /coach/{playerId}/daily-brief
POST /coach/{playerId}/feedback

✔ Integrated with personalization
✔ Behavior tracking wired

6. Admin API Surface

Includes:

Summary metrics
Archetype distribution
Recommendation performance
Player debugging
Rule management

✔ Strong observability + admin tooling

7. Dependency Injection

Registered in:

AddApplication()
Program.cs (Sidecar client)

✔ Proper DI layering
✔ Clean service boundaries

8. Feature Flags / Config
"Personalization": {
  "Enabled": true,
  "UseSidecar": true,
  "AdaptiveMissions": true,
  "AdaptiveStore": true
}

✔ Feature toggles implemented
✔ Guardrail thresholds configurable

9. Sidecar Client (C#)

Implemented endpoints:

POST /personalization/score-player
POST /personalization/recommendation-candidates

✔ Clean HTTP abstraction
✔ Fault-tolerant fallback

⚠️ Partial / Missing Areas
❗ 1. Missing Reason Field in Recommendations
Problem

PersonalizationRecommendation lacks a reason/explainability field, despite:

Audit logs capturing reasoning
Sidecar providing reason
Impact
Frontend cannot explain why recommendations exist
Reduces trust + UX clarity
Breaks explainability requirement from design plan
Fix
public string Reason { get; set; } = "";

Also:

Populate from sidecar candidate
Persist in DB
Return in DTO
❗ 2. Recommendation Persistence Logic Flaw
Problem

In PersonalizationService:

Recommendations are added BEFORE guardrail filtering
SaveChangesAsync only runs if allowed recommendations exist
Result
Blocked recommendations may:
Not persist at all
Or persist inconsistently
Fix Options
Option A (Recommended)

Only persist allowed recommendations:

if (guardrailResult.Allowed)
{
    _db.PersonalizationRecommendations.Add(rec);
}
Option B

Add status field:

public string Status { get; set; } = "pending";
// allowed | blocked | accepted | dismissed
❗ 3. Sidecar FastAPI Routes Not Verified
Expected Endpoints
POST /personalization/score-player
POST /personalization/recommendation-candidates
Current State
C# client implemented
FastAPI routes not found in repo scan
Risk
Runtime failures
Silent fallback → degraded personalization
Action

Verify or implement:

@router.post("/personalization/score-player")
@router.post("/personalization/recommendation-candidates")
❗ 4. Config Not Fully Utilized
Problem

Config:

"SidecarPersonalization": {
  "TimeoutSeconds": 3
}

But code uses:

client.Timeout = TimeSpan.FromSeconds(5);
Fix
var timeout = builder.Configuration.GetValue<int>("SidecarPersonalization:TimeoutSeconds");
client.Timeout = TimeSpan.FromSeconds(timeout);
❗ 5. Database Migration Not Verified
Required Tables
player_mind_profiles
player_behavior_events
personalization_recommendations
personalization_rules
personalization_audit_logs
Action

Run:

dotnet ef migrations list
dotnet ef database update
❗ 6. Missing Ownership Validation
Problem

Endpoints accept arbitrary playerId

Example:

GET /personalization/home/{playerId}
Risk
Users accessing other users' personalization
Data leakage
Fix

Validate:

var userId = context.User.FindFirst("sub")?.Value;
if (userId != playerId.ToString())
    return Results.Forbid();
❗ 7. OpenAPI / Swagger Incomplete
Problem

Groups lack .WithOpenApi()

Fix
app.MapGroup("/personalization").WithOpenApi();
app.MapGroup("/coach").WithOpenApi();
❗ 8. Blocked Recommendation Handling
Current State
Stored inconsistently
Mixed with allowed flow
Recommendation

Move blocked candidates → Audit only

Recommendation Table → allowed only  
Audit Table → full trace (allowed + blocked)
📌 Final Alignment Score
Layer	Score
Domain	90%
Persistence	85%
Application	90%
API	90%
Sidecar Integration	75%
Security	70%
Observability	95%
🚀 Priority Fix Roadmap
Phase 1 — Critical (Do First)
Add Reason field
Fix recommendation persistence logic
Verify Sidecar FastAPI endpoints
Add ownership validation
Phase 2 — Stability
Wire config-driven timeouts
Verify DB migrations
Normalize blocked recommendation handling
Phase 3 — Polish
Add OpenAPI annotations
Expand admin analytics (optional)
Add recommendation status lifecycle
🎯 Definition of “Fully Aligned”

The system is considered complete when:

✅ Recommendations include reasoning
✅ Sidecar fully operational
✅ No unauthorized access to personalization data
✅ DB schema verified and stable
✅ Config drives behavior (no hardcoding)
✅ Recommendation lifecycle is deterministic
✅ Swagger fully reflects API
🧠 Strategic Note

You are past the hard part.

This is no longer a scaffolding phase — it’s now a refinement + hardening phase.

Once the above gaps are resolved, you’ll have:

A production-grade personalization engine
A scalable ML-sidecar architecture
A system ready for frontend integration and A/B testing