# Changelog — `claude/add-minio-docker-QBFUx`

All changes made on this branch relative to `main`.

---

## [2026-03-16] Game Event Tracking System

Adds a separate event analytics layer covering the GameEvent, Guardian, and Territory game modes. Deliberately **not** mixed with the ranked-ladder (`PlayerSeasonProfile` / `LeaderboardEntry`) so tier assignment is never distorted.

### New Entity: `PlayerEventStats`
- **File:** `Tycoon.Backend.Domain/Entities/PlayerEventStats.cs`
- One row per player-season; updated incrementally (no batch recompute job needed)
- Fields:
  - **GameEvent** — `EventsEntered`, `EventsTop20`, `EventsWon`, `TotalEventXpEarned`, `TotalEventCoinsEarned`, `ChampionBattleEliminations`
  - **Guardian** — `GuardianPromotions`, `GuardianDefencesWon`, `GuardianDefencesLost`, `GuardianDaysTotal`
  - **Territory** — `TilesEverCaptured`, `CurrentTilesOwned`, `PeakXpMultiplierBps`

### EF Infrastructure
- **`PlayerEventStatsConfiguration`** — `player_event_stats` table; unique index on `(SeasonId, PlayerId)`; composite indexes on `(SeasonId, EventsWon)`, `(SeasonId, GuardianDefencesWon)`, `(SeasonId, CurrentTilesOwned)`
- **`IAppDb` / `AppDb`** — `DbSet<PlayerEventStats> PlayerEventStats`
- **Migration `20260315000000_AddPlayerEventStats`** — `CREATE TABLE player_event_stats` with all columns and indexes

### New Service: `PlayerEventStatsService`
- **File:** `Tycoon.Backend.Application/EventStats/PlayerEventStatsService.cs`
- `GetOrCreateAsync(seasonId, playerId, ct)` — upsert helper used by all hooks

### New Query Handlers (`Application/EventStats/`)
| Handler | What it answers |
|---|---|
| `GetGameEventLeaderboard(GameEventId, Page, PageSize)` | Ranked participant list for a closed event, ordered by `FinalRank`, with prize amounts |
| `GetPlayerEventHistory(PlayerId, SeasonId?, Page, PageSize)` | All game events a player entered (optionally filtered to a season), with rank and prize outcomes |
| `GetEventSeasonLeaderboard(SeasonId, SortBy, Page, PageSize)` | Season-wide event standings; `SortBy` = `event_wins` (default), `events_entered`, `guardian_defences`, `tiles_owned` |
| `GetTerritoryDominanceLeaderboard(SeasonId, TierNumber, Top)` | Live top-N tile owners in a tier, aggregated from `TerritoryTile` (no extra table) |

### New DTOs (`Tycoon.Shared.Contracts/Dtos/EventStatsDtos.cs`)
- `EventLeaderboardEntryDto(PlayerId, FinalRank, AwardedXp, AwardedCoins, EliminatedAt?)`
- `PlayerEventHistoryDto(GameEventId, Kind, FinalRank?, AwardedXp, AwardedCoins, EnteredAt)`
- `EventSeasonLeaderboardEntryDto(PlayerId, EventsWon, EventsTop20, EventsEntered, GuardianDefencesWon, GuardianDaysTotal, CurrentTilesOwned, PeakXpMultiplierBps)`
- `TerritoryDominanceDto(PlayerId, TilesOwned, TotalXpMultiplierBps)`

### New API Endpoints (`GameEventStatsEndpoints`)
```
GET /game-events/{gameEventId}/leaderboard?page&pageSize
GET /game-events/players/{playerId}/event-history?seasonId&page&pageSize   [Authorized]
GET /game-events/season-leaderboard?seasonId&sortBy&page&pageSize
GET /territory/{seasonId}/{tierNumber}/dominance?top
```

### Incremental Hooks Added to Existing Handlers
| Handler | Stats updated |
|---|---|
| `EnterGameEvent` | `EventsEntered++` |
| `CloseGameEventAndDistributePrizes` | `EventsTop20++`, `EventsWon++` (rank 1), `TotalEventXpEarned +=`, `TotalEventCoinsEarned +=` |
| `ResolveGuardianChallenge` (challenger wins) | `GuardianPromotions++` (challenger), `GuardianDefencesLost++` (deposed guardian) |
| `ResolveGuardianChallenge` (guardian wins) | `GuardianDefencesWon++` |
| `ResolveTerritoryDuel` (challenger wins) | `TilesEverCaptured++`, `CurrentTilesOwned` refresh, `PeakXpMultiplierBps` high-water mark |
| `GuardianAssignmentJob` (daily) | `GuardianDaysTotal++` per active guardian (idempotent — only increments when economy txn is newly applied) |

### DI Registration
- `PlayerEventStatsService` registered as `AddScoped` in `Application/DependencyInjection.cs`

---

## [2026-03-15] MinIO Backend Integration

### `IObjectStorage` Abstraction
- Added `Tycoon.Backend.Application/Abstractions/IObjectStorage.cs`
  - `Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct)`
  - `string GetPublicUrl(string key)`

### Storage Implementations
- **`MinioObjectStorage`** — Minio SDK v6.0.5, auto-creates bucket on first upload, returns configurable public URL
- **`LocalObjectStorage`** — zero-config fallback writing to `wwwroot/`; used in local dev and tests

### `MinioOptions` POCO
Fields: `Endpoint`, `AccessKey`, `SecretKey`, `Bucket` (default `tycoon-assets`), `UseSSL`, `PublicEndpoint`

### Dependency Injection
- `DependencyInjection.cs` selects implementation at startup:
  - MinIO: when `MinIO:Endpoint` is configured
  - Local: fallback (including in-memory/test branch)

