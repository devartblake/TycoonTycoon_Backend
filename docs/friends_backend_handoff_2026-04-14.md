# Friends / Presence Backend Handoff (2026-04-14)

## Purpose

This note summarizes:

1. What the frontend Friends screen implementation has already completed.
2. What backend capabilities appear to exist in planning/client code.
3. What backend contracts are still needed or need confirmation to complete the frontend implementation end-to-end.

---

## Executive Summary

The Friends screen is **mostly implemented on the frontend** from a UI and presence perspective:

- real Friends screen exists
- WebSocket-backed presence service exists
- online/offline/activity indicators are wired
- `DetailedPresenceCard` integration is wired
- current-user activity updates for quiz/match are wired

However, the screen is **not yet fully backendized for friend data**.

Right now, the main Friends screen still loads its friend list, pending requests, and suggestions from the local/mock `friendDiscoveryServiceProvider` path instead of authoritative backend endpoints.

### Current blocker

The frontend repo shows **partial evidence** of backend friend APIs, but not enough to confirm the full contract is live and ready:

- confirmed in frontend handoff/docs:
  - `GET /users/search?handle=`
  - `DELETE /friends`
- planned / referenced in client/docs:
  - `GET /users/{id}/friends`
  - `POST /users/{id}/friends/request`
  - `POST /users/{id}/friends/accept`
- not clearly confirmed in current alpha handoff:
  - pending friend requests list endpoint
  - outgoing/sent friend requests list endpoint
  - friend suggestions / mutual friends endpoint
  - backend-backed friend presence subscription semantics tied to friendship graph

---

## Frontend Work Completed So Far

### 1. Friends screen UI is implemented

Implemented file:

- `lib/screens/profile/friends_screen.dart`

Completed behavior:

- loads friends UI
- shows online friends section
- shows presence text such as online / in game
- supports friend detail bottom sheet
- includes contextual actions such as challenge / quiz / profile / remove

### 2. Presence widgets are implemented and integrated

Implemented file:

- `lib/ui_components/presence/presence_status_widget.dart`

Completed behavior:

- `PresenceStatusIndicator`
- `DetailedPresenceCard`
- animated pulse / status badge rendering

### 3. Rich presence service is implemented

Implemented file:

- `lib/core/services/presence/rich_presence_service.dart`

Completed behavior:

- singleton presence manager
- friend presence cache
- `subscribeToUsers(...)`
- `unsubscribeFromUsers(...)`
- `setGameActivity(...)`
- `getFormattedPresence(...)`
- `watchUserPresence(...)`

### 4. Presence WebSocket adapter is implemented

Implemented file:

- `lib/core/services/presence/presence_websocket_adapter.dart`

Completed behavior:

- listens to WebSocket envelopes
- handles:
  - `hello`
  - `presence.update`
  - `presence.bulk`
- sends:
  - `presence.subscribe`
  - `presence.unsubscribe`
  - `presence.update`

### 5. Presence service is initialized at app startup

Implemented file:

- `lib/core/bootstrap/app_init.dart`

Completed behavior:

- initializes `RichPresenceService().initialize(useWebSocket: true)`

### 6. Partial backend social/profile wiring already exists elsewhere

Implemented files:

- `lib/core/services/social/backend_profile_social_service.dart`
- `lib/game/providers/profile_providers.dart`
- `lib/core/networking/tycoon_api_client.dart`

Completed/known backend-backed behavior:

- backend user search by handle
- backend unfriend via `DELETE /friends`
- client methods exist for planned friend endpoints

---

## What Is Still Using Placeholder / Local Data

Current Friends screen data loading still uses:

- `friendDiscoveryServiceProvider`
- `lib/core/services/social/friend_discovery_service.dart`

Current local/mock usage in that service:

- `_loadMockData()`
- local in-memory friendships
- local pending request storage
- local friend suggestions

This means the following Friends screen areas are not yet authoritative against backend data:

- friend list
- incoming friend requests
- friend suggestions
- possibly sent-request reconciliation

---

## Backend Capability Status: What We Can Confirm vs What We Cannot

## Confirmed in frontend alpha handoff/docs

