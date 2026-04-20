# Direct Messaging Backend API Handoff

> **Audience:** Frontend + backend teams  
> **Date:** 2026-04-20  
> **Scope:** Direct-message core only

---

## Contract Summary

The frontend has been updated to treat direct messages as a backend-owned domain for:

- conversation list
- direct-conversation creation
- message history
- send message
- mark conversation read
- unread count via conversation summaries

Expected routes:

- `GET /messages/conversations`
- `POST /messages/conversations/direct`
- `GET /messages/conversations/{conversationId}/messages`
- `POST /messages/conversations/{conversationId}/messages`
- `POST /messages/conversations/{conversationId}/read`
- `GET /messages/unread-count`

Non-goals for this first pass:

- group chat
- attachments/uploads
- reactions
- typing/presence parity

---

## Conversation DTO

The frontend now accepts this conversation summary shape:

```json
{
  "id": "conv-1",
  "type": "direct",
  "participantIds": ["player-1", "player-2"],
  "displayTitle": "Sarah Chen",
  "avatarUrl": "https://example.test/avatar.png",
  "lastMessagePreview": "See you soon!",
  "lastMessageTimestamp": "2026-04-20T13:00:00Z",
  "unreadCount": 2,
  "createdAtUtc": "2026-04-20T10:00:00Z",
  "updatedAtUtc": "2026-04-20T13:00:00Z"
}
```

Paginated response envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 50,
  "total": 0,
  "totalPages": 0
}
```

`POST /messages/conversations/direct` request:

```json
{
  "targetPlayerId": "player-2"
}
```

---

## Message DTO

Thread history and send responses should use:

```json
{
  "id": "msg-1",
  "conversationId": "conv-1",
  "senderId": "player-2",
  "senderDisplayName": "Sarah Chen",
  "content": "Hey there",
  "type": "text",
  "status": "delivered",
  "createdAtUtc": "2026-04-20T13:05:00Z"
}
```

`POST /messages/conversations/{conversationId}/messages` request:

```json
{
  "content": "Hello!",
  "clientMessageId": "client-123"
}
```

`clientMessageId` is optional but recommended for idempotent retries.

Unread count response:

```json
{
  "unreadCount": 5
}
```

---

## Frontend Integration Notes

- messages list is no longer intended to be sourced from local sample storage for normal online flows
- the DM creation flows in friends and create-DM dialogs now expect backend conversation creation
- message detail hydration now expects backend message history
- mark-read state for a thread now expects `POST /messages/conversations/{conversationId}/read`

The current frontend still retains lightweight local typing state only as transitional UI behavior. It is not the message source of truth and should not be used as the backend contract reference.

---

## Error Handling

Use the shared nested error envelope:

```json
{
  "error": {
    "code": "forbidden",
    "message": "You are not a participant in this conversation.",
    "details": {}
  }
}
```

Frontend already consumes this through the shared API layer, consistent with premium-store handling.
