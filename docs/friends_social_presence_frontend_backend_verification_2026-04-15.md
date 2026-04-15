# Friends, Social, and Presence Frontend Backend Verification

**Date:** 2026-04-15  
**Audience:** Backend / Platform Team  
**Purpose:** Confirm that the Flutter frontend is now wired to the correct friends, social, and presence endpoints and aligned with the intended backend contracts.

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

Updated friend request DTO with `status: "Accepted"`.

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

Updated friend request DTO with `status: "Declined"`.

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

**Current request body sent by frontend**

```json
{
  "playerId": "current-user-guid",
  "friendId": "friend-guid",
  "targetUserId": "friend-guid",
  "friendPlayerId": "friend-guid"
}
```

**Why multiple field names are being sent**

The frontend intentionally sends `friendId`, `targetUserId`, and `friendPlayerId` together as a temporary compatibility shim while the alpha contract settles. Earlier discussions and legacy code referenced different parameter names.

**Backend confirmation requested**

Please confirm which single field name should remain long-term for the delete payload. The frontend can be tightened immediately once the canonical body shape is confirmed.

**Accepted frontend success conditions**

The current UI treats the request as successful when:

- the response body is empty, or
- `removed == true`, or
- `success == true`

If the backend intends a stricter success response, please confirm that as well.

---

### User search for add-by-username

**Frontend call**

`GET /users/search?handle=<username>`

**Used by**

- `BackendProfileSocialService.searchUsers()`
- `AddFriendByUsernameScreen`

**Current frontend parsing fallback**

The frontend accepts result arrays under any of these keys:

- `items`
- `users`
- `results`
- `data`

Each item is then expected to contain:

- `id` or `userId`
- `handle` or `username` or `userName`
- optionally `displayName` or `name`

**Backend confirmation requested**

This route was already present in the codebase and is still being used for add-by-username. Please confirm:

1. That `/users/search` is still the intended endpoint
2. The canonical query parameter name is `handle`
3. The canonical response envelope key for the result list
4. The canonical user ID and username field names

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

If the backend will only ever emit `online`, `inGame`, and `offline`, that is still compatible.

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

Please confirm the following so the frontend can remove temporary compatibility shims and finalize the integration:

1. Is `GET /users/search?handle=` the intended search route for add-by-username?
2. What is the canonical result envelope for `/users/search`?
3. For user search results, what are the canonical field names for:
   `id`, `displayName`, `username`/`handle`?
4. For unfriend, which exact request body should be supported long-term?
   Current frontend sends `playerId`, `friendId`, `targetUserId`, and `friendPlayerId`.
5. For `DELETE /friends`, should the success response be:
   `204 No Content`, empty JSON, or `{ "success": true }`?
6. For presence, should the backend ever emit `busy` or `away`, or should the frontend treat only `online`, `inGame`, and `offline` as the full enum?
7. For friend list and request endpoints, are the current DTO field names now considered stable for alpha?

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