From `docs/frontend_backend_handoff_alpha_2026-04-04.md`:

- `GET /users/search?handle=`
- `GET /users/{userId}/career-summary`
- `GET /users/me/preferences/loadout`
- `PUT /users/me/preferences/loadout`
- `DELETE /friends`

That same handoff also states that:

- friend request create/accept still use local placeholder flows
- those flows can be backendized later if the contract enters alpha scope

### Conclusion

The repo does **not** currently prove that friend request create/accept/list flows are fully available in the backend alpha environment.

## Planned / referenced in client or roadmap docs

From `docs/synaptix_backend_plan.md` and `lib/core/networking/tycoon_api_client.dart`:

- `GET /users/{id}/friends`
- `POST /users/{id}/friends/request`
- `POST /users/{id}/friends/accept`

### Conclusion

These endpoints are clearly part of the intended design, but the current frontend handoff does not prove they are already implemented and deployed in the backend environment the app is using.

## Presence WebSocket contract

The frontend is already implemented against this protocol shape:

- `presence.subscribe`
- `presence.unsubscribe`
- `presence.update`
- `presence.bulk`

What still needs confirmation from backend:

- whether presence subscription is scoped to actual friendships
- whether backend sends bulk initial friend presence on connection/subscription
- whether presence updates include `gameActivity` payloads in the expected shape
- whether offline / disconnect transitions are emitted consistently

---

## Backend Deliverables Needed To Finish Frontend Implementation

The frontend team needs either:

1. confirmation that the endpoints below already exist and are stable, or
2. implementation of the missing ones with final request/response contracts.

## Required REST endpoints

### 1. Get current user's friends

Preferred:

- `GET /users/{userId}/friends`

Needed for:

- Friends tab authoritative list
- online/offline friend display
- friend action targeting

Expected response shape:

```json
{
  "items": [
    {
      "id": "user_123",
      "displayName": "Sarah Chen",
      "username": "sarah",
      "avatarUrl": "https://...",
      "isOnline": true,
      "lastSeenUtc": "2026-04-14T10:20:30Z"
    }
  ]
}
```

At minimum each friend item needs:

- `id`
- `displayName`
- `username` or handle
- `avatarUrl` nullable

### 2. Send friend request

Preferred:

- `POST /users/{userId}/friends/request`

Suggested request:

```json
{
  "targetUserId": "user_456"
}
```

Suggested response:

```json
{
  "requestId": "req_123",
  "status": "pending"
}
```

### 3. Accept friend request

Preferred:

- `POST /users/{userId}/friends/accept`

Suggested request:

```json
{
  "requestId": "req_123"
}
```

Suggested response:

```json
{
  "friendshipId": "friendship_123",
  "status": "accepted"
}
```

### 4. List incoming pending friend requests

This endpoint is currently needed and not clearly defined in frontend handoff docs.

Suggested endpoint:

- `GET /users/{userId}/friends/requests`

Suggested response:

```json
{
  "items": [
    {
      "requestId": "req_123",
      "senderId": "user_456",
      "senderDisplayName": "Mike Johnson",
      "senderUsername": "mikej",
      "senderAvatarUrl": "https://...",
      "createdAtUtc": "2026-04-14T10:20:30Z"
    }
  ]
}
```

### 5. Decline friend request

Needed for full request management in the Friends screen.

Suggested endpoint:

- `POST /users/{userId}/friends/decline`

Suggested request:

```json
{
  "requestId": "req_123"
}
```

### 6. List outgoing/sent friend requests

Needed if backend wants the frontend to prevent duplicate requests using server truth instead of local heuristics.

Suggested endpoint:

- `GET /users/{userId}/friends/requests/sent`

### 7. Friend suggestions or mutuals endpoint

The current UI has a suggestions surface. If suggestions are meant to remain in product scope, we need one of:

- `GET /users/{userId}/friends/suggestions`
- or a documented decision that suggestions should remain local-only / hidden for now

Suggested response item:

```json
{
  "id": "user_789",
  "displayName": "Emma Davis",
  "username": "emmad",
  "avatarUrl": "https://...",
  "mutualFriendCount": 3,
  "reason": "Mutual friends"
}
```

