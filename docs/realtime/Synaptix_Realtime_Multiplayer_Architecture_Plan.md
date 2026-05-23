# Synaptix Realtime Multiplayer Architecture Plan

## Building Nakama-Level Capabilities Natively in C#/.NET

## 1. Executive Summary

This document outlines the recommended architecture and implementation strategy for building a Nakama-level multiplayer backend platform directly within the existing Synaptix/TycoonTycoon `.NET` ecosystem, rather than integrating an external game backend platform.

The objective is to evolve the current backend into a fully modular, scalable, server-authoritative multiplayer infrastructure capable of supporting:

- Realtime multiplayer trivia
- Matchmaking
- Competitive ranking
- Parties and lobbies
- Presence tracking
- Tournaments
- Seasonal progression
- Realtime synchronization
- Anti-cheat validation
- Social features
- Analytics and Theory of Mind systems

This strategy preserves:

- Full C#/.NET ecosystem consistency
- Tight integration with wallet and economy systems
- Existing backend investments
- Existing observability stack
- Existing admin and moderation tooling
- Existing gateway architecture
- Existing PostgreSQL infrastructure

## 2. Why Not Integrate Nakama

The current backend already contains significant architectural investments:

- ASP.NET Core APIs
- YARP API Gateway
- PostgreSQL
- Redis
- RabbitMQ
- SignalR
- Hangfire
- JWT authentication
- Wallet and economy systems
- Admin tooling
- Analytics systems
- Anti-cheat concepts
- Seasonal progression
- Theory of Mind systems

Integrating Nakama would introduce:

- Runtime fragmentation
- Additional operational complexity
- Go, TypeScript, or Lua runtime dependencies
- Duplicated state ownership
- Split business logic authority
- Increased debugging complexity

Instead, the recommended approach is:

> Build a dedicated Synaptix Game Services layer entirely in C#/.NET.

## 3. High-Level Architecture

```text
Flutter Client
    |
    v
YARP Gateway
    |
    +---------------------------------------------------+
    |                                                   |
    v                                                   v
Business APIs                                    Game Services Layer
(.NET APIs)                                      (.NET Realtime)
    |                                                   |
    +---------------------+-----------------------------+
                          |
                          v
                    PostgreSQL
                          |
                          v
                       Redis
                          |
                          v
                     RabbitMQ
```

## 4. Core Architectural Principle

The most important architectural concept is:

> Server-authoritative gameplay.

The Flutter client should never be trusted to determine:

- Match outcomes
- XP rewards
- Question timing
- Correct answers
- Ranking changes
- Currency grants
- Anti-cheat decisions

The backend becomes the sole authority.

## 5. Recommended Services

### 5.1 `Tycoon.GameServer`

**Purpose:** Dedicated realtime multiplayer orchestration service.

**Responsibilities:**

- Match creation
- Match lifecycle management
- Realtime synchronization
- Question progression
- Tick loops
- Answer validation
- State broadcasting
- Match completion
- Event publishing

**Recommended technology:**

- ASP.NET Core
- SignalR
- Redis backplane
- Hosted Services
- gRPC internal communication

### 5.2 `Tycoon.Matchmaking`

**Purpose:** Player queue and matchmaking engine.

**Responsibilities:**

- Queue management
- Skill/MMR matching
- Region matching
- Party matchmaking
- Timeout handling
- Match allocation
- Duplicate queue prevention

**Recommended infrastructure:**

- Redis Sorted Sets
- Redis Streams
- PostgreSQL persistence
- Background workers

### 5.3 `Tycoon.Presence`

**Purpose:** Realtime player session tracking.

**Responsibilities:**

- Online/offline tracking
- Session management
- Device tracking
- Active match tracking
- Party membership tracking
- Reconnect handling

**Recommended infrastructure:**

- Redis
- SignalR
- Distributed cache

### 5.4 `Tycoon.Competitive`

**Purpose:** Competitive progression systems.

**Responsibilities:**

- Leaderboards
- Tournaments
- Seasons
- Ranked tiers
- Promotion/demotion
- Rewards
- Snapshot generation

**Recommended infrastructure:**

- PostgreSQL
- Redis caching
- Hangfire jobs

### 5.5 `Tycoon.GameEvents`

**Purpose:** Event-driven synchronization and analytics pipeline.

**Responsibilities:**

- `MatchStarted`
- `AnswerSubmitted`
- `MatchCompleted`
- `RewardGranted`
- `AntiCheatFlagged`
- `SeasonPointsAwarded`

**Recommended infrastructure:**

- RabbitMQ
- Redis Streams
- OpenTelemetry tracing

## 6. Recommended Repository Structure

