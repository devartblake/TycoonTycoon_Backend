# Synaptix Frontend Integration Guide

**Date:** 2026-04-01
**Audience:** Frontend (Flutter) development team
**Backend branch:** `claude/check-synaptix-docs-fw3NG`
**Purpose:** Complete reference for integrating the Flutter app with the Synaptix backend. Covers every available API, real-time system, data contract, and known gaps.

---

## Table of Contents

1. [Platform Overview](#1-platform-overview)
2. [Authentication](#2-authentication)
3. [Player Preferences (Synaptix Mode System)](#3-player-preferences)
4. [Questions & Gameplay](#4-questions--gameplay)
5. [Matches & Matchmaking](#5-matches--matchmaking)
6. [Economy & Wallet](#6-economy--wallet)
7. [Store / Shop](#7-store--shop)
8. [Missions](#8-missions)
9. [Leaderboards (Arena)](#9-leaderboards-arena)
10. [Seasons & Tiers](#10-seasons--tiers)
11. [Skills / Pathways](#11-skills--pathways)
12. [Powerups (Enhancements)](#12-powerups-enhancements)
13. [Friends & Social (Circles)](#13-friends--social-circles)
14. [Party System](#14-party-system)
15. [Game Events (Jackpot / Crown)](#15-game-events)
16. [Guardians & Territory](#16-guardians--territory)
17. [Referrals & QR](#17-referrals--qr)
18. [Votes / Polls](#18-votes--polls)
19. [Analytics](#19-analytics)
20. [Real-Time (SignalR)](#20-real-time-signalr)
21. [Terminology Mapping](#21-terminology-mapping)
22. [Known Gaps & Remaining Backend Work](#22-known-gaps--remaining-backend-work)
23. [Migration Notes](#23-migration-notes)

---

## 1. Platform Overview

The Synaptix backend is a .NET 9 API with PostgreSQL (source of truth), MongoDB (analytics), Elasticsearch (metrics), and a Python FastAPI sidecar (ML/utilities). Real-time communication uses SignalR (WebSocket).

**Base URL:** Configured per environment (dev/staging/prod)
**Auth:** JWT Bearer tokens via `/auth/login` or `/auth/signup`
**Content-Type:** `application/json` for all requests

### Currency System

| Internal Name | Display Name | CurrencyType Enum |
|---|---|---|
| `Xp` | Neural XP | `1` |
| `Coins` | Credits | `2` |
| `Diamonds` | Synapse Shards | `3` |

---

## 2. Authentication

### Register + Auto-Login (Recommended for Flutter)

```
POST /auth/signup
```
```json
{
  "email": "player@example.com",
  "password": "securepass123",
  "deviceId": "flutter-device-uuid",
  "handle": "PlayerName",
  "country": "US"
}
```
**Response:** `SignupResponse`
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc123...",
  "expiresIn": 3600,
  "userId": "guid-string",
  "user": {
    "id": "guid",
    "handle": "PlayerName",
    "email": "player@example.com",
    "country": "US",
    "tier": null,
    "mmr": 0
  }
}
```

### Standard Login

```
POST /auth/login
```
```json
{
  "email": "player@example.com",
  "password": "securepass123",
  "deviceId": "flutter-device-uuid"
}
```
**Response:** `LoginResponse` (same shape as signup minus `userId` string field)

### Token Refresh

```
POST /auth/refresh
```
```json
{ "refreshToken": "abc123..." }
```

### Logout

```
POST /auth/logout   [Requires Authorization]
```
```json
{ "deviceId": "flutter-device-uuid" }
```

### Update Profile

```
PATCH /users/me   [Requires Authorization]
```
```json
{ "handle": "NewName", "country": "GB" }
```
Both fields are optional — only provided fields are updated.

---

## 3. Player Preferences

These endpoints persist the Synaptix mode/theme system preferences. Created on first PUT; returns sensible defaults on GET if none set.

### Get Preferences

```
GET /users/me/preferences   [Requires Authorization]
```
**Response:**
```json
{
  "synaptixMode": "adult",
  "preferredSurface": "hub",
  "reducedMotion": false,
  "tonePreference": "balanced"
}
```

### Update Preferences

```
PUT /users/me/preferences   [Requires Authorization]
```
All fields are optional — only provided fields are updated.
```json
{
  "synaptixMode": "teen",
  "preferredSurface": "arena",
  "reducedMotion": false,
  "tonePreference": "competitive"
}
```

**Allowed values:**

| Field | Values |
|---|---|
| `synaptixMode` | `kids`, `teen`, `adult` |
| `preferredSurface` | `hub`, `arena`, `labs`, `pathways`, `journey`, `circles`, `command` |
| `reducedMotion` | `true`, `false` |
| `tonePreference` | `playful`, `balanced`, `competitive` |

---

## 4. Questions & Gameplay

### Fetch Questions for a Match/Session

Serves random questions **without correct answers** (anti-cheat by design).

```
GET /questions/set?category=Science&difficulty=2&count=10
```

| Param | Type | Required | Notes |
|---|---|---|---|
| `category` | string | No | Filter by category (e.g., "Science", "General") |
| `difficulty` | int | No | `1`=Easy, `2`=Medium, `3`=Hard, `4`=Expert |
| `count` | int | No | 1–50, default 10 |

**Response:** `QuestionSetDto`
```json
{
  "questions": [
    {
      "id": "guid",
      "text": "What is the speed of light?",
      "category": "Science",
      "difficulty": 2,
      "options": [
        { "id": "A", "text": "300,000 km/s" },
        { "id": "B", "text": "150,000 km/s" },
        { "id": "C", "text": "500,000 km/s" },
        { "id": "D", "text": "1,000,000 km/s" }
      ],
      "mediaKey": null
    }
  ],
  "count": 10
}
```

### Check Single Answer

```
POST /questions/check
```
```json
{
  "questionId": "guid",
  "selectedOptionId": "A"
}
```
**Response:**
```json
{
  "questionId": "guid",
  "selectedOptionId": "A",
  "correctOptionId": "A",
  "isCorrect": true
}
```

### Batch Check Answers (Full Round)

```
POST /questions/check-batch
```
```json
{
  "answers": [
    { "questionId": "guid-1", "selectedOptionId": "A" },
    { "questionId": "guid-2", "selectedOptionId": "C" }
  ]
}
```
**Response:**
```json
{
  "results": [
    { "questionId": "guid-1", "selectedOptionId": "A", "correctOptionId": "A", "isCorrect": true },
    { "questionId": "guid-2", "selectedOptionId": "C", "correctOptionId": "B", "isCorrect": false }
  ],
  "total": 2,
  "correct": 1
}
```

---

## 5. Matches & Matchmaking

### Start a Match

```
POST /matches/start
```
```json
{ "hostPlayerId": "guid", "mode": "ranked" }
```
Enforces moderation checks — banned players are rejected.

### Submit Match Results

```
POST /matches/submit   [Rate Limited]
```
```json
{
  "eventId": "guid (idempotency key)",
  "matchId": "guid",
  "mode": "ranked",
  "category": "general",
  "questionCount": 10,
  "startedAtUtc": "2026-04-01T12:00:00Z",
  "endedAtUtc": "2026-04-01T12:05:00Z",
  "status": 1,
  "participants": [
    { "playerId": "guid", "score": 800, "correct": 8, "wrong": 2, "avgAnswerTimeMs": 3200.5 }
  ]
}
```
**MatchStatus:** `1` = Completed, `2` = Aborted
**Response** includes XP/Coin awards per participant.

### Get Match Detail

```
GET /matches/{matchId}
```

### Matchmaking Queue

```
POST /matchmaking/enqueue   [Rate Limited]
```
```json
{ "playerId": "guid", "mode": "ranked", "tier": 3 }
```

```
POST /matchmaking/cancel
```
Body: `"guid"` (playerId)

```
GET /matchmaking/status/{playerId}
```

Match found notifications arrive via **SignalR MatchHub**.

---

## 6. Economy & Wallet

The backend is **authoritative** for wallet state. The wallet entity holds XP, Coins, and Diamonds with delta-based mutations.

### Get Economy State (Config + Rules)

```
GET /mobile/economy/state
```
Returns balance config (energy costs, mode rules, safeguards).

### Start Session (Energy Discount for New Players)

```
POST /mobile/economy/session/start?playerId={guid}
```

### Daily Free Jackpot Ticket

```
POST /mobile/economy/daily-jackpot-ticket/claim?playerId={guid}
```

### Revive Cost Quote

```
POST /mobile/economy/revive/quote?playerId={guid}&almostWin=true
```

### Pity System (Loss Streak)

```
POST /mobile/economy/pity/report-loss?playerId={guid}
POST /mobile/economy/pity/report-win?playerId={guid}
```

---

## 7. Store / Shop

### Browse Catalog

```
GET /store/catalog?itemType=powerup
```
**Response:** `StoreCatalogDto`
```json
{
  "items": [
    {
      "id": "guid",
      "sku": "powerup:skip",
      "name": "Skip Question",
      "description": "Skip one question without penalty",
      "itemType": "powerup",
      "priceCoins": 100,
      "priceDiamonds": 5,
      "grantQuantity": 1,
      "maxPerPlayer": 0,
      "mediaKey": null,
      "sortOrder": 1
    }
  ],
  "count": 1
}
```

### Get Single Item

```
GET /store/catalog/{sku}
```

### Purchase Item

```
POST /store/purchase   [Requires Authorization]
```
```json
{
  "playerId": "guid",
  "sku": "powerup:skip",
  "quantity": 3,
  "currency": "coins"
}
```
**Currency:** `"coins"` (Credits) or `"diamonds"` (Synapse Shards)

**Response:** `StorePurchaseResultDto`
```json
{
  "status": "Applied",
  "transactionId": "guid",
  "balanceXp": 1200,
  "balanceCoins": 750,
  "balanceDiamonds": 42,
  "errorMessage": null
}
```
**Possible statuses:** `Applied`, `Duplicate`, `InsufficientFunds`, `Failed`

Purchases are **idempotent** (via internal EventId), **atomic** (wallet deduction + item grant in one transaction), and **reversible** (via admin rollback).

---

## 8. Missions

### List Missions

```
GET /missions/?type=daily
```

### Apply Progress

```
POST /missions/progress/match-completed
POST /missions/progress/round-completed
```

### Claim Reward

```
POST /missions/{missionId}/claim?playerId={guid}&type=daily
```
Triggers SignalR broadcast: `MissionClaimed` with reward details.

---

## 9. Leaderboards (Arena)

### My Tier Rank

```
GET /leaderboards/me/{playerId}
```
**Response:** `MyTierDto` — playerId, tierId, tierRank, globalRank, score, xpProgress

### Tier Leaderboard (Paginated)

```
GET /leaderboards/tiers/{tierId}?page=1&pageSize=25
```

### Ranked Leaderboard (Season-Filtered)

```
GET /leaderboards/ranked?seasonId={guid}&scope=global&tier=3&page=1&pageSize=25&sort=points
```

---

## 10. Seasons & Tiers

### Active Season

```
GET /seasons/active
```
**Response:** `SeasonDto` — seasonId, number, name, status, start/end dates

### Player Season State

```
GET /seasons/state/{playerId}
```
**Response:** `PlayerSeasonStateDto` — rankPoints, wins, losses, draws, matchesPlayed, tier, tierRank, seasonRank

---

## 11. Skills / Pathways

### Skill Catalog

```
GET /skills/tree
```
**Response:** `SkillTreeCatalogDto` — all nodes with branch, tier, title, prerequisites, costs, effects

**Branches:** `Knowledge (1)`, `Strategy (2)`, `Powerups (3)`

### Player Skill State

```
GET /skills/state/{playerId}
```
Returns list of unlocked node keys.

### Unlock Skill

```
POST /skills/unlock
```
```json
{ "eventId": "guid", "playerId": "guid", "nodeKey": "recall-mastery" }
```
**Statuses:** `Unlocked`, `Duplicate`, `MissingPrereq`, `NotFound`, `InsufficientFunds`

### Respec (Full Reset)

```
POST /skills/respec
```
```json
{ "eventId": "guid", "playerId": "guid", "refundPercent": 80 }
```
Returns refunded Coins/Diamonds amounts.

---

## 12. Powerups (Enhancements)

**Types:** `FiftyFifty (1)`, `Skip (2)`, `DoublePoints (3)`, `ExtraTime (4)`

### Inventory State

```
GET /powerups/state/{playerId}
```

### Use Powerup

```
POST /powerups/use
```
```json
{ "eventId": "guid", "playerId": "guid", "type": 2 }
```
**Statuses:** `Used`, `Duplicate`, `Insufficient`, `Cooldown`

---

## 13. Friends & Social (Circles)

### Send Friend Request

```
POST /friends/request
```
```json
{ "fromPlayerId": "guid", "toPlayerId": "guid" }
```

### Accept / Decline

```
POST /friends/request/{requestId}/accept
POST /friends/request/{requestId}/decline
```
Body: `{ "playerId": "guid" }`

### List Friends

```
GET /friends?playerId={guid}&page=1&pageSize=25
```

### List Friend Requests

```
GET /friends/requests?playerId={guid}&box=incoming&page=1&pageSize=25
```
**Box:** `incoming`, `outgoing`, `all`

---

## 14. Party System

### Create Party

```
POST /party
```
```json
{ "leaderPlayerId": "guid" }
```

### Get Party Roster

```
GET /party/{partyId}
```

### Invite / Accept / Decline / Leave

```
POST /party/{partyId}/invite        — { "fromPlayerId": "guid", "toPlayerId": "guid" }
POST /party/invites/{inviteId}/accept   — { "playerId": "guid" }
POST /party/invites/{inviteId}/decline  — { "playerId": "guid" }
POST /party/{partyId}/leave             — { "playerId": "guid" }
```

### List Invites

```
GET /party/invites?playerId={guid}&box=incoming&page=1&pageSize=25
```

### Party Matchmaking

```
POST /party/{partyId}/enqueue  — { "leaderPlayerId": "guid", "mode": "ranked", "tier": 3 }
POST /party/{partyId}/queue/cancel — { "leaderPlayerId": "guid" }
```

---

## 15. Game Events

### List Upcoming

```
GET /game-events/upcoming?tierId=3
```

### Get Event Status

```
GET /game-events/{gameEventId}
```

### Enter Event

```
POST /game-events/enter   [Requires Authorization]
```
```json
{ "eventId": "guid", "gameEventId": "guid", "playerId": "guid" }
```
Deducts entry fee (Coins) from wallet.

### Revive in Event

```
POST /game-events/revive   [Requires Authorization]
```
```json
{ "eventId": "guid", "gameEventId": "guid", "playerId": "guid" }
```
Deducts revive cost (Diamonds) from wallet.

---

## 16. Guardians & Territory

### Get Tier Guardians

```
GET /guardians/{tierNumber}?seasonId={guid}
```

### Challenge Guardian

```
POST /guardians/challenge   [Requires Authorization]
```
```json
{
  "eventId": "guid",
  "seasonId": "guid",
  "tierNumber": 3,
  "challengerId": "guid",
  "guardianId": "guid"
}
```

### Territory Board

```
GET /territory/{seasonId}/{tierNumber}
```

### Territory Duel

```
POST /territory/duel   [Requires Authorization]
```
```json
{
  "eventId": "guid",
  "seasonId": "guid",
  "tierNumber": 3,
  "category": "Science",
  "challengerId": "guid"
}
```

### Tile Multiplier

```
GET /territory/multiplier/{seasonId}/{tierNumber}/{playerId}
```

---

## 17. Referrals & QR

### Create Referral Code

```
POST /referrals/
```
```json
{ "ownerPlayerId": "guid" }
```

### Look Up Code

```
GET /referrals/{code}
```

### Redeem Referral

```
POST /referrals/{code}/redeem
```
```json
{ "eventId": "guid", "redeemerPlayerId": "guid" }
```
Awards XP + Coins to both owner and redeemer. Prevents self-redeem.

### QR Scan Tracking

```
POST /qr/track-scan     — single scan
POST /qr/sync           — batch sync
GET /qr/history/{playerId}?type=&fromUtc=&toUtc=&page=&pageSize=
```

---

## 18. Votes / Polls

### Cast Vote

```
POST /votes/   [Requires Authorization]
```
```json
{ "playerId": "guid", "option": "Option A", "topic": "best-mode" }
```

### Get Results

```
GET /votes/{topic}/results
```

---

## 19. Analytics

### Send Analytics Events

```
POST /analytics/events
```
Accepts single object or array. All Synaptix dimension fields are optional:
```json
{
  "playerId": "guid",
  "matchId": "guid",
  "questionId": "q-123",
  "mode": "ranked",
  "category": "Science",
  "difficulty": 2,
  "isCorrect": true,
  "answerTimeMs": 3200,
  "pointsAwarded": 100,
  "answeredAtUtc": "2026-04-01T12:00:00Z",
  "synaptixMode": "teen",
  "surface": "arena",
  "audienceSegment": "competitive",
  "entryPoint": "hub_card",
  "brandVersion": "1.0"
}
```

### Synaptix Dimension Reference

| Dimension | Values | Purpose |
|---|---|---|
| `synaptixMode` | `kids`, `teen`, `adult` | Presentation mode active during event |
| `surface` | `hub`, `arena`, `labs`, `pathways`, `journey`, `circles`, `command` | Which product surface the event occurred on |
| `audienceSegment` | Free-form string | Audience segment derived from player profile |
| `entryPoint` | `hub_card`, `deep_link`, `notification`, etc. | How the player navigated to this event |
| `brandVersion` | Semver string | For A/B rollout tracking |

---

## 20. Real-Time (SignalR)

### Hubs

| Hub | Path | Purpose |
|---|---|---|
| **NotificationHub** | `/ws/notify` | General notifications — missions, votes, game events, guardians, territory |
| **MatchHub** | `/ws/match` | Live match communication — answer submissions |
| **PresenceHub** | `/ws/presence` | Player presence/online tracking |

### NotificationHub Groups & Events

**Auto-join:** Connect with `?playerId={guid}` query parameter.

| Join Method | Group Pattern | Event | Payload |
|---|---|---|---|
| `JoinPlayer(playerId)` | `player:{playerId}` | `MissionClaimed` | PlayerId, MissionId, MissionType, MissionKey, RewardXp, RewardCoins, RewardDiamonds, ClaimedAtUtc |
| `JoinTopic(topic)` | `topic:{topic}` | `VoteCast` | VoteCastMessage |
| `JoinGameEvent(gameEventId)` | `game-event:{id}` | `GameEventElimination`, `GameEventClosed` | Elimination/Closed details |
| `JoinGuardianWatch(seasonId, tier)` | `guardian:{s}:{t}` | `GuardianChanged` | GuardianChangedMessage |
| `JoinTerritory(seasonId, tier)` | `territory:{s}:{t}` | `TerritoryCapture` | TerritoryCaptureMesage |

### MatchHub

**Auto-join:** Connect with `?playerId={guid}`.

| Method | Description |
|---|---|
| `JoinMatch(matchId)` | Subscribe to match group |
| `SubmitAnswer(matchId, answerId)` | Broadcast answer to all match participants |

**Server broadcasts:** `answer_submitted` with `{ matchId, user, answerId }`

---

## 21. Terminology Mapping

Use these display names in the Flutter app:

| Backend / Internal | Frontend Display | Context |
|---|---|---|
| Coins | **Credits** | In-game standard currency |
| Diamonds | **Synapse Shards** | Premium currency |
| XP | **Neural XP** | Experience points |
| Leaderboard | **Arena** | Shell/nav label; keep "Leaderboard" inside tables |
| Arcade | **Labs** | Shell/nav label |
| Skill Tree | **Pathways** | Shell/nav label |
| Profile | **Journey** | Shell/nav label |
| Friends / Social | **Circles** | Shell/nav label |
| Admin | **Command** | Admin shell label |
| Trivia Tycoon | **Synaptix** | Product name everywhere |

---

## 22. Known Gaps & Remaining Backend Work

### Affects Frontend Now

| Gap | Impact | Status |
|---|---|---|
| **EF migration for PlayerPreferences + StoreItem** | Tables don't exist in DB yet — endpoints will 500 until migration runs | Needs `dotnet ef migrations add` in build env |
| **Store catalog is empty** | `GET /store/catalog` returns 0 items until an admin seeds data | Need admin seeding script or admin endpoint |
| **No player search** | Cannot search players by handle for friend requests | Backend gap — `GET /users/search?handle=` needed |
| **No IAP receipt validation** | Cannot validate Apple/Google purchase receipts | Backend gap — needs platform-specific receipt checking |
| **Question bank may be empty** | `GET /questions/set` returns 0 if no questions imported | Use admin bulk import or sidecar `/utilities/questions/import` |

### Affects Frontend Later (Not Blocking Alpha)

| Gap | Impact | Timeline |
|---|---|---|
| **Crypto economy layer** | No crypto rewards, wallet linking, or withdrawal | Not started — backend-led |
| **Cosmetics/avatar system** | No equipped items, banners, or visual customization | Not started |
| **Profile enrichment** | No career stats summary (W-L, winrate, seasonal rank trend) | Not started |
| **Unfriend endpoint** | Can add friends but cannot remove them | Quick backend fix needed |
| **ML models** | Churn/difficulty/quality scorers are placeholder rules, not trained models | Sidecar improvement |

### Does NOT Affect Frontend (Backend-Only)

| Item | Notes |
|---|---|
| Build verification (`dotnet build`) | No .NET SDK in current env — needs CI |
| Packet E namespace rename | Deferred — no frontend impact |
| Docker/CI/telemetry naming | Internal infrastructure |

---

## 23. Migration Notes

### Synaptix Rebrand Completion

The backend rebrand (Packets A–D) is complete. All operator-facing dashboards, Swagger docs, and analytics dimensions use Synaptix terminology. The frontend should:

1. **Use the preferences API** (`GET/PUT /users/me/preferences`) to persist mode selection from onboarding
2. **Send analytics dimensions** with every event — at minimum `synaptixMode` and `surface`
3. **Display currency** as Credits / Neural XP / Synapse Shards (the backend dashboards already use these terms)
4. **Connect to the authoritative wallet** via `/mobile/economy/*` endpoints — local Hive persistence should sync with backend state

### Recommended Frontend Integration Order

1. **Auth** — wire `/auth/signup` and `/auth/login` for real accounts
2. **Preferences** — persist Synaptix mode from onboarding via `/users/me/preferences`
3. **Questions** — fetch from `/questions/set`, grade via `/questions/check-batch`
4. **Matches** — wire match start/submit flow
5. **Economy** — sync local wallet with `/mobile/economy/state` and transaction endpoints
6. **Store** — display catalog, wire purchase flow
7. **Leaderboards / Seasons** — wire Arena display
8. **Missions** — connect progress and claiming
9. **Skills** — wire Pathways unlock/respec
10. **Social** — wire Circles (friends, party)
11. **Analytics** — instrument all surfaces with Synaptix dimensions
12. **Real-time** — connect SignalR hubs for live match and notifications