---

## Presence / WebSocket Contract Needed From Backend

The frontend is ready for real-time presence, but backend confirmation is needed for these payloads.

## Client-to-server

### Subscribe

```json
{
  "op": "presence.subscribe",
  "ts": 1710000000,
  "data": {
    "userIds": ["user_123", "user_456"]
  }
}
```

### Update my presence

```json
{
  "op": "presence.update",
  "ts": 1710000001,
  "data": {
    "status": "inGame",
    "activity": "Playing Quiz",
    "gameActivity": {
      "gameType": "quiz",
      "gameMode": "solo",
      "currentLevel": "Medium",
      "score": 1200,
      "gameState": "playing",
      "startTime": "2026-04-14T10:20:30Z",
      "metadata": {
        "category": "Science"
      }
    }
  }
}
```

## Server-to-client

### Single update

```json
{
  "op": "presence.update",
  "ts": 1710000002,
  "data": {
    "userId": "user_123",
    "status": "online",
    "activity": "In Match",
    "lastSeen": "2026-04-14T10:20:35Z",
    "gameActivity": {
      "gameType": "match",
      "gameMode": "pvp",
      "gameState": "lobby",
      "startTime": "2026-04-14T10:20:30Z",
      "metadata": {
        "opponentName": "Sarah"
      }
    }
  }
}
```

### Initial bulk snapshot

```json
{
  "op": "presence.bulk",
  "ts": 1710000003,
  "data": {
    "presences": [
      {
        "userId": "user_123",
        "status": "online",
        "lastSeen": "2026-04-14T10:20:35Z"
      }
    ]
  }
}
```

---

## Error Handling Needed From Backend

For the frontend to provide strong UX, the backend team should confirm stable error codes for:

- self-friend-request denied
- already friends
- duplicate pending request
- request not found
- unauthorized access to another user's friend requests
- blocked user

Preferred error envelope:

```json
{
  "error": {
    "code": "ALREADY_FRIENDS",
    "message": "Users are already friends."
  }
}
```

---

## Auth / Identity Assumptions To Confirm

Please confirm:

- whether friend endpoints require JWT auth
- whether `{userId}` must match the authenticated user
- whether `/users/me/friends...` aliases are available and preferred

If `/users/me/...` is available, frontend integration will be cleaner and less error-prone than passing an explicit user id everywhere.

---

## Recommended Backend Response To This Handoff

Please reply with one of these outcomes:

### Option A: Endpoints already live

Provide:

- final endpoint list
- request/response examples
- error codes
- any differences from the assumed shapes above

### Option B: Endpoints partially live

Provide:

- which of the following are implemented now:
  - friends list
  - send request
  - accept request
  - decline request
  - pending requests list
  - sent requests list
  - suggestions
- what remains to be implemented
- expected completion order

### Option C: Scope reduction for alpha

If alpha only intends to support:

- add-by-username search
- unfriend
- presence only for locally known friends

please confirm that explicitly so the frontend can reduce the Friends screen scope and remove placeholder UX rather than waiting on nonexistent APIs.

---

## Proposed Frontend Next Step Once Backend Confirms

After backend confirmation, frontend will:

1. replace `friendDiscoveryServiceProvider` in `FriendsScreen`
2. switch friend list loading to backend source of truth
3. switch pending request flows to backend
4. optionally switch suggestions to backend or hide them behind capability checks
5. keep WebSocket presence layer unchanged unless payload contract differs

---

## Reference Files

Frontend files already implemented:

- `lib/screens/profile/friends_screen.dart`
- `lib/core/services/presence/rich_presence_service.dart`
- `lib/core/services/presence/presence_websocket_adapter.dart`
- `lib/ui_components/presence/presence_status_widget.dart`
- `lib/core/bootstrap/app_init.dart`

Files showing current partial backend/social wiring:

- `lib/core/services/social/backend_profile_social_service.dart`
- `lib/core/networking/tycoon_api_client.dart`
- `docs/frontend_backend_handoff_alpha_2026-04-04.md`
- `docs/synaptix_backend_plan.md`