```text
src/
 ├── Tycoon.GameServer/
 │    ├── Hubs/
 │    ├── Runtime/
 │    ├── Matchmaking/
 │    ├── Services/
 │    ├── State/
 │    └── Workers/
 │
 ├── Tycoon.Matchmaking/
 │    ├── Services/
 │    ├── Queue/
 │    ├── Rating/
 │    └── Workers/
 │
 ├── Tycoon.Presence/
 │    ├── Services/
 │    ├── Tracking/
 │    └── Redis/
 │
 ├── Tycoon.Competitive/
 │    ├── Leaderboards/
 │    ├── Seasons/
 │    ├── Tournaments/
 │    └── Rewards/
 │
 ├── Tycoon.GameEvents/
 │    ├── Contracts/
 │    ├── Publishers/
 │    ├── Consumers/
 │    └── Pipelines/
 │
 └── Tycoon.Shared/
      ├── Contracts/
      ├── DTOs/
      ├── Enums/
      ├── Extensions/
      └── Utilities/
```

## 7. Realtime Infrastructure Design

### 7.1 SignalR Match Hubs

Recommended hubs:

- `MatchHub`
- `PartyHub`
- `PresenceHub`
- `ChatHub`
- `TournamentHub`

### 7.2 Match Runtime

Recommended core components:

- `MatchActor`
- `MatchLoop`
- `MatchState`
- `PlayerConnection`
- `AnswerValidator`
- `QuestionScheduler`
- `RewardDispatcher`
- `AntiCheatValidator`

### 7.3 Match Tick Loop

Recommended tick-loop behavior:

- 20-30 ticks per second
- State updates pushed to connected clients
- Authoritative answer timing
- Synchronization validation

### 7.4 Operator Dashboard Realtime Strategy

The backend already exposes realtime infrastructure through SignalR hubs (`/ws/match`, `/ws/presence`, `/ws/notify`) and a raw WebSocket presence endpoint (`/ws`). These endpoints are primarily player/mobile-facing today.

`Synaptix.OperatorDashboard.Django` should remain the canonical operator UI, but it should not assume all dashboard data needs full push-based realtime delivery. Most operator views are administrative, auditable, and permission-gated, so near-realtime freshness is usually safer and simpler than persistent browser subscriptions.

Recommended strategy:

- Use lightweight polling for broad dashboard freshness:
  - health/status cards
  - moderation queues
  - anti-cheat lists
  - event queue status
  - notification/dead-letter status
  - store/player stock views
  - personalization summaries
- Use SignalR for true realtime operator streams where push semantics materially improve response time:
  - live game-event lifecycle updates
  - new anti-cheat flags
  - notification dispatch/dead-letter changes
  - matchmaking/presence incident indicators
  - critical operational alerts
- Consider SSE for one-way operational feeds if SignalR is unnecessary for a specific dashboard stream.

Security and architecture rules:

- Keep Django session auth and Django permission checks as the operator-facing authority.
- Never expose `X-Admin-Ops-Key` or backend service credentials to browser JavaScript.
- Browser clients should subscribe only through operator-safe channels or through Django-issued short-lived subscription credentials.
- Mutating operator actions must continue to use POST-backed Django views or BFF endpoints with CSRF/session protection.
- Realtime messages should be treated as refresh hints; authoritative state should still be reloaded through existing admin APIs before an operator takes action.

This keeps the dashboard operationally fresh without coupling every admin workflow to persistent WebSocket state.

## 8. Matchmaking Design

### Queue Flow

```text
Player Enters Queue
        |
        v
Redis Queue Storage
        |
        v
Matchmaker Worker
        |
        v
Match Candidate Generation
        |
        v
Validation Rules
        |
        v
GameServer Allocation
        |
        v
SignalR Match Session
```

## 9. Presence Architecture

### Presence Tracking

Each player session tracks:

- User ID
- Device ID
- Current match
- Current party
- Last heartbeat
- Region
- Connection ID

## 10. Competitive Systems

### 10.1 Leaderboards

Recommended leaderboard types:

- Global
- Weekly
- Daily
- Regional
- Tier-specific
- Tournament-specific

### 10.2 Tournaments

Recommended tournament features:

- Scheduled tournaments
- Bracket tournaments
- Swiss-style tournaments
- Seasonal tournaments
- Event-based tournaments

### 10.3 Seasons

Recommended season systems:

- Tier resets
- Placement matches
- Seasonal rewards
- Seasonal XP
- Promotion tracking

## 11. Anti-Cheat Architecture

### Key Principle

Clients should never directly grant:

- Rewards
- XP
- Ranking
- Coins
- Progression

