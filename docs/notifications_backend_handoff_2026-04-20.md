# Player Notifications Backend API Handoff

> **Audience:** Frontend + backend teams  
> **Date:** 2026-04-20  
> **Scope:** Player inbox + unread count + push refresh behavior

---

## Contract Summary

The frontend is now prepared to treat notifications as a backend-owned player inbox instead of local sample state.

Expected routes:

- `GET /notifications/inbox`
- `GET /notifications/unread-count`
- `POST /notifications/{notificationId}/read`
- `POST /notifications/read-all`
- `DELETE /notifications/{notificationId}`

Realtime behavior:

- keep using the existing player notification hub/websocket channel
- any new notification push should be sufficient to trigger inbox + unread-count refresh
- push does not replace inbox hydration; the inbox endpoints remain the source of truth

---

## DTO Shape

Frontend currently expects each inbox item to support:

```json
{
  "id": "notif-1",
  "type": "friend",
  "title": "Sarah sent you a friend request",
  "body": "You have 12 mutual friends",
  "createdAtUtc": "2026-04-20T12:00:00Z",
  "unread": true,
  "actionRoute": "/friends",
  "payload": {
    "friendId": "player-2"
  },
  "icon": "person_add",
  "avatarUrl": "https://example.test/avatar.png"
}
```

Accepted `type` values for the current app:

- `alert`
- `notification`
- `friend`
- `achievement`
- `system`
- `challenge`

The frontend also tolerates `category`, `kind`, `summary`, `message`, `createdAt`, and `route` aliases, but backend should standardize on the JSON shown above.

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

## Frontend Integration Notes

- notifications screen is now backed by the inbox endpoints, not local sample providers
- notification badges in the main app chrome now read from `GET /notifications/unread-count`
- opening a notification attempts `POST /notifications/{notificationId}/read`
- “Mark all read” uses `POST /notifications/read-all`
- swipe-to-dismiss and delete actions use `DELETE /notifications/{notificationId}`

The app still supports local UI filtering by notification type, but the backend inbox is the source of truth for content and unread state.

---

## Error Handling

Use the shared nested error envelope:

```json
{
  "error": {
    "code": "forbidden",
    "message": "You do not have access to this notification.",
    "details": {}
  }
}
```

Frontend already reuses the shared `ApiRequestException.message` parsing path used by premium store.
