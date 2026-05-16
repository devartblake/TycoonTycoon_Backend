# Player Notifications Backend API Handoff

> **Audience:** Frontend + backend teams  
> **Date:** 2026-04-20  
> **Scope:** Player inbox + unread count + lightweight realtime refresh

---

## Status

Player notifications v1 are now implemented and registered in the backend route surface.

Implemented routes:

- `GET /notifications/inbox`
- `GET /notifications/unread-count`
- `POST /notifications/{notificationId}/read`
- `POST /notifications/read-all`
- `DELETE /notifications/{notificationId}`

Validated on **April 20, 2026** with:

- `dotnet test Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-build --no-restore --filter "PlayerNotificationsEndpointsTests|MessagesEndpointsTests"`
- Result: `Passed (8/8)`

Current v1 notification sources:

- friend request received
- friend request accepted
- onboarding reward claimed as a simple system notification

This is a dedicated player inbox domain. It is separate from admin notification history and should be treated as the source of truth for player notifications.

---

## Backend Team Status Updates

> Future backend implementation notes for this handoff should be appended here.

### April 20, 2026

Current backend status:

- Player Notifications v1 is implemented and route-registered.
- New player inbox persistence has been added separately from admin notification history.
- Relational schema migration has been added:
  - `20260420231724_AddNotificationMessageing`
- Current backend notification sources are:
  - friend request received
  - friend request accepted
  - onboarding reward claimed as a simple system notification
- `/ws/notify` now supports lightweight `NotificationInboxUpdated` refresh events.
- Focused backend contract tests are passing for the notifications and messaging slice.
- EF pending-model check reports no pending model changes after the migration.

Frontend status interpreted from this handoff:

- Frontend has added `PlayerNotificationsService`.
- Frontend notification screen and unread badges are backend-backed.
- Mark-one-read, mark-all-read, and dismiss/delete actions call backend routes.
- Frontend currently treats the existing notification hub stream as a refresh signal.
- Frontend still needs Flutter test execution, widget/integration coverage, and real `/ws/notify` smoke validation.

Backend remaining work:

- Apply the EF migration in the target relational environment before deployment.
- Run the notifications endpoints against the target relational database after migration.
- Smoke test `/ws/notify` with a real authenticated client to confirm frontend event handling.
- Decide whether to add an admin/system endpoint for manually creating player system notifications beyond the onboarding reward source.
- Add additional notification sources only as follow-up work:
  - achievements
  - challenges
  - gameplay alerts
  - richer system notices
- Notification preferences, categories, bulk delete, and full push-payload sync remain v2 work.

Latest backend verification:

- `dotnet build Tycoon.Backend.Migrations\Tycoon.Backend.Migrations.csproj --no-restore -m:1`
- `dotnet build Tycoon.MigrationService\Tycoon.MigrationService.csproj --no-restore -m:1`
- `dotnet ef migrations has-pending-model-changes --project Tycoon.Backend.Migrations\Tycoon.Backend.Migrations.csproj --startup-project Tycoon.MigrationService\Tycoon.MigrationService.csproj --context AppDb --no-build`
- `dotnet test Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-build --no-restore --filter "PlayerNotificationsEndpointsTests|MessagesEndpointsTests"`

---

## Contract Summary

The frontend should treat notifications as a backend-owned inbox rather than local sample state.

### Read and mutation routes

- `GET /notifications/inbox`
- `GET /notifications/unread-count`
- `POST /notifications/{notificationId}/read`
- `POST /notifications/read-all`
- `DELETE /notifications/{notificationId}`

### Auth behavior

- bearer auth required for every route
- inbox is always implicitly "me"
- no `{playerId}` path is used for notifications v1

### Pagination behavior

`GET /notifications/inbox` supports optional query parameters:

- `page`
- `pageSize`

If omitted, the backend defaults to:

- `page = 1`
- `pageSize = 50`

---

## DTO Shape

Each inbox item is returned in this canonical shape:

```json
{
  "id": "6d0b0d2f-19f3-4d0a-a0a7-4f5c8e1931fd",
  "type": "friend",
  "title": "New friend request",
  "body": "Sarah sent you a friend request.",
  "createdAtUtc": "2026-04-20T12:00:00Z",
  "unread": true,
  "actionRoute": "/friends",
  "payload": {
    "requestId": "fd46c9eb-7cb8-4380-94c8-c30dffb8dbef",
    "fromPlayerId": "9e8f0e85-e1bb-42c5-9184-1b2685151f7d"
  },
  "icon": "person_add",
  "avatarUrl": "https://example.test/avatar.png"
}
```

