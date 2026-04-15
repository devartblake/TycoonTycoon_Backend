# Friends, Social, and Presence Frontend Backend Verification

**Date:** 2026-04-15  
**Audience:** Backend / Platform Team  
**Purpose:** Confirm that the Flutter frontend is now wired to the correct friends, social, and presence endpoints and aligned with the intended backend contracts.

**Verification status:** Cross-checked against the backend route mappings and DTOs in:

- `Tycoon.Backend.Api/Features/Users/UserFriendsEndpoints.cs`
- `Tycoon.Backend.Api/Features/Friends/FriendsEndpoints.cs`
- `Tycoon.Backend.Api/Features/Users/UsersEndpoints.cs`
- `Tycoon.Shared.Contracts/Dtos/SocialDtos.cs`
- `Tycoon.Backend.Api/Program.cs`

---

## Summary

The frontend has been migrated off the old mock-driven friends flow for the primary friends surfaces. The app now uses typed DTOs, Riverpod providers, backend-backed friends/request/suggestions loading, and WebSocket presence wiring with the required `playerId` query parameter.

This document reflects the code currently in the frontend, not the earlier migration plan. Please use this to verify endpoint paths, payloads, response shapes, and any remaining contract gaps.

---

## Frontend Changes Completed

The following pieces are now backend-connected:

- Friends roster loading in `FriendsScreen`
- Incoming friend requests loading in `FriendsScreen`
- Suggested friends loading in `FriendsScreen`
- Send friend request from `FriendsScreen`
- Accept/decline friend request from `FriendsScreen`
- Unfriend from `FriendsScreen`
- Add-by-username flow in `AddFriendByUsernameScreen`
- Incoming-request handling in `AddFriendDialog`
- DM recipient picker in `CreateDMDialog`
- Presence WebSocket initialization via `/ws?playerId=<guid>`
- Presence subscribe/update/unsubscribe messages via the raw WebSocket client

Primary frontend files involved:

- [backend_profile_social_service.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/services/social/backend_profile_social_service.dart)
- [friends_providers.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/providers/friends_providers.dart)
- [friends_screen.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/profile/friends_screen.dart)
- [add_friend_dialog.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/profile/dialogs/add_friend_dialog.dart)
- [add_friends_screen.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/profile/enhanced/add_friends_screen.dart)
- [create_dm_dialog.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/screens/messages/dialogs/create_dm_dialog.dart)
- [app_init.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/bootstrap/app_init.dart)
- [core_providers.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/game/providers/core_providers.dart)
- [presence_websocket_adapter.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/services/presence/presence_websocket_adapter.dart)
- [rich_presence_service.dart](/C:/Users/lmxbl/StudioProjects/trivia_tycoon/lib/core/services/presence/rich_presence_service.dart)

---

## REST Endpoints Currently Used By Frontend

### Friends list

**Frontend call**

`GET /users/me/friends?page=1&pageSize=50`

**Used by**

- `BackendProfileSocialService.getFriends()`
- `friendsListProvider`
- `FriendsScreen`
- `CreateDMDialog`
- `AddFriendByUsernameScreen`

**Expected response envelope**

```json
{
  "page": 1,
  "pageSize": 50,
  "total": 3,
  "totalPages": 1,
  "items": [
    {
      "friendPlayerId": "guid",
      "displayName": "sarah_chen",
      "username": "sarah_chen",
      "avatarUrl": null,
      "isOnline": true,
      "lastSeenUtc": null,
      "sinceUtc": "2026-04-10T08:00:00Z"
    }
  ]
}
```

**Frontend field expectations**

- `friendPlayerId`: required string
- `displayName`: preferred display label
- `username`: required handle fallback
- `avatarUrl`: nullable
- `isOnline`: boolean
- `lastSeenUtc`: nullable ISO timestamp
- `sinceUtc`: nullable ISO timestamp

---

### Incoming friend requests

**Frontend call**

`GET /users/me/friends/requests?page=1&pageSize=50`

**Used by**

- `BackendProfileSocialService.getIncomingFriendRequests()`
- `incomingFriendRequestsProvider`
- `FriendsScreen`
- `AddFriendDialog`
- `AddFriendByUsernameScreen`

**Expected response envelope**

The backend now returns `page`, `pageSize`, `total`, `totalPages`, and `items`.

```json
{
  "page": 1,
  "pageSize": 50,
  "total": 2,
  "totalPages": 1,
  "items": [
    {
      "requestId": "guid",
      "fromPlayerId": "guid",
      "toPlayerId": "guid",
      "status": "Pending",
      "createdAtUtc": "2026-04-15T09:30:00Z",
      "respondedAtUtc": null,
      "senderDisplayName": "mike_johnson",
      "senderUsername": "mike_johnson",
      "senderAvatarUrl": null
    }
  ]
}
```

**Frontend field expectations**