### `AdminMediaEndpoints` Refactor
- Upload handler now injects `IObjectStorage` instead of inlining file-write logic

### Config
- `appsettings.Docker.json` — added `MinIO` section (`minio:9000`, `tycoon-assets`, `PublicEndpoint: localhost:9000`)

### NuGet
- `Directory.Packages.props` — `Minio` v6.0.5
- `Tycoon.Backend.Infrastructure.csproj` — `<PackageReference Include="Minio" />`

### Test
- `AdminMediaTests.Upload_Stores_File_And_Returns_AssetKey_And_Url`
  Chains `/admin/media/intent` → `/admin/media/upload/{assetKey}` multipart POST, asserts `200 OK`, correct `assetKey`, non-empty `url`

---

## [2026-03-15] Realtime File Layout Fix

- Moved `ConnectionRegistry.cs` and `SignalRMatchmakingNotifier.cs` from `Features/Realtime/` → `Realtime/` to match their declared namespace `Tycoon.Backend.Api.Realtime`

---

## [2026-03-15] Vote Feature — SignalR Broadcast

- `VoteCastMessage` shared contract (`Topic`, `Option`, `Counts` dictionary)
- `INotificationClient.VoteCast(VoteCastMessage)` method added
- `NotificationHub.JoinTopic(string topic)` — lets clients subscribe to a topic group (`topic:{topic}`)
- `VoteCastEventHandler` — domain event handler that broadcasts to the topic group on every vote

**Frontend integration:** connect to `/ws/notify`, call `JoinTopic(topic)`, listen for `"VoteCast"` events to update live tallies.

---

## [2026-03-15] Vote Feature — Expanded Options

- Valid vote options extended to: `!A`, `!B`, `!C`, `!D`, `!True`, `!False`
  (supports 3-choice, 4-choice, and true/false poll formats)

---

## [2026-03-15] Vote Feature — Domain, Handlers & API

### Domain
- `Vote` aggregate: `PlayerId`, `Option`, `Topic`, `TimestampUtc`; raises `VoteCastEvent`
- `VoteCastEvent` domain event

### Infrastructure
- `VoteConfiguration`: table `votes`, unique index on `(PlayerId, Topic)`, index on `Topic`
- `AppDb` / `IAppDb`: `DbSet<Vote> Votes`

### Application
- `CastVote` handler: validates option, enforces one-vote-per-player-per-topic, returns `Recorded / DuplicateVote / InvalidOption`
- `GetVoteResults` handler: groups by option, returns counts + percentages ordered by count

### API
| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/votes` | Required | Cast a vote |
| `GET`  | `/votes/{topic}/results` | Public | Fetch tally |

### Shared Contracts
- `CastVoteRequest`, `CastVoteResponse`, `VoteOptionResult`, `VoteResultsResponse`

> **Note:** run an EF migration to add the `votes` table before deploying.

---

## [2026-03-15] Unit Test Coverage Expansion

### `Tycoon.Backend.Infrastructure.Tests`
| File | What's covered |
|------|---------------|
| `EfRepositoryTests` | `GetAsync` (found / null), Add staging and persistence, multi-entity |
| `IdempotencyIndexModelTests` | Unique index on `EventId` for `ProcessedGameplayEvent`, `EconomyTransaction`, `SeasonPointTransaction`; composite indexes |
| `AppDbDomainEventCollectionTests` | Domain event collection/clearing in `SaveChangesAsync`, multi-aggregate clearing, `MatchCompletedEvent` raised by `Finish()` |

### `Tycoon.Backend.Application.Tests` — Domain
| File | What's covered |
|------|---------------|
| `PlayerTests` | XP/leveling, score clamping, tier idempotency, match result application |
| `MissionClaimTests` | Progress clamping, completion, `MarkClaimed` guard/idempotency, `Reset` |

### `Tycoon.Backend.Application.Tests` — Handlers / Services
| File | What's covered |
|------|---------------|
| `StartMatchHandlerTests` | Match creation, mode defaulting, multi-host isolation |
| `ClaimMissionHandlerTests` | `NotFound / NotCompleted / AlreadyClaimed / Claimed` flows, type filter |
| `ListMissionsHandlerTests` | Active filter, type filter, ordering, DTO mapping |
| `MissionProgressServiceTests` | All mission keys, win/loss branching, multi-mission updates |
| `EconomyServiceTests` | Applied/Duplicate/Invalid/InsufficientFunds, wallet creation, balance accumulation, transaction persistence, history pagination/clamping |
| `SeasonPointsServiceTests` | Applied/Duplicate, profile creation, point accumulation, zero-clamp on negative delta, transaction persistence, `GetActiveSeasonAsync` scenarios |

---

## [2026-03-12] MinIO Docker Setup

### Docker Compose
- `compose.yml` — `minio` service: API port `9000`, console port `9001`, healthcheck, persistent volume, `tycoon-net`
- `compose.prod.yml` — hides ports in production, enforces required password

### Environment Variables
`.env` / `.env.example`:
```
MINIO_ROOT_USER
MINIO_ROOT_PASSWORD
MINIO_PORT        (default 9000)
MINIO_CONSOLE_PORT (default 9001)
```

### Tooling
- `MakeFile` — MinIO added to health check output; new `shell-minio` target
- `Docker.md` — MinIO documented in services table, connection strings, dev URLs, health check reference

---

## [2026-03-12] MinIO Bucket Setup Guide

- Added `docs/minio-setup.md` (278 lines)
  Covers: console access, bucket creation (UI / `mc` CLI / AWS CLI), naming conventions, access policies, upload/download, presigned URLs, .NET SDK connection examples, health check reference

---

## Pending

- EF migration for the `votes` table
- MinIO service needs to be added to the `docker-compose.yml` or Aspire AppHost if using Aspire orchestration