Current important fields:

- `type`
  v1 currently emits `friend` and `system`
- `actionRoute`
  current values are route-like frontend navigation targets such as `/friends` and `/wallet`
- `payload`
  structured JSON object surfaced from JSON-backed backend storage
- `avatarUrl`
  optional and nullable

Paginated inbox response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "total": 0,
  "totalPages": 0
}
```

Unread count response:

```json
{
  "unreadCount": 3
}
```

---

## Realtime Refresh

The inbox endpoints remain the source of truth. Realtime is only a freshness signal in v1.

Current backend behavior:

- when a new player notification is created, the backend emits a refresh event over `/ws/notify`
- when notification read state changes, the backend emits a refresh event over `/ws/notify`

Current lightweight player event:

- `NotificationInboxUpdated`

Message payload shape:

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000",
  "unreadCount": 2,
  "reason": "created",
  "occurredAtUtc": "2026-04-20T12:00:00Z"
}
```

Frontend guidance:

- treat websocket events as a refresh signal only
- refetch `GET /notifications/inbox` and `GET /notifications/unread-count` after receiving the event
- do not treat the websocket event itself as a replacement for inbox hydration

---

## Frontend Integration Notes

- notifications screen should hydrate from `GET /notifications/inbox`
- notification badge counts should hydrate from `GET /notifications/unread-count`
- opening a notification should call `POST /notifications/{notificationId}/read`
- "mark all read" should call `POST /notifications/read-all`
- swipe-to-dismiss/delete should call `DELETE /notifications/{notificationId}`

The backend does not provide a separate archive endpoint in v1. Deletion is hard delete from the player inbox record.

---

## Error Handling

Notifications use the shared backend-standard nested error envelope:

```json
{
  "error": {
    "code": "FORBIDDEN",
    "message": "Notification does not belong to the authenticated user.",
    "details": {}
  }
}
```

Current common error codes:

- `UNAUTHORIZED`
- `FORBIDDEN`
- `NOT_FOUND`

Frontend should parse:

- `error.code`
- `error.message`
- `error.details`

---

## Current Limits

Notifications v1 intentionally does not yet include:

- achievements/challenges/gameplay alerts
- rich notification preferences
- notification categories endpoint
- bulk delete endpoint
- server-pushed full notification payload sync

Those can be layered on top of the current inbox model without changing the basic route family above.

---

## Frontend Implementation Status - April 20, 2026

### Completed

- Added `PlayerNotificationsService` for the v1 inbox routes:
  - `GET /notifications/inbox`
  - `GET /notifications/unread-count`
  - `POST /notifications/{notificationId}/read`
  - `POST /notifications/read-all`
  - `DELETE /notifications/{notificationId}`
- Extracted `InboxItem`, `InboxType`, and notification display config into a shared core model instead of defining them inside the screen.
- Replaced local sample notification data in `NotificationsScreen` with backend-backed Riverpod providers.
- Notification unread badges in the main menu/app bar now read from `GET /notifications/unread-count`.
- Mark-one-read, mark-all-read, and dismiss/delete UI actions now call backend mutation routes and invalidate inbox/unread providers.
- Existing notification hub stream is used as a lightweight refresh signal by invalidating inbox and unread-count providers.
- Added frontend service tests that verify endpoint paths, query parameters, DTO parsing, unread count parsing, mark-read, and dismiss calls.

### Remaining Frontend Work

- Run Flutter tests once `flutter`/`dart` are available on PATH.
- Validate against the real backend that `/ws/notify` emits the expected refresh event name and payload for inbox changes.
- Add widget/integration tests for notification screen loading, error, mark-read, mark-all-read, and dismiss flows.
- Decide whether notification detail primary actions should deep-link immediately from the list or remain detail-screen actions only.
- Wire additional notification sources as backend emits them, such as achievements, challenges, gameplay alerts, and richer system notices.
- Add notification preferences/categories UI only after the backend exposes those as a separate v2 contract.