- `requestId`
- `fromPlayerId`
- `toPlayerId`
- `status`
- `createdAtUtc`
- `respondedAtUtc`
- `senderDisplayName`
- `senderUsername`
- `senderAvatarUrl`

---

### Sent / outgoing friend requests

**Frontend call**

`GET /users/me/friends/requests/sent?page=1&pageSize=50`

**Used by**

- `BackendProfileSocialService.getSentFriendRequests()`
- `sentFriendRequestsProvider`
- `AddFriendByUsernameScreen`

**Expected response envelope**

The backend now returns `page`, `pageSize`, `total`, `totalPages`, and `items`.

Same envelope and item shape as incoming requests.

The frontend currently checks:

- `toPlayerId`
- `status`

for deduping pending outbound requests during add-by-username flow.

---

### Send friend request

**Frontend call**

`POST /users/me/friends/request`

**Request body**

```json
{
  "targetUserId": "guid"
}
```

**Used by**

- `BackendProfileSocialService.sendFriendRequest()`
- `FriendsScreen`
- `AddFriendByUsernameScreen`

**Expected response**

The frontend expects the full request DTO:

```json
{
  "requestId": "guid",
  "fromPlayerId": "guid",
  "toPlayerId": "guid",
  "status": "Pending",
  "createdAtUtc": "2026-04-15T10:00:00Z",
  "respondedAtUtc": null
}
```

The frontend also supports idempotent responses where `status` may already be `"Accepted"`.

---

### Accept friend request

**Frontend call**

`POST /users/me/friends/requests/{requestId}/accept`

**Request body**

```json
{}
```

**Used by**

- `BackendProfileSocialService.acceptFriendRequest()`
- `FriendsScreen`
- `AddFriendDialog`

**Expected response**

Updated base friend request DTO with `status: "Accepted"`.

---

### Decline friend request

**Frontend call**

`POST /users/me/friends/requests/{requestId}/decline`

**Request body**

```json
{}
```

**Used by**

- `BackendProfileSocialService.declineFriendRequest()`
- `FriendsScreen`
- `AddFriendDialog`

**Expected response**

Updated base friend request DTO with `status: "Declined"`.

---

### Friend suggestions

**Frontend call**

`GET /users/me/friends/suggestions`

**Used by**

- `BackendProfileSocialService.getFriendSuggestions()`
- `friendSuggestionsProvider`
- `FriendsScreen`

**Expected response**

Bare JSON list:

```json
[
  {
    "id": "guid",
    "displayName": "emma_davis",
    "username": "emma_davis",
    "avatarUrl": null,
    "mutualFriendCount": 0,
    "reason": "New to Synaptix"
  }
]
```

**Frontend field expectations**

- `id`
- `displayName`
- `username`
- `avatarUrl`
- `mutualFriendCount`
- `reason`

---

### Unfriend

**Frontend call**

`DELETE /friends`

**Used by**

- `BackendProfileSocialService.removeFriend()`
- `FriendsScreen`

**Backend request body currently supported**

```json
{
  "playerId": "current-user-guid",
  "friendPlayerId": "friend-guid"
}
```

The backend canonical contract is `playerId` plus `friendPlayerId`.

For compatibility during the frontend transition, the backend also accepts:

- `friendId`
- `targetUserId`

**Accepted frontend success conditions**

The backend currently returns:

- `204 No Content` on success

No `{ "removed": true }` or `{ "success": true }` response is currently implemented on this route.

---

### User search for add-by-username

**Frontend call**

`GET /users/search?handle=<username>`

**Used by**

- `BackendProfileSocialService.searchUsers()`
- `AddFriendByUsernameScreen`

**Backend response currently returned**

The backend now returns a paged envelope:

```json
{
  "page": 1,
  "pageSize": 20,
  "total": 1,
  "totalPages": 1,
  "items": [
    {
      "id": "guid",
      "handle": "sarah_chen",
      "displayName": "sarah_chen",
      "username": "sarah_chen",
      "country": "US",
      "tier": "Bronze",
      "mmr": 1200
    }
  ]
}
```

Canonical backend details today:

1. `/users/search` is implemented and requires authorization
2. The canonical query parameter name is `handle`
3. The canonical result envelope key is `items`
4. The canonical field names are `id`, `handle`, `displayName`, and `username`

---

## Presence WebSocket Contract Currently Used By Frontend

### Connection URL

The frontend now explicitly appends the player ID to the raw WebSocket endpoint:

`ws://<host>/ws?playerId=<current-player-guid>`

This is done in two places:

- `AppInit.initializeWebSocket()`
- `wsClientProvider`

This matches the backend guidance that JWT identity extraction is not yet implemented for `/ws` and that `playerId` must be passed explicitly.

---

### Frontend WebSocket message ops handled

The frontend presence adapter currently handles:

- `hello`
- `presence.bulk`
- `presence.update`

### Frontend messages sent

The frontend sends:

- `presence.subscribe`
- `presence.unsubscribe`
- `presence.update`

### Presence subscribe payload

```json
{
  "op": "presence.subscribe",
  "ts": 1744751234569,
  "data": {
    "userIds": ["guid-1", "guid-2"]
  }
}
```

### Presence update payload

```json
{
  "op": "presence.update",
  "ts": 1744751234571,
  "data": {
    "status": "inGame",
    "activity": "Playing quiz",
    "gameActivity": {
      "gameType": "quiz",
      "gameMode": "solo",
      "currentLevel": "Medium",
      "score": 1200,
      "gameState": "playing",
      "startTime": "2026-04-15T10:00:00Z",
      "metadata": {
        "category": "Science"
      }
    }
  }
}
```

### Presence update payload parsed from server

The frontend expects:

```json
{
  "op": "presence.update",
  "ts": 1744751234573,
  "data": {
    "userId": "guid",
    "status": "online",
    "activity": "In Match",
    "gameActivity": {
      "gameType": "match",
      "gameMode": "pvp",
      "gameState": "lobby",
      "startTime": "2026-04-15T10:00:00Z",
      "metadata": {
        "opponentName": "Sarah"
      }
    },
    "lastSeen": "2026-04-15T10:00:35Z"
  }
}
```

### Presence status parsing

The frontend parser currently accepts:

- `online`
- `away`
- `busy`
- `inGame`
- `in_game`
- `offline`

The core online UI treats these statuses as online-like:

- `online`
- `inGame`
- `busy`

The backend explicitly emits `online` and `offline` on connect/disconnect, and otherwise relays whatever `status` the client sent in `presence.update`. There is no backend-only enum enforcement here today.

The raw `presence.subscribe` handler is restricted to the current player and their friends, so snapshot reads now align with the friendship graph rather than arbitrary requested user IDs.

---

## Timeout and Local Development Behavior

Friends/social REST calls now use a frontend timeout of:

`Duration(seconds: 10)`

This was increased from the app’s shorter default behavior because `/users/me/friends` was timing out in local Docker development and surfacing as:

`ApiRequestException [/users/me/friends]: API Timeout`

Backend note:

- If local Docker or emulator routing still causes latency spikes beyond 10 seconds, we may need to revisit local dev performance or container networking.
- This timeout increase was only applied to the social service methods, not globally to every API call.

---

## UI Surfaces Now Depending On Backend Contract

### Friends screen

The main friends screen now depends on:

- `/users/me/friends`
- `/users/me/friends/requests`
- `/users/me/friends/suggestions`
- `/users/me/friends/request`
- `/users/me/friends/requests/{id}/accept`
- `/users/me/friends/requests/{id}/decline`
- `DELETE /friends`
- `/ws?playerId=<guid>`

### Add friend by username

The username-based add flow now depends on:

- `/users/search?handle=...`
- `/users/me/friends`
- `/users/me/friends/requests`
- `/users/me/friends/requests/sent`
- `/users/me/friends/request`

### Add friend dialog

The incoming-requests dialog now depends on:

- `/users/me/friends/requests`
- `/users/me/friends/requests/{id}/accept`
- `/users/me/friends/requests/{id}/decline`

### Create DM dialog

The DM recipient picker now depends on:

- `/users/me/friends`

This means messaging recipient selection is now coupled to the live backend friends roster rather than mock discovery data.

---

## Contract Questions For Backend Team

Confirmed from current backend implementation:

1. `GET /users/search?handle=` is implemented and authorized
2. `/users/search` returns a paged envelope with `items`
3. User search result field names are `id`, `handle`, `displayName`, and `username`
4. `DELETE /friends` canonically supports `playerId` and `friendPlayerId`, and also tolerates `friendId` and `targetUserId` as compatibility aliases
5. `DELETE /friends` currently returns `204 No Content`
6. Presence always emits `online` and `offline` from server-side lifecycle events; all other statuses are pass-through from client updates
7. Friend list and friend-request DTO responses now include `totalPages`

---

## Frontend Assumptions Still In Place

- `avatarUrl` may be `null`; the app renders generated initials avatars.
- `lastSeenUtc` may be `null`; the app primarily relies on `isOnline` and presence socket updates.
- Suggestions may return `mutualFriendCount = 0`; the UI does not require real mutual counts.
- Presence identity still depends on `playerId` in the WebSocket query string.
- Messages recipient selection is limited to the current friend roster returned by `/users/me/friends`.

---

## Recommended Backend Reply Format

To close the loop quickly, a backend response that covers the following would be enough:

1. Confirmed endpoint table
2. Confirmed request body for unfriend
3. Confirmed `/users/search` contract
4. Confirmed stable DTO field names
5. Any known differences between local Docker routing and deployed routing for `/users/me/friends` or `/ws`

Once those are confirmed, the frontend can remove the remaining defensive parsing and compatibility fields.
