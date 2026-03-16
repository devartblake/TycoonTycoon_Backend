# Flutter Integration Guide — Trivia Tycoon Backend

This document is the authoritative reference for connecting the Flutter client to the Trivia Tycoon backend. It covers every integration point: authentication, the full REST API surface, real-time SignalR events, feature flows, and error handling.

> **Base URL:** configure per environment. All paths below are relative to this base.
> **API Version:** v1 (no version prefix in path — `Content-Type: application/json` on all requests).

---

## Contents

1. [Project Setup](#1-project-setup)
2. [Authentication](#2-authentication)
3. [API Reference](#3-api-reference)
   - [Auth](#auth)
   - [Users & Players](#users--players)
   - [Matches](#matches)
   - [Matchmaking](#matchmaking)
   - [Leaderboards](#leaderboards)
   - [Seasons](#seasons)
   - [Missions](#missions)
   - [Skills](#skills)
   - [Powerups](#powerups)
   - [Economy / Wallet](#economy--wallet)
   - [Referrals & QR](#referrals--qr)
   - [Votes](#votes)
   - [Game Events](#game-events)
   - [Guardians](#guardians)
   - [Territory](#territory)
   - [Event Stats & Leaderboards](#event-stats--leaderboards)
4. [Real-Time (SignalR)](#4-real-time-signalr)
5. [Feature Flows](#5-feature-flows)
   - [Game Event (Battle Royale / Champion Battle)](#game-event-flow)
   - [Guardian Challenge](#guardian-challenge-flow)
   - [Territory Capture](#territory-capture-flow)
   - [Ranked Match](#ranked-match-flow)
6. [Event System Activation Controls](#6-event-system-activation-controls)
7. [Error Handling](#7-error-handling)
8. [Rate Limits](#8-rate-limits)

---

## 1. Project Setup

### Recommended `pubspec.yaml` additions

```yaml
dependencies:
  dio: ^5.4.0                    # HTTP client with interceptors
  signalr_netcore: ^1.3.4        # SignalR WebSocket client
  flutter_secure_storage: ^9.2.2 # Secure JWT token storage
  json_annotation: ^4.9.0        # DTO serialization helpers

dev_dependencies:
  json_serializable: ^6.8.0
  build_runner: ^2.4.0
```

### Environment configuration

```dart
// lib/config/app_config.dart
class AppConfig {
  static const String baseUrl       = String.fromEnvironment('BASE_URL',
                                        defaultValue: 'http://10.0.2.2:5000');
  static const String notifyHubPath = '/ws/notify';
  static const String matchHubPath  = '/ws/match';
  static const String jwtAudience   = 'mobile-app';
}
```

> Pass `--dart-define=BASE_URL=https://api.yourserver.com` in your `flutter run` / `flutter build` commands for each environment.

### Dio client with auth interceptor

```dart
// lib/network/api_client.dart
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  final Dio _dio;
  final FlutterSecureStorage _storage;

  ApiClient(this._storage)
      : _dio = Dio(BaseOptions(
          baseUrl: AppConfig.baseUrl,
          headers: {'Content-Type': 'application/json'},
          connectTimeout: const Duration(seconds: 10),
          receiveTimeout: const Duration(seconds: 15),
        )) {
    _dio.interceptors.add(_AuthInterceptor(_storage, _dio));
  }

  Dio get client => _dio;
}

class _AuthInterceptor extends QueuedInterceptorsWrapper {
  final FlutterSecureStorage _storage;
  final Dio _dio;

  _AuthInterceptor(this._storage, this._dio);

  @override
  Future<void> onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await _storage.read(key: 'access_token');
    if (token != null) options.headers['Authorization'] = 'Bearer $token';
    handler.next(options);
  }

  @override
  Future<void> onError(DioException err, ErrorInterceptorHandler handler) async {
    if (err.response?.statusCode == 401) {
      final refreshed = await _tryRefresh();
      if (refreshed) {
        // Retry the original request with new token
        final token = await _storage.read(key: 'access_token');
        err.requestOptions.headers['Authorization'] = 'Bearer $token';
        final response = await _dio.fetch(err.requestOptions);
        return handler.resolve(response);
      }
      // Refresh failed — redirect to login
    }
    handler.next(err);
  }

  Future<bool> _tryRefresh() async {
    final refreshToken = await _storage.read(key: 'refresh_token');
    if (refreshToken == null) return false;
    try {
      final res = await _dio.post('/auth/refresh', data: {'refreshToken': refreshToken});
      await _storage.write(key: 'access_token',  value: res.data['accessToken']);
      await _storage.write(key: 'refresh_token', value: res.data['refreshToken']);
      return true;
    } catch (_) {
      return false;
    }
  }
}
```

---

## 2. Authentication

All user tokens carry audience `"mobile-app"`. Do **not** use admin tokens in the Flutter app.

### JWT claims issued in the access token

| Claim | Value |
|---|---|
| `sub` | User UUID (use as `userId`) |
| `email` | User email address |
| `handle` | Display username / handle |
| `role` | `"user"` |
| `scope` | `"profile:read profile:write gameplay:read gameplay:write"` |
| `client_type` | `"user"` |

Access token lifetime: **60 minutes** (configurable server-side).
Refresh token lifetime: **30 days**.

### Storage keys (FlutterSecureStorage)

| Key | Content |
|---|---|
| `access_token` | JWT bearer string |
| `refresh_token` | Opaque refresh string |
| `device_id` | Stable UUID, generated once on first launch |
| `user_id` | UUID from `sub` claim |

---

## 3. API Reference

> **Auth Required** column: `✓` = `Authorization: Bearer <token>` header required; `–` = public.

---

### Auth

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/auth/signup` | – | Register + login in one call (preferred for mobile) |
| `POST` | `/auth/login` | – | Login with email + password |
| `POST` | `/auth/register` | – | Register only (no token returned) |
| `POST` | `/auth/refresh` | – | Rotate access + refresh tokens |
| `POST` | `/auth/logout` | ✓ | Revoke current device's refresh token |

#### `POST /auth/signup`
```json
// Request
{
  "email": "player@example.com",
  "password": "SecureP@ss1",
  "deviceId": "<stable-uuid>",
  "username": "TycoonKing",
  "country": "US"
}

// Response 200
{
  "accessToken": "<jwt>",
  "refreshToken": "<opaque>",
  "expiresIn": 3600,
  "userId": "<uuid-string>",
  "user": { "id": "<uuid>", "handle": "TycoonKing", "email": "player@example.com", "country": "US", "tier": null, "mmr": 0 }
}
```

#### `POST /auth/login`
```json
// Request
{ "email": "player@example.com", "password": "SecureP@ss1", "deviceId": "<stable-uuid>" }

// Response 200 — same shape as signup (without "userId" top-level field)
{ "accessToken": "<jwt>", "refreshToken": "<opaque>", "expiresIn": 3600, "user": { ... } }
```

#### `POST /auth/refresh`
```json
// Request
{ "refreshToken": "<opaque>" }

// Response 200 — same as login response
```

#### `POST /auth/logout`
```json
// Request
{ "deviceId": "<stable-uuid>" }
// Response 204 No Content
```

---

### Users & Players

> **Prefer the `/mobile/` endpoints** for the Flutter app — they are identical but may receive mobile-specific optimisations first.

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/mobile/players` | – | Create a player profile after signup |
| `GET` | `/mobile/players/{playerId}` | – | Get player profile |
| `PATCH` | `/users/me` | ✓ | Update handle or country |

#### `POST /mobile/players`
```json
// Request
{ "username": "TycoonKing", "countryCode": "US" }

// Response 200
{ "id": "<uuid>", "username": "TycoonKing", "countryCode": "US", "level": 1, "xp": 0.0 }
```

#### `GET /mobile/players/{playerId}`
```json
// Response 200
{ "id": "<uuid>", "username": "TycoonKing", "countryCode": "US", "level": 5, "xp": 1240.0 }
```

#### `PATCH /users/me`
```json
// Request
{ "handle": "NewHandle", "country": "CA" }
// Response 204 No Content
```

---

### Matches

> Mobile prefix variants (`/mobile/matches/...`) mirror the standard endpoints.

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/mobile/matches/start` | – | Create a match record |
| `POST` | `/mobile/matches/submit` | – | Submit scores + get awards |
| `GET` | `/matches/{matchId}` | – | Get match detail |

#### `POST /mobile/matches/start`
```json
// Request
{ "hostPlayerId": "<uuid>", "mode": "ranked" }

// Response 200
{ "matchId": "<uuid>", "startedAt": "2026-03-16T12:00:00Z" }
```

#### `POST /mobile/matches/submit`
The `eventId` is a client-generated UUID used for idempotency — submit the same `eventId` twice and the second call returns `"Duplicate"` without double-awarding.
```json
// Request
{
  "eventId": "<client-uuid>",
  "matchId": "<uuid>",
  "mode": "ranked",
  "category": "general",
  "questionCount": 10,
  "startedAtUtc": "2026-03-16T12:00:00Z",
  "endedAtUtc": "2026-03-16T12:05:30Z",
  "status": 1,
  "participants": [
    { "playerId": "<uuid>", "score": 750, "correct": 8, "wrong": 2, "avgAnswerTimeMs": 3200 },
    { "playerId": "<uuid>", "score": 500, "correct": 6, "wrong": 4, "avgAnswerTimeMs": 4100 }
  ]
}

// Response 200
{
  "eventId": "<client-uuid>",
  "matchId": "<uuid>",
  "status": "Applied",
  "awards": [
    { "playerId": "<uuid>", "awardedXp": 120, "awardedCoins": 50 },
    { "playerId": "<uuid>", "awardedXp": 80,  "awardedCoins": 30 }
  ]
}
```

**`status` enum:** `1 = Completed`, `2 = Aborted`

---

### Matchmaking

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/matchmaking/enqueue` | – | Join the ranked matchmaking queue |
| `POST` | `/matchmaking/cancel` | – | Leave the queue |
| `GET` | `/matchmaking/status/{playerId}` | – | Check queue position / match found |

When a match is found the server pushes a SignalR event on the `NotificationHub` — see [Real-Time](#4-real-time-signalr) for the event name.

---

### Leaderboards

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/mobile/leaderboards/me/{playerId}` | – | Player's tier + rank position |
| `GET` | `/mobile/leaderboards/tiers/{tierId}` | – | Paginated leaderboard for a tier |
| `GET` | `/leaderboards/ranked` | – | Global ranked leaderboard |

#### `GET /mobile/leaderboards/me/{playerId}`
```json
// Response 200
{ "playerId": "<uuid>", "tierId": 3, "tierRank": 12, "globalRank": 312, "score": 4800, "xpProgress": 0.62 }
```

#### `GET /mobile/leaderboards/tiers/{tierId}?page=1&pageSize=50`
```json
// Response 200
{
  "tierId": 3,
  "page": 1, "pageSize": 50, "total": 100,
  "entries": [
    { "playerId": "<uuid>", "username": "TycoonKing", "countryCode": "US", "level": 10, "score": 5200, "globalRank": 1, "tierRank": 1, "xpProgress": 0.88 }
  ]
}
```

---

### Seasons

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/mobile/seasons/active` | – | Active season metadata |
| `GET` | `/mobile/seasons/state/{playerId}` | – | Player's ranked points + tier this season |

#### `GET /mobile/seasons/active`
```json
// Response 200
{
  "seasonId": "<uuid>", "seasonNumber": 3, "name": "Season 3",
  "status": 2,
  "startsAtUtc": "2026-03-01T00:00:00Z",
  "endsAtUtc": "2026-06-01T00:00:00Z"
}
// Response 204 when no active season
```

**`status` enum:** `1 = Scheduled`, `2 = Active`, `3 = Closed`

#### `GET /mobile/seasons/state/{playerId}`
```json
// Response 200
{ "playerId": "<uuid>", "seasonId": "<uuid>", "rankPoints": 3200, "wins": 24, "losses": 8, "draws": 2, "matchesPlayed": 34, "tier": 2, "tierRank": 7, "seasonRank": 57 }
```

---

### Missions

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/missions` | – | List all missions |
| `POST` | `/missions/progress/match-completed` | – | Notify mission system of a completed match |
| `POST` | `/missions/progress/round-completed` | – | Notify mission system of a completed round |
| `POST` | `/missions/{missionId}/claim` | – | Claim a completed mission reward |

When a mission is claimed the server pushes `MissionClaimed` via SignalR (see [Real-Time](#4-real-time-signalr)).

---

### Skills

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/skills/tree` | – | Full skill tree catalogue |
| `GET` | `/skills/state/{playerId}` | – | Player's unlocked skill keys |
| `POST` | `/skills/unlock` | – | Unlock a skill node |
| `POST` | `/skills/respec` | – | Reset all skills (partial currency refund) |

#### `POST /skills/unlock`
```json
// Request
{ "eventId": "<client-uuid>", "playerId": "<uuid>", "nodeKey": "double_xp_t1" }

// Response 200
{
  "eventId": "<client-uuid>", "playerId": "<uuid>", "nodeKey": "double_xp_t1",
  "status": "Unlocked",
  "unlockedKeys": ["double_xp_t1", "fast_answers_t1"]
}
// status: "Unlocked" | "Duplicate" | "MissingPrereq" | "NotFound" | "InsufficientFunds"
```

**Branch enum:** `1 = Knowledge`, `2 = Strategy`, `3 = Powerups`

---

### Powerups

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/powerups/state/{playerId}` | – | Player's powerup balances + cooldowns |
| `POST` | `/powerups/use` | – | Consume one powerup during a match |

#### `POST /powerups/use`
```json
// Request
{ "eventId": "<client-uuid>", "playerId": "<uuid>", "type": 1 }

// Response 200
{ "status": "Used", "remaining": 2, "cooldownUntilUtc": null }
// status: "Used" | "Duplicate" | "Insufficient" | "Cooldown"
```

**`type` enum:** `1 = FiftyFifty`, `2 = Skip`, `3 = DoublePoints`, `4 = ExtraTime`

---

### Economy / Wallet

Wallet balances are returned as part of `EconomyTxnResultDto` from mission claims, powerup usage, match awards, and event prizes. There is no standalone "get wallet" endpoint — balances appear in:
- Match submit awards (`SubmitMatchResponse.Awards`)
- Mission claim result
- Powerup use result

To check raw economy history:

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/economy/history/{playerId}` | – | Paginated transaction history |

```json
// Response 200
{
  "playerId": "<uuid>", "page": 1, "pageSize": 20, "total": 48,
  "items": [
    { "eventId": "<uuid>", "kind": "match-result", "lines": [{"currency": 1, "delta": 120}, {"currency": 2, "delta": 50}], "createdAtUtc": "..." }
  ]
}
```

**`currency` enum:** `1 = Xp`, `2 = Coins`, `3 = Diamonds`

---

### Referrals & QR

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/referrals` | – | Create a referral code for a player |
| `GET` | `/referrals/{code}` | – | Lookup a referral code |
| `POST` | `/referrals/{code}/redeem` | – | Redeem a code (awards both parties) |
| `POST` | `/qr/track-scan` | – | Record a QR scan event |
| `POST` | `/qr/sync` | – | Batch-upload offline QR scans |
| `GET` | `/qr/history/{playerId}` | – | Get player's QR scan history |

#### `POST /referrals/{code}/redeem`
```json
// Request
{ "eventId": "<client-uuid>", "redeemerPlayerId": "<uuid>" }

// Response 200
{
  "code": "ABC123", "ownerPlayerId": "<uuid>", "redeemerPlayerId": "<uuid>",
  "awardXpToOwner": 500, "awardCoinsToOwner": 100,
  "awardXpToRedeemer": 250, "awardCoinsToRedeemer": 50,
  "status": "Redeemed",
  "processedAtUtc": "..."
}
// status: "Redeemed" | "Duplicate" | "Invalid" | "SelfRedeemNotAllowed"
```

---

### Votes

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/votes` | ✓ | Cast a vote on a topic |
| `GET` | `/votes/{topic}/results` | – | Get live tally for a topic |

#### `POST /votes`
```json
// Request
{ "playerId": "<uuid>", "option": "A", "topic": "question-123" }

// Response 200
{ "voteId": "<uuid>", "playerId": "<uuid>", "option": "A", "topic": "question-123", "timestampUtc": "..." }
```

Connect to the `topic:{topic}` SignalR group to receive live `VoteCast` push events as other players vote.

---

### Game Events

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/game-events/upcoming` | – | List open/scheduled events |
| `GET` | `/game-events/{eventId}` | – | Get event status (participants, alive count, jackpot) |
| `POST` | `/game-events/enter` | ✓ | Enter an event (deducts entry fee) |
| `POST` | `/game-events/revive` | ✓ | Revive after elimination (costs gems) |

#### `GET /game-events/upcoming`
```json
// Response 200 — array of summaries
[
  { "id": "<uuid>", "kind": "champion_battle", "tierId": 3, "status": 2, "scheduledAtUtc": "...", "entryFeeCoins": 200, "maxParticipants": 100 }
]
```

**`kind` values:** `"champion_battle"`, `"survival"`, `"blitz"` (or custom strings)
**`status` enum:** `1 = Scheduled`, `2 = Open`, `3 = Live`, `4 = Closed`

#### `GET /game-events/{eventId}`
```json
// Response 200
{ "id": "<uuid>", "kind": "champion_battle", "status": 3, "scheduledAtUtc": "...", "participantCount": 87, "aliveCount": 42, "jackpotPool": 12400 }
```

#### `POST /game-events/enter`
```json
// Request
{ "eventId": "<client-uuid>", "gameEventId": "<game-event-uuid>", "playerId": "<uuid>" }

// Response 200
{ "eventId": "<client-uuid>", "status": "Entered" }
// status: "Entered" | "Duplicate" | "InsufficientFunds" | "NotFound" | "InvalidStatus"
// HTTP 503 returned with code "FEATURE_DISABLED" when game_events_enabled=false
```

#### `POST /game-events/revive`
```json
// Request
{ "eventId": "<client-uuid>", "gameEventId": "<uuid>", "playerId": "<uuid>" }

// Response 200
{ "eventId": "<client-uuid>", "status": "Revived", "revivesUsed": 1 }
```

---

### Guardians

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/guardians/{tierNumber}` | – | List active guardians for a tier in the current season |
| `POST` | `/guardians/challenge` | ✓ | Challenge a guardian (spawns a match) |

> `tierNumber` is a 1-based integer matching the tier system.

#### `GET /guardians/{tierNumber}`
```json
// Response 200 — array
[
  { "id": "<uuid>", "seasonId": "<uuid>", "tierNumber": 3, "playerId": "<uuid>", "assignedAtUtc": "...", "expiresAtUtc": "...", "defencesWon": 4, "defencesLost": 1 }
]
```

#### `POST /guardians/challenge`
```json
// Request
{
  "eventId": "<client-uuid>",
  "seasonId": "<uuid>",
  "tierNumber": 3,
  "challengerId": "<your-player-uuid>",
  "guardianId": "<guardian-player-uuid>"
}

// Response 200
{ "eventId": "<client-uuid>", "status": "Challenged", "matchId": "<uuid>" }
// status: "Challenged" | "AlreadyPending" | "NotFound"
// HTTP 503 returned with code "FEATURE_DISABLED" when guardian_enabled=false
```

Play the match via the MatchHub (`/ws/match`) then submit via `/mobile/matches/submit`. The backend auto-resolves the challenge outcome. A `GuardianChanged` push event fires if the challenger wins.

---

### Territory

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/territory/{seasonId}/{tierNumber}` | – | Full board — all tiles with ownership |
| `POST` | `/territory/duel` | ✓ | Challenge a tile (spawns a match) |
| `GET` | `/territory/multiplier/{seasonId}/{tierNumber}/{playerId}` | – | Player's total XP multiplier from owned tiles |

#### `GET /territory/{seasonId}/{tierNumber}`
```json
// Response 200
{
  "seasonId": "<uuid>", "tierNumber": 3,
  "tiles": [
    { "category": "science", "ownerId": "<uuid>", "xpMultiplierBps": 500 },
    { "category": "history", "ownerId": null,     "xpMultiplierBps": 300 }
  ]
}
```

`xpMultiplierBps` is basis points — `500` = **+5% XP** on questions in that category. `null` ownerId = unowned.

#### `POST /territory/duel`
```json
// Request
{
  "eventId": "<client-uuid>",
  "seasonId": "<uuid>",
  "tierNumber": 3,
  "category": "science",
  "challengerId": "<uuid>"
}

// Response 200 — tile challenged, match spawned
{ "matchId": "<uuid>", "tileOwnerId": "<uuid-or-null>" }

// Response 200 — challenger already owns this tile
{ "status": "AlreadyOwner", "matchId": "00000000-0000-0000-0000-000000000000", "tileOwnerId": "<uuid>" }

// HTTP 503 — territory_enabled=false
{ "code": "FEATURE_DISABLED", "message": "Territory feature is currently disabled." }
```

After the match, submit it normally. The backend resolves tile ownership automatically. A `TerritoryCapture` push event fires if the challenger wins.

---

### Event Stats & Leaderboards

These endpoints were added as part of the event tracking system and cover cross-mode standings.

| Method | Path | Auth | Description |
|---|---|---|---|
| `GET` | `/game-events/{eventId}/leaderboard` | – | Ranked participants in a closed event |
| `GET` | `/game-events/players/{playerId}/event-history` | ✓ | Player's full event participation history |
| `GET` | `/game-events/season-leaderboard` | – | Season-wide event champion standings |
| `GET` | `/territory/{seasonId}/{tierNumber}/dominance` | – | Top tile owners in a tier |

#### `GET /game-events/{eventId}/leaderboard?page=1&pageSize=50`
```json
// Response 200 — only available after event status = Closed (4)
[
  { "playerId": "<uuid>", "finalRank": 1, "awardedXp": 500, "awardedCoins": 250, "eliminatedAt": null },
  { "playerId": "<uuid>", "finalRank": 2, "awardedXp": 200, "awardedCoins": 100, "eliminatedAt": "2026-03-16T12:44:00Z" }
]
```

#### `GET /game-events/players/{playerId}/event-history?seasonId={uuid}&page=1&pageSize=20`
```json
// Response 200
[
  { "gameEventId": "<uuid>", "kind": "champion_battle", "finalRank": 1, "awardedXp": 500, "awardedCoins": 250, "enteredAt": "2026-03-15T10:00:00Z" }
]
```

#### `GET /game-events/season-leaderboard?seasonId={uuid}&sortBy=event_wins&page=1&pageSize=50`
```json
// Response 200
[
  { "playerId": "<uuid>", "eventsWon": 3, "eventsTop20": 7, "eventsEntered": 10, "guardianDefencesWon": 5, "guardianDaysTotal": 14, "currentTilesOwned": 4, "peakXpMultiplierBps": 1800 }
]
```

`sortBy` options: `event_wins` (default), `events_entered`, `guardian_defences`, `tiles_owned`

#### `GET /territory/{seasonId}/{tierNumber}/dominance?top=20`
```json
// Response 200
[
  { "playerId": "<uuid>", "tilesOwned": 5, "totalXpMultiplierBps": 2200 }
]
```

---

## 4. Real-Time (SignalR)

Use the [`signalr_netcore`](https://pub.dev/packages/signalr_netcore) package.

### Hub connections

| Hub | URL | Connect when |
|---|---|---|
| **NotificationHub** | `{baseUrl}/ws/notify?playerId={id}&access_token={token}` | At login — keep alive for session |
| **MatchHub** | `{baseUrl}/ws/match?playerId={id}&access_token={token}` | During an active match only |

### Flutter connection setup

```dart
import 'package:signalr_netcore/signalr_client.dart';

class NotificationHubService {
  late HubConnection _hub;

  Future<void> connect(String baseUrl, String playerId, String accessToken) async {
    _hub = HubConnectionBuilder()
        .withUrl('$baseUrl/ws/notify?playerId=$playerId&access_token=$accessToken')
        .withAutomaticReconnect()
        .build();

    _hub.on('GameEventElimination', _onElimination);
    _hub.on('GameEventClosed',      _onEventClosed);
    _hub.on('GuardianChanged',      _onGuardianChanged);
    _hub.on('TerritoryCapture',     _onTerritoryCapture);
    _hub.on('VoteCast',             _onVoteCast);
    _hub.on('MissionClaimed',       _onMissionClaimed);

    await _hub.start();
  }

  // --- Group subscriptions (call after connect) ---
  Future<void> watchGameEvent(String gameEventId) =>
      _hub.invoke('JoinGameEvent', args: [gameEventId]);

  Future<void> watchGuardians(String seasonId, int tierNumber) =>
      _hub.invoke('JoinGuardianWatch', args: [seasonId, tierNumber]);

  Future<void> watchTerritory(String seasonId, int tierNumber) =>
      _hub.invoke('JoinTerritory', args: [seasonId, tierNumber]);

  Future<void> watchVoteTopic(String topic) =>
      _hub.invoke('JoinTopic', args: [topic]);

  // --- Handlers ---
  void _onElimination(List<Object?>? args) { /* args[0] = map */ }
  void _onEventClosed(List<Object?>? args) { /* args[0] = map */ }
  void _onGuardianChanged(List<Object?>? args) { /* args[0] = map */ }
  void _onTerritoryCapture(List<Object?>? args) { /* args[0] = map */ }
  void _onVoteCast(List<Object?>? args) { /* args[0] = map */ }
  void _onMissionClaimed(List<Object?>? args) { /* args[0] = map */ }

  Future<void> disconnect() => _hub.stop();
}
```

### Server-push event payloads

#### `GameEventElimination`
```json
{ "gameEventId": "<uuid>", "playerId": "<uuid>", "survivorsRemaining": 41, "at": "2026-03-16T12:44:10Z" }
```
Fire when any player is eliminated. Always update your "alive count" UI when received.

#### `GameEventClosed`
```json
{ "gameEventId": "<uuid>", "kind": "champion_battle", "totalParticipants": 87, "jackpotDistributed": 12400 }
```
Fire when the event ends and prizes are distributed. Fetch `/game-events/{id}/leaderboard` after receiving this.

#### `GuardianChanged`
```json
{ "seasonId": "<uuid>", "tierNumber": 3, "newGuardianId": "<uuid>", "previousGuardianId": "<uuid-or-null>" }
```
Fire when a guardian is replaced. Refetch `/guardians/{tierNumber}` to refresh the UI.

#### `TerritoryCapture`
```json
{ "seasonId": "<uuid>", "tierNumber": 3, "category": "science", "newOwnerId": "<uuid>", "xpMultiplierBps": 500 }
```
Fire when any tile in a watched tier changes ownership. Update the local board state.

#### `VoteCast`
```json
{ "voteId": "<uuid>", "playerId": "<uuid>", "option": "A", "topic": "q-123", "castAtUtc": "2026-03-16T12:44:11Z" }
```

#### `MissionClaimed`
```json
{ "playerId": "<uuid>", "missionId": "<uuid>", "missionType": "win_streak", "missionKey": "win_5_in_a_row", "rewardXp": 500, "rewardCoins": 100, "rewardDiamonds": 0, "claimedAtUtc": "..." }
```

---

## 5. Feature Flows

### Game Event Flow

```
1. GET /game-events/upcoming
      → display list of Open (status=2) events

2. POST /game-events/enter
      → deducts entryFeeCoins from player's wallet

3. HubService.watchGameEvent(gameEventId)
      → subscribe to live eliminations

4. [Event goes Live — status=3]
   ← GameEventElimination pushed on each kill
      → update survivorsRemaining in UI
      → if own playerId matches: show eliminated / offer revive

5. [Optional] POST /game-events/revive
      → player returns to live pool

6. ← GameEventClosed pushed
      → navigate to results screen

7. GET /game-events/{id}/leaderboard
      → show final rankings + prizes

8. GET /game-events/season-leaderboard?sortBy=event_wins
      → show season standings
```

---

### Guardian Challenge Flow

```
1. GET /guardians/{tierNumber}
      → list who currently holds the guardian position

2. POST /guardians/challenge  (body: seasonId, tierNumber, challengerId, guardianId)
      → returns matchId

3. Connect MatchHub, play the trivia match

4. POST /mobile/matches/submit (include matchId)
      → awards XP/coins; backend resolves challenge outcome

5. ← GuardianChanged pushed to JoinGuardianWatch subscribers
      → if newGuardianId == your playerId: show promotion screen
      → else: show defender retained screen

6. GET /guardians/{tierNumber}  (refresh)

7. GET /game-events/season-leaderboard?sortBy=guardian_defences
      → show guardian prestige standings
```

---

### Territory Capture Flow

```
1. GET /territory/{seasonId}/{tierNumber}
      → render board — tiles with owner + XP multiplier

2. POST /territory/duel  (body: seasonId, tierNumber, category, challengerId)
      → returns matchId + tileOwnerId (null if unowned)

3. Play match via MatchHub

4. POST /mobile/matches/submit
      → backend resolves tile ownership

5. ← TerritoryCapture pushed to JoinTerritory subscribers
      → update local tile ownership in UI

6. GET /territory/multiplier/{seasonId}/{tierNumber}/{playerId}
      → display player's total XP boost

7. GET /territory/{seasonId}/{tierNumber}/dominance?top=10
      → show leaderboard of biggest territory owners
```

---

### Ranked Match Flow

```
1. POST /matchmaking/enqueue  (body: playerId, tierId, mode)
      → join queue; poll GET /matchmaking/status/{playerId}
        or wait for SignalR push from NotificationHub

2. [Match found] → connect to MatchHub
      hub.invoke('JoinMatch', [matchId])

3. hub.invoke('SubmitAnswer', [matchId, answerId]) per question
      ← answer broadcast to opponent via MatchHub

4. POST /mobile/matches/submit  (with status=1 Completed)
      → awards XP, coins, updates season rank points

5. GET /mobile/seasons/state/{playerId}
      → refresh tier + rank display
```

---

## 6. Event System Activation Controls

### Game Events

Individual game events are managed via **state machine** — they cannot be "paused" mid-state.

| Transition | How triggered |
|---|---|
| Scheduled → Open | `GameEventSchedulerJob` runs every minute; opens event at `OpenAtUtc` |
| Open → Live | Admin: `POST /admin/game-events/{id}/start` |
| Live → Closed | Admin: `POST /admin/game-events/{id}/close` OR auto-closes after 2 hours |

**To suspend all game events globally** (e.g. maintenance):
- Pause the `game-event-scheduler` Hangfire job via the Hangfire dashboard at `/hangfire`
- No in-app UI toggle currently exists — this is a backend-operator action only

From the Flutter client's perspective:
- If no events are in `Open` status, `/game-events/upcoming` returns an empty list
- Display a "No events available" state rather than an error

### Seasons

Seasonal features (leaderboards, tier ranking) are active only while a `Season` row has `status = 2 (Active)`.

- If `/mobile/seasons/active` returns `204 No Content`, no season is running
- All season-scoped queries (`/seasons/state/{id}`, season leaderboards) return empty/null gracefully
- The client should handle this by showing an "Off-season" state rather than hard-failing

Admin controls (not needed in Flutter client):
- `POST /admin/seasons/activate` — starts a season
- `POST /admin/seasons/close` — ends a season (distributes rewards, optionally starts next)

---

## 7. Error Handling

### HTTP status codes

| Code | Meaning | Action |
|---|---|---|
| `200` | Success | Parse response body |
| `204` | Success, no body | e.g. active season not found → show off-season state |
| `400` | Bad request | Show validation error from body |
| `401` | Unauthorised | Trigger token refresh; if fails, redirect to login |
| `403` | Forbidden | Player lacks permission for this action |
| `404` | Not found | Show "not found" UI |
| `409` | Conflict | Duplicate submission (check `status` field) |
| `429` | Rate limited | Back off and retry after `Retry-After` header |
| `500` | Server error | Show generic error, log to crash reporter |
| `503` | Feature disabled | Show "feature unavailable" message; check `code: "FEATURE_DISABLED"` in body |

### Domain status strings

Many endpoints return `{ "status": "<string>" }` instead of HTTP error codes. Map these in your service layer:

| Status string | Meaning |
|---|---|
| `"Applied"` / `"Entered"` / `"Redeemed"` etc. | Success variant |
| `"Duplicate"` | Idempotency — same `eventId` already processed. Treat as success. |
| `"InsufficientFunds"` | Not enough coins/gems/diamonds |
| `"NotFound"` | Target entity doesn't exist |
| `"InvalidStatus"` | Action not valid in current state (e.g. event not Open) |
| `"AlreadyPending"` | Guardian challenge already in progress |
| `"SelfRedeemNotAllowed"` | Player tried to redeem their own referral code |
| `"Cooldown"` | Powerup on cooldown |
| `"MissingPrereq"` | Skill prerequisite not met |
| `"AlreadyOwner"` | Player already owns this territory tile (no match spawned) |

> **Feature flags**: When an operator disables a feature (`game_events_enabled`, `guardian_enabled`, `territory_enabled`), the affected endpoints return HTTP **503** with body `{"code":"FEATURE_DISABLED","message":"..."}`. Show a friendly "feature unavailable" state rather than an error. The flag state is controlled by the backend; the Flutter client should not cache it.

### Recommended Dart error model

```dart
class ApiResult<T> {
  final T? data;
  final String? status;    // domain status string
  final int? httpCode;
  final String? message;

  bool get isSuccess => httpCode != null && httpCode! < 300;
  bool get isDuplicate => status == 'Duplicate';
  bool get isInsufficientFunds => status == 'InsufficientFunds';
}
```

---

## 8. Rate Limits

All limits are per-IP unless noted.

| Endpoint group | Limit |
|---|---|
| General API | 100 requests / minute |
| `POST /matches/submit` | 10 requests / 10 seconds (per user) |
| `POST /admin/auth/login` | 5 requests / minute |
| `POST /admin/auth/refresh` | 10 requests / minute |
| Admin notifications send | 20 requests / minute (per user) |

When a `429` is returned, respect the `Retry-After` response header before retrying.