All reward decisions must occur server-side.

### Anti-Cheat Validation Layers

Recommended validation types:

- Answer timing validation
- Duplicate answer detection
- Impossible response speed
- Match replay validation
- Packet integrity validation
- Multi-session abuse detection
- Suspicious win-rate analysis

## 12. Infrastructure Requirements

### Required Core Infrastructure

| Component | Purpose |
|---|---|
| PostgreSQL | Source of truth |
| Redis | Realtime state/cache |
| RabbitMQ | Event messaging |
| SignalR | Realtime communication |
| YARP | Gateway routing |
| Hangfire | Background jobs |
| OpenTelemetry | Observability |
| Grafana | Monitoring |
| Prometheus | Metrics |
| MinIO | Object storage |

## 13. Scaling Requirements

### Initial Scaling Strategy

- Horizontal scaling
- Multiple `GameServer` instances
- Redis backplane
- Sticky sessions
- Distributed matchmaking queues

### Future Scaling

- Advanced scaling
- Match sharding
- Regional routing
- Dedicated realtime clusters
- Kubernetes orchestration
- Match affinity balancing

## 14. Recommended Development Phases

### Phase 1: Realtime POC

**Goals:**

- SignalR realtime matches
- Match state synchronization
- Simple authoritative gameplay
- Optional lightweight polling refresh for critical Operator Dashboard health/status cards

**Estimated time:** 2-4 weeks

### Phase 2: Matchmaking

**Goals:**

- Queue system
- MMR matching
- Match allocation
- Optional polling refresh for operator queue and incident indicators

**Estimated time:** 4-6 weeks

### Phase 3: Presence and Parties

**Goals:**

- Party creation
- Presence tracking
- Reconnect handling

**Estimated time:** 4-8 weeks

### Phase 4: Competitive Systems

**Goals:**

- Ranked systems
- Seasons
- Leaderboards
- Tournament scheduling

**Estimated time:** 8-12 weeks

### Phase 5: Anti-Cheat and Replay Validation

**Goals:**

- Replay validation
- Event audit
- Server-side enforcement

**Estimated time:** 8-16 weeks

### Phase 6: Full Platform Maturity

**Goals:**

- Advanced scaling
- Operational tooling
- Realtime dashboards with SignalR-backed operational streams
- Cluster orchestration

**Estimated time:** 6-12 months

## 15. Recommended Technology Stack

| Layer | Technology |
|---|---|
| Frontend | Flutter |
| Gateway | YARP |
| APIs | ASP.NET Core |
| Realtime | SignalR |
| Database | PostgreSQL |
| Cache | Redis |
| Messaging | RabbitMQ |
| Background Jobs | Hangfire |
| Object Storage | MinIO |
| Analytics | FastAPI |
| Monitoring | Grafana |
| Metrics | Prometheus |
| Tracing | OpenTelemetry |

## 16. Strategic Advantages of This Approach

### Full Ecosystem Consistency

The entire platform remains aligned around:

- C#
- .NET
- ASP.NET Core
- EF Core
- SignalR

### Better Integration

This approach provides tighter integration with:

- Wallet systems
- XP systems
- Admin tools
- Theory of Mind systems
- Analytics
- Anti-cheat
- Moderation

### Full Ownership

This avoids dependency on:

- Third-party runtime engines
- Go/Lua scripting
- External multiplayer frameworks

### Easier Operational Governance

The platform keeps unified:

- Deployment
- Monitoring
- Logging
- Observability
- CI/CD

## 17. Final Recommendation

The recommended strategy for Synaptix is:

> Build a dedicated `.NET` game platform layer.

The recommendation is not to integrate Nakama or replace existing backend systems.

The current backend already contains most foundational components required to achieve Nakama-level capabilities. The missing pieces are primarily:

- Authoritative realtime runtime
- Advanced matchmaking
- Scalable presence systems
- Multiplayer orchestration
- Competitive lifecycle management

These can all be implemented natively within the existing `.NET` architecture while preserving:

- Architectural consistency
- Operational control
- Scalability
- Monetization flexibility
- Security
- Backend ownership

## 18. Recommended Immediate Next Steps

### Priority Order

1. Build `Tycoon.GameServer`
   - SignalR hubs
   - Match state
   - Realtime synchronization
2. Build matchmaking service
   - Queue system
   - Redis integration
   - MMR matching
3. Build presence layer
   - Online tracking
   - Session registry
   - Party state
4. Implement server-authoritative match runtime
   - Tick loops
   - Match actors
   - Answer validation
5. Implement competitive systems
   - Seasons
   - Leaderboards
   - Tournament orchestration

## End of Document
