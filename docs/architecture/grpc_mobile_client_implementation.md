# gRPC Mobile Client Implementation

**Date:** 2026-06-04  
**Status:** Implemented  
**Service:** `MobileMatchGrpcService` — port 5001 (HTTP/2)  
**Proto:** `protos/mobile.proto`

---

## Overview

The backend exposes a gRPC service alongside the existing REST API and SignalR hubs. Each transport targets a specific client type:

| Transport | Port | Clients |
|-----------|------|---------|
| REST (HTTP/1.1) | 5000 | Browser admin, operator dashboard, general API |
| SignalR WebSocket | 5000 | Browser notifications, presence, DMs |
| gRPC (HTTP/2) | 5001 | Flutter mobile/desktop/web (native game client) |

---

## Service Definition

**File:** `protos/mobile.proto` — package `tycoon.mobile`

### RPCs

| RPC | Type | Purpose |
|-----|------|---------|
| `StartMatch` | Unary | Start a match; mirrors `POST /mobile/matches/start` |
| `SubmitMatch` | Unary | Submit results; mirrors `POST /mobile/matches/submit` |
| `PlayMatch` | Bidirectional stream | Live match session (questions → answers → scores) |
| `WatchLeaderboard` | Server stream | Live rank-neighbourhood updates (polls every 15s) |
| `WatchMatchmaking` | Server stream | Enter queue; receive status until `Matched` or cancel |
| `CancelMatchmaking` | Unary | Withdraw from matchmaking queue |

### Authentication

All RPCs require an `authorization: Bearer <jwt>` gRPC metadata header. The JWT is the same token issued by `POST /auth/login` (REST).

---

## Implementation

**Service file:** `Synaptix.Backend.Api/Grpc/MobileMatchGrpcService.cs`

### Injected services

| Service | Purpose |
|---------|---------|
| `IMediator` | Dispatches match start/submit/leaderboard requests |
| `IAppDb` | Database access for answer evaluation |
| `MatchmakingService` | Enqueue, poll status, cancel queue tickets |
| `ILogger` | Structured logging |

### `PlayMatch` — fan-out architecture

Active bidirectional streams are stored in a `ConcurrentDictionary<string, MatchSession>` keyed by `matchId`. When a player submits an answer, the `MatchSession` broadcasts the opponent's score update to all other participants in the same match. This avoids Redis/SignalR for match-internal events (lower latency).

### `WatchLeaderboard` — polling

Currently polls `GetTierLeaderboard` on a 15-second interval (configurable via `MOBILE_MATCH_LEADERBOARD_POLL_SECONDS` env var, 1–60s range). Future enhancement: replace with Redis pub/sub change-stream when that plumbing is promoted to a shared notification channel.

### `WatchMatchmaking` — polling

Enqueues the player via `MatchmakingService.EnqueueAsync`, pushes an initial `Queued` status, then polls `GetStatusAsync` every 2 seconds until `Matched` or `Cancelled`. Returns the `opponent_id` when matched. Match creation and opponent notification are handled by the existing `MatchmakingService.TryMatchAsync` + `IMatchmakingNotifier.NotifyMatchedAsync` flow.

---

## Track 3+ Deferred RPCs

| RPC | Blocked by | Priority |
|-----|-----------|---------|
| `SpectateMatch` (server stream) | No backend spectate service yet | P2 |
| `StreamAnalyticsEvents` (client stream) | REST analytics sufficient for now | P3 |
| `GetMatchHistory` (unary) | REST `/matches/{id}` sufficient for now | P3 |

---

## Configuration

| Env var | Default | Description |
|---------|---------|-------------|
| `BACKEND_GRPC_PORT` | 5001 | gRPC port in Docker compose |
| `MOBILE_MATCH_LEADERBOARD_POLL_SECONDS` | 15 | Leaderboard push interval |

---

## Flutter client files

See `C:\Users\lmxbl\StudioProjects\trivia_tycoon\docs\GRPC_INTEGRATION.md` for the Flutter-side setup.

| Flutter file | Purpose |
|-------------|---------|
| `protos/mobile.proto` | Copy of backend proto (kept in sync manually) |
| `lib/core/networking/grpc/generated/` | Auto-generated Dart stubs (run `scripts/generate_proto.sh`) |
| `lib/core/networking/grpc/grpc_channel_manager.dart` | Channel singleton (native + web) |
| `lib/core/networking/grpc/grpc_auth_interceptor.dart` | JWT injection interceptor |
| `lib/core/networking/grpc/grpc_match_client.dart` | Typed stub wrapper |
| `lib/core/services/grpc_match_service.dart` | Business-logic façade |
| `lib/game/providers/grpc_providers.dart` | Riverpod providers |
