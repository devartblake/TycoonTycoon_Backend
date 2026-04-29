# Remaining Tasks & Work Backlog

_Last updated: 2026-04-29 (Unified Personalization Layer complete: core services + admin endpoints + gameplay/store/notification hooks)_

> This file is the canonical "what is left to do" reference.
> For completed work, see [`docs/ALPHA_TASK_AUDIT.md`](ALPHA_TASK_AUDIT.md).
> For the frontend/backend handoff status, see
> [`docs/frontend_backend_handoff_alpha_2026-04-04.md`](frontend_backend_handoff_alpha_2026-04-04.md).

---

## Summary Table

| Area | Priority | Status | Blocked? |
|------|----------|--------|----------|
| Frontend/backend alpha handoff | High | Store, profile/social, question gameplay complete; crypto + ML remain | No |
| **3D Avatar purchase path (Browse → Buy → Download)** | **High** | **Complete** | **No** |
| **Avatar handler unit tests (18 tests)** | **High** | **Complete — `Tycoon.Backend.Application.Tests/Avatars/AvatarHandlerTests.cs`** | **No** |
| **MinIO catalog seeders (StoreItems, SkillNodes, SeasonRewards, Questions)** | **High** | **Complete** | **No** |
| **Pending EF migrations (6 total)** | **High** | **SQL script ready — `docs/pending_migrations_2026-04-29.sql`; DBA must apply to staging + prod** | **No** |
| Store stock system P0 (daily store + stock enforcement) | High | **Complete** | No |
| Store stock system P1 (player catalog + hub + special offers) | High | **Complete** | No |
| **Store stock system P2 (admin policies + flash sales + analytics)** | Medium | **Complete** | No |
| **Unified Personalization Layer — Core (PRs 1–5)** | **High** | **Complete — DB schema, services, APIs, sidecar, DTOs** | **No** |
| **Unified Personalization Layer — Admin Endpoints (Issue 13)** | **High** | **Complete — 8 admin routes, archetype/churn/rule management** | **No** |
| **Unified Personalization Layer — Gameplay Hooks (Issues 9–12)** | **High** | **Complete — question, match, module, store, notification hooks** | **No** |
| **Operator Dashboard Wave B (Questions, Events, Seasons)** | Medium | **Complete — 2026-04-29** | No |
| **Operator Dashboard Wave C (Moderation, Notifications, Economy, Anti-cheat, Event Queue)** | Medium | **Complete — 2026-04-29** | No |
| **Django DefaultPermissions fix** | Medium | **Complete — all 12 scopes now granted on login** | No |
| Operator Dashboard Wave D (Cutover + Blazor decommission) | Low | Staging parallel-run pending (May 8–14); cutover May 15 | Depends on sign-off |
| Phase 2 - Crash recovery stubs | High | Code complete; device validation pending | Yes — needs device |
| Phase 3 - Test coverage (remaining gaps) | Medium | ~4.1% → 40% target | No |
| Phase 4 - Dependency audit | Medium | Partial | No |
| Sprint 1 - Auth/profile integration verification | Medium | Partially improved; live backend verification still needed | Yes — needs live server |
| Sprint 2 - Networking layer (Flutter) | High | Not started | No |
| Synaptix runtime validation | Medium | Blocked | Yes — needs device + backend |
| Backend Packet E (namespace rename) | Deferred | Not started | Intentional deferral |

---

## 1. Frontend/Backend Alpha Handoff

### 1a. Store / payments / subscriptions COMPLETE
- Frontend wiring now covers backend catalog, purchase flows, inventory refresh, subscription
  refresh, `POST /store/iap/validate`, and in-app Stripe/PayPal return handling.
- Payment return routing and post-return reconciliation are in place.

### 1b. Users / profile / social COMPLETE
- Backend search-by-handle is wired for add-friend.
- Career-summary fetch is wired into the enhanced profile experience.
- Loadout `GET` / `PUT` wiring is implemented for hydration and save.
- `DELETE /friends` is wired for unfriend.
- Friends list, incoming requests, sent requests, and suggestions are now wired to backend social endpoints.
- Add-friend-by-username now reconciles against backend friend/request state instead of local placeholder friendship checks.
- DM recipient picking now uses the backend friends roster.
- Shared `/ws` connection paths now append `?playerId=<guid>` for presence compatibility.
- Follow-up profile persistence patch:
  - onboarding now attempts broader backend profile sync
  - login/signup now attempt backend profile hydration
  - bootstrap now attempts backend profile hydration before falling back to Hive

### 1b.ii Friends/social runtime verification REMAINING
- Still needed:
- Live verification of:
  - `GET /users/me/friends`
  - `GET /users/me/friends/requests`
  - `GET /users/me/friends/requests/sent`
  - `GET /users/me/friends/suggestions`
  - `GET /users/search?handle=...` with authenticated requests
  - `DELETE /friends` with authenticated requests
- Two-account/device validation for:
  - send request
  - accept request
  - decline request
  - unfriend
- Presence runtime validation for:
  - initial `presence.bulk`
  - `presence.update` on quiz/match activity
  - offline transitions on disconnect
- Final cleanup/deprecation decision for `FriendDiscoveryService`
- Flutter-enabled formatter/analyzer/test pass

### 1b.i Portable avatar/object-storage persistence IN PROGRESS
- Still needed:
- Frontend hookup to the backend-supported upload flow for picked avatar images
- Frontend adoption of the stable backend profile field for object URL/key after upload
- Frontend upload client for MinIO-backed avatar storage
- Replace local-device avatar file-path persistence with backend-served object references
- Recommended file targets:
- `lib/game/controllers/profile_avatar_controller.dart`
- `lib/core/services/settings/profile_sync_service.dart`
- `lib/core/services/storage/` or `lib/core/services/profile/` upload client
- `lib/screens/profile/` edit/avatar surfaces
- `test/core/services/` upload/profile sync tests
- Notes:
- Asset-path avatars and backend-served URLs are already safe to sync.
- Local emulator/device file paths are not portable across reinstalls and should
  not be treated as authoritative backend profile values.

#### MinIO avatar upload integration checklist

##### Goal
- Replace local-device avatar file path persistence with a backend-backed,
  portable avatar reference that survives emulator wipes, reinstalls, and
  multi-device login.

##### Assumed backend contract
- Implemented backend pattern:
1. Presigned/upload-ticket flow
   - `POST /users/me/avatar/upload-url`
   - Request body:
     ```json
     {
       "fileName": "avatar_1710000000.jpg",
       "contentType": "image/jpeg",
       "contentLength": 183421
     }
     ```
   - Response body:
     ```json
     {
       "uploadUrl": "https://minio.example.com/bucket/key?...signature...",
       "objectKey": "avatars/user-123/avatar_1710000000.jpg",
       "publicUrl": "https://cdn.example.com/avatars/user-123/avatar_1710000000.jpg"
     }
     ```
   - Frontend uploads bytes directly to `uploadUrl`, then persists the final
     profile value via `PATCH /users/me` using `avatarUrl: publicUrl`.

##### Expected profile fields after upload
- Backend should return at least one stable avatar field:
  - `avatarUrl`
- Frontend should treat the returned URL/key as authoritative.
- Frontend should never persist raw local file paths to backend profile state.

##### Sequenced implementation steps
1. Confirm backend upload contract and response shape.
   File targets:
   - backend docs / API contract source
   - `docs/frontend_backend_handoff_alpha_2026-04-04.md`
2. Add a dedicated avatar upload service.
   File targets:
   - `lib/core/services/profile/profile_avatar_upload_service.dart` (new)
   - or `lib/core/services/storage/minio_upload_service.dart` (new)
3. Add typed upload models for request/response payloads.
   File targets:
   - `lib/core/models/profile/avatar_upload_ticket.dart` (new)
   - `lib/core/models/profile/avatar_upload_result.dart` (new)
4. Update `ProfileAvatarController` to:
   - keep local crop/compress behavior
   - upload cropped bytes when the user confirms avatar save
   - replace local temp path with returned backend URL/key
   File targets:
   - `lib/game/controllers/profile_avatar_controller.dart`
5. Update backend profile sync/hydration to treat uploaded avatar URLs as
   authoritative profile values.
   File targets:
   - `lib/core/services/settings/profile_sync_service.dart`
   - `lib/core/services/settings/player_profile_service.dart`
6. Update profile edit/onboarding/avatar-picker surfaces to show upload progress,
   error states, and retry messaging.
   File targets:
   - `lib/screens/profile/`
   - `lib/ui_components/profile_avatar/`
   - `lib/screens/onboarding/`
7. Add tests for upload success/failure and persisted avatar hydration.
   File targets:
   - `test/core/services/profile/` (new)
   - `test/game/controllers/profile_avatar_controller_test.dart`
   - `test/screens/profile/` widget tests as needed

##### Frontend implementation assumptions
- Upload should happen only after the image has already been square-cropped and
  compressed locally.
- Prefer JPEG output for consistency with the existing crop pipeline unless the
  backend requires another content type.
- The frontend should support:
  - optimistic preview from the local cropped file
  - persisted final state from the returned backend URL/key
  - retry without losing the local preview

##### Acceptance criteria
- User picks avatar on emulator/device.
- Avatar is cropped/compressed locally.
- Avatar uploads successfully to backend/MinIO flow.
- Backend returns stable avatar URL/key.
- Frontend stores that returned value in Hive.
- After emulator data wipe and fresh login, the profile rehydrates from backend
  and the avatar is shown again without depending on the old local file path.

### 1c. Questions gameplay COMPLETE
- Question retrieval now prefers `GET /questions/set`.
- Per-question validation uses `POST /questions/check`.
- End-of-quiz authoritative reconciliation uses `POST /questions/check-batch`.
- Legacy/local fallback remains available where needed for resilience.
- `QuestionScreen` now shows a visible backend-vs-local-fallback status banner.
- `QuestionHubService` now logs explicit backend and fallback source decisions for
  category loads, daily/mixed quiz loads, stats reads, and answer validation paths.
- Still needed:
  - live verification that normal gameplay stays on backend data in local/staging/prod environments
  - decision on whether local fallback should remain in production once backend parity is proven
  - deeper observability metrics beyond the new source banner/logging (success ratios, latency, coverage drift)

### 1f. Study backend contract COMPLETE, frontend adoption REMAINING
- Backend now supports:
  - generated category study sets
  - generated weak-area study sets
  - favorites-backed generated study set
  - due-review generated study set from persisted study card state
  - custom saved study sets
  - resumable study sessions
  - flashcard/self-test mode persistence
  - explicit flashcard interaction state persistence
- Frontend still needed:
  - `StudyHubScreen`
  - `/study` route scaffolding
  - favorites review UI
  - category / weak-area / due-review Study entry points
  - custom-set create/edit UI
  - flashcard session UI consuming `StudySessionDto.interactions`
  - self-test UI consuming `StudySessionDto.answeredQuestionIds`
  - recent-session resume wiring
- Primary handoff:
  - `docs/study_frontend_backend_handoff_2026-04-18.md`

### 1d. Crypto economy player surfaces REMAINING
- Still needed:
- Broader player-facing wallet balance integration beyond the main menu sync
- Transaction/history integration
- Wallet link / withdraw UX
- Staking and unstaking UI
- Feature-flag strategy for staged rollout
- Newly completed:
  - `main_menu_screen.dart` now syncs coin/gem display from backend player wallet data during economy refresh
  - duplicate green energy strip under the menu currency display was removed
- Backend endpoints available for consumption:
- `POST /crypto/link-wallet`
- `GET /crypto/balance/{playerId}`
- `GET /crypto/history/{playerId}`
- `POST /crypto/withdraw`
- `POST /crypto/stake`
- `POST /crypto/unstake`
- `GET /crypto/staking/{playerId}`
- Recommended file targets:
- `lib/core/services/crypto/crypto_service.dart`
- `lib/core/models/crypto/`
- `lib/game/providers/crypto_providers.dart`
- `lib/screens/store/crypto_wallet_screen.dart`
- `lib/screens/store/crypto_history_screen.dart`
- `lib/screens/store/crypto_staking_screen.dart`
- `test/core/services/crypto/`
- `test/game/providers/crypto_providers_test.dart`

### 1e. ML enhancement signal consumption REMAINING
- Still needed:
- Frontend service/provider consumption for `POST /ml/churn-risk`
- Frontend service/provider consumption for `POST /ml/match-quality`
- Optional UX treatment and telemetry for `source` values (`deployed-model` vs `heuristic`)
- These should remain optional enhancement signals rather than hard dependencies for core UX.
- Recommended file targets:
- `lib/core/services/ml/ml_signal_service.dart`
- `lib/core/models/ml/`
- `lib/game/providers/ml_providers.dart`
- `lib/core/services/analytics/`
- `lib/screens/home/`
- `lib/screens/question/`
- `test/core/services/ml/`
- `test/game/providers/ml_providers_test.dart`

---

## 11. Store Stock System (P0 / P1 / P2)

Full design: `docs/store_stock_backend_implementation_and_schema.md`
API contracts: `docs/store_admin_backend_handoff_2026-04-23.md`

### Prerequisite — SeasonRewardRule EF Migration

The `season_reward_rules` table does not yet have an EF Core migration. Run this before adding the stock system migrations:

```bash
dotnet ef migrations add AddSeasonRewardRules \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
dotnet ef database update \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
```

### 11a. P0 — Daily Store + Stock Enforcement ✅ COMPLETE

- [x] `StoreStockPolicy` + `PlayerStoreStockState` domain entities
- [x] EF configs + migration `20260425130000_AddStoreStockSystem`
- [x] `IStoreStockService` / `StoreStockService`: lazy reset, check, consume, daily items
- [x] `GET /store/daily` — stock-policy items with per-player remaining qty + reset time
- [x] `POST /store/purchase` extended: stock check before transaction, consume on success
- [x] `409 store_item_out_of_stock` error code

### 11b. P1 — Player-Specific Catalog + Hub Surface ✅ COMPLETE

- [x] `FlashSale` entity + EF config + migration `20260425140000_AddFlashSale`
- [x] `GET /store/catalog/{playerId}` — full catalog with per-player stock state, ownership, discounts
- [x] `GET /store/hub` — featured items, daily stock items, category list
- [x] `GET /store/special-offers` — active flash sales joined with catalog items
- [x] `PlayerStoreCatalogItemDto` with `availabilityState` + `stockState` fields
- [x] Concurrent data loading (policies, stock states, flash sales, ownership) via `Task.WhenAll`

### 11c. P2 — Admin Stock Management ✅ COMPLETE

- [x] `GET /admin/store/stock-policies` + `PUT /admin/store/stock-policies/{sku}` — list + upsert (creates or updates)
- [x] `POST /admin/store/stock-policies/bulk-reset` — resets all players for a given list of SKUs
- [x] `GET /admin/store/player-stock/{playerId}` + `POST /admin/store/player-stock/{playerId}/override` — view + set `EffectiveMaxQuantity`
- [x] Flash sale CRUD: `GET /admin/store/flash-sales`, `POST /admin/store/flash-sales`, `DELETE /admin/store/flash-sales/{id}` (soft cancel)
- [x] Reward limit management: `GET /admin/store/reward-limits/{rewardId}` + `PUT` (upsert `RewardClaimRule` entity)
- [x] Analytics: `GET /admin/store/analytics/purchases` (aggregates + date filter) + `GET /admin/store/analytics/stock-resets` (paginated reset history)
- [x] `RewardClaimRule` entity + EF config + migration `20260426100000_AddRewardClaimRule`
- [x] `EffectiveMaxQuantity` column on `PlayerStoreStockState` + migration `20260426110000_AddEffectiveMaxQuantity`

---

## 2. Phase 2 - Crash Recovery + Core Stubs

### 2a. Crash recovery state restoration COMPLETE
- **Files:** `lib/main.dart`, `lib/core/services/crash_recovery_service.dart`,
  `lib/core/services/state_persistence_service.dart`
- **Completed:** Crash/restart recovery now rehydrates saved quiz progress, player progress,
  and persisted profile/session metadata. Recovery acceptance also clears the crash flag so
  the restore prompt does not loop on the next clean launch.

### 2b. Notification persistence COMPLETE
- **Files:** `lib/core/bootstrap/app_init.dart`,
  `lib/game/providers/notification_history_store.dart`,
  `lib/game/providers/notification_template_store.dart`
- **Completed:** Notification templates and notification history are now restored from
  persistent storage during app bootstrap, so restart no longer drops the in-app notification
  context/history used by the admin and notification surfaces.

### 2c. Profile avatar cropping COMPLETE
- **Files:** `lib/game/controllers/profile_avatar_controller.dart`
- **Completed:** The active avatar picker flow already performs a centered square crop and
  JPEG re-encode before persisting the chosen avatar path. This backlog item was stale and has
  been reclassified as complete.

### 2d. Remaining `UnimplementedError` throws RESOLVED

| File | Line | Detail | Resolution |
|------|------|--------|------------|
| `lib/game/providers/core_providers.dart` | 41 | Provider stub | Intentional design-time guard; doc comment added clarifying it is overridden at startup |
| `lib/core/services/analytics/app_lifecycle.dart` | 18 | Lifecycle provider stub | Intentional design-time guard; doc comment added clarifying override via `AppLifecycleObserver` |
| `lib/screens/rewards/spin_earn_screen.dart` | n/a | Stale comment referencing a replaced `UnimplementedError` | Stale comment removed |

---

## 3. Phase 3 - Test Coverage Remaining Gaps

**Current:** 45 test files / 1,088 source files ~= **4.1%** (target: **40%** on `lib/game/` and `lib/core/`)

### 3a. Arcade game controllers COMPLETE

| File added | Coverage |
|---|---|
| `test/arcade/games/memory_flip_controller_test.dart` | `MemoryFlipController` - initial state, deck structure, `flip()` (first/match/miss/ignored), `allMatched -> isOver`, `toResult()`, `dispose()` |
| `test/arcade/games/pattern_sprint_controller_test.dart` | `PatternSprintController` - initial state, `answer()` (correct/wrong/lock), streak multiplier, question generation across all difficulties, `toResult()`, `dispose()` |

### 3b. Presence services (not yet tested)

#### `RichPresenceService` (`lib/core/services/presence/rich_presence_service.dart`, 357 lines)
- **Dependency:** Singleton - test using `initialize(useWebSocket: false)` (legacy polling mode)
  to avoid real WebSocket connections.

| Method / scenario | What to test |
|---|---|
| `initialize(useWebSocket: false)` | `currentUserPresence` is not null after init |
| `updateCurrentUserPresence()` | `currentUserPresence` reflects new status/activity |
| `setGameActivity()` | Presence activity updated; notifies listeners |
| `clearGameActivity()` | Activity cleared; notifies listeners |
| `canUserJoinGame(userId)` | Returns `true` when user is `online`/`idle`; `false` when `inGame`/`offline` |
| `watchUserPresence(userId)` | Stream emits when `updateFriendPresence` is called |
| `dispose()` | No throw; timers cancelled |

#### `TypingIndicatorService` COMPLETE
- `test/core/services/presence/typing_indicator_service_test.dart` added
- Coverage: `isAnyoneTyping`, `isCurrentUserTyping`, `startTyping`, `stopTyping`,
  `updateUserTypingStatus`, `handleTextInput`, `handleMessageSent`, `clearConversationTyping`,
  `getTypingText`, `getTypingStats`

### 3c. Auth flow edge cases (not yet tested)
- Social login flows (OAuth token injection, account linking)
- Token refresh when `AuthHttpClient` encounters a 401 on a concurrent request
- Offline login attempt (cached credentials vs. no cache)
- Logout clears all stored tokens and in-memory state

### 3d. Widget tree tests (not yet tested)
- Leaderboard screen renders `AnimatedRankBadge` and `EnhancedScoreDisplay` correctly
  (basic rendering exists in `test/widgets/leaderboard_widgets_test.dart` - extend with
  interaction tests)
- `ArcadeGameShell` mounts the correct game widget for each `ArcadeGameId`
- `DailyBonusScreen` renders correct coins/gems/streak values from `ArcadeDailyBonusService`
- `ArcadeMissionsScreen` renders claimed vs. unclaimed missions correctly

### 3e. Other service gaps COMPLETE

| File added | Coverage |
|---|---|
| `test/arcade/services/arcade_session_service_test.dart` | `startSession()`, `endSession()`, `attachDuration()` |
| `test/arcade/services/arcade_registry_test.dart` | All 3 game definitions, IDs, titles, difficulties, `ArcadeGameId` completeness |
| `test/arcade/leaderboards/local_arcade_leaderboard_service_test.dart` | `recordRun()`, `top()` (sort order, limit), `best()`, `wouldBeNewBest()`, `clearBoard()`, `clearAll()`, persistence, `topForGame()` |

---

## 4. Phase 4 - Dependency Audit

### 4a. Outdated packages
- **Action:** Run `flutter pub outdated` in a Flutter-enabled environment.
- **Goal:** Apply security fixes; identify packages with breaking-change upgrades.
- **Note:** `just_audio` (music streaming) and `flutter_soloud` (low-latency SFX) are
  intentionally kept separate - different use cases, not consolidatable.

### 4b. Unused transitive dependencies
- **Action:** Run `flutter pub deps --style=compact` and cross-reference with imports.
- **Goal:** Remove any transitive packages pulled in by old features that were removed.

---

## 5. Sprint 1 - P2 Auth/Profile Integration Verification

Status is improved but still requires explicit live verification:

- [ ] `AuthHttpClient` provider is registered in the Riverpod provider graph (confirm in
  `lib/game/providers/core_providers.dart` or `auth_providers.dart`)
- [ ] Login screen shows correct error messages for 401, 403, 422, network timeout
- [ ] Signup screen validates and surfaces backend error codes
- [ ] Auto-refresh (token renewal on 401) tested end-to-end with a live or stub backend
- [ ] Backend profile hydration after login verified against a live backend endpoint
- [ ] Backend profile hydration after emulator/data wipe verified end-to-end
- [ ] Local web auth CORS/origin validation confirmed for Docker-hosted backend

### 5a. Local web auth / Docker verification REMAINING
- Frontend-side diagnosis is complete:
  - browser auth should target `http://localhost:5000` for local host access
  - stale `https://localhost:5000` local defaults were corrected in frontend config
- Still needed:
  - verify Docker publishes the backend port to the host machine
  - verify backend CORS allows the Flutter web dev origin
  - verify any social/OAuth local callback URLs use the same reachable origin/protocol

---

## 6. Sprint 2 - Networking Layer

Not started. Estimated ~70 min.

- [ ] Copy 4 networking files into `lib/core/networking/`
- [ ] Add `web_socket_channel` and `uuid` to `pubspec.yaml`
- [ ] Add 3 new Riverpod providers (WebSocket connection, message stream, reconnect logic)
- [ ] Integrate with existing HTTP layer (`AuthHttpClient` -> `HttpClient` -> WebSocket upgrade)

---

## 7. Synaptix Runtime Validation

**Blocked** - requires a running physical/simulator device and a live backend.
Curl CONNECT tunnel failure blocks live smoke-checking in CI. Flutter SDK not on PATH in CI.

- [ ] App launch - no crash, no ANR
- [ ] Auth/bootstrap flow - login, token refresh, session establishment
- [ ] Onboarding - age group selection -> mode mapping (`Kids`/`Teen`/`Adult`)
- [ ] Hub rendering - correct mode-specific content for each Synaptix mode
- [ ] Arena/Labs/Pathways/Journey/Circles/Command navigation - all routes reachable
- [ ] Brand validation - no visible "Trivia Tycoon" strings anywhere in the UI
- [ ] Retention hooks - daily bonus prompt, mission notification after first session
- [ ] Premium Hub - upgrade prompt, feature gating, paywall rendering

---

## 8. Synaptix Backend - Packet E (Deferred)

Intentionally deferred to after Alpha launch. No urgency.

- [ ] Namespace rename: `Tycoon.Backend.*` -> `Synaptix.Backend.*` across all backend projects
- [ ] Service/telemetry identifier rename
- [ ] Docker/CI/issuer/audience configuration naming cleanup

---

## 9. Web Platform - Remaining `dart:io` Screen Files

**Startup fixed:** `api_service.dart` and `auth_error_messages.dart` no longer import
`dart:io`, eliminating the startup cascade failure on web.

The following files still import `dart:io` unconditionally. They are **not** in the startup
path so the app loads on web, but these screens/features will throw when visited on web:

| File | Used in | Impact on web |
|------|---------|---------------|
| `lib/ui_components/spin_wheel/services/prize_log_export_service.dart` | Spin-wheel export | File export will fail |
| `lib/ui_components/shimmer_avatar/widgets/avatar_content.dart` | Avatar display | Avatar widget will throw |
| `lib/ui_components/shimmer_avatar/utils/avatar_helpers.dart` | Avatar helpers | Avatar feature will throw |
| `lib/ui_components/profile_avatar/profile_image_picker.dart` | Avatar upload | File picker unavailable on web |
| `lib/ui_components/depth_card_3d/core/depth_card_3d.dart` | 3D cards | May throw if `dart:io` class is instantiated |
| `lib/ui_components/confetti/utils/confetti_log_manager.dart` | Confetti debug overlay | Debug-only; low impact |
| `lib/ui_components/color_picker/utils/color_log_manager.dart` | Color picker | Debug-only; low impact |
| `lib/screens/profile/widgets/profile_avatar_preview.dart` | Profile screen | Avatar preview will throw |
| `lib/screens/profile/widgets/shimmer_avatar.dart` | Profile screen | Shimmer avatar will throw |
| `lib/screens/profile/widgets/avatar_image_card.dart` | Profile screen | Avatar card will throw |
| `lib/screens/profile/widgets/avatar_package_image.dart` | Profile screen | Avatar image will throw |
| `lib/screens/profile/tabs/collection_tab.dart` | Profile -> Collections tab | Collections tab will throw |
| `lib/game/services/avatar_package_service.dart` | Avatar packages | Service will throw on web |
| `lib/game/services/collection_items_loader.dart` | Collection items | Loader will throw on web |
| `lib/game/controllers/profile_avatar_controller.dart` | Profile avatar | Controller will throw on web |
| `lib/admin/widgets/encrypted_file_preview.dart` | Admin | Admin only; low web priority |
| `lib/admin/widgets/question_editor_form.dart` | Admin | Admin only; low web priority |
| `lib/admin/questions/question_list_screen.dart` | Admin | Admin only; low web priority |
| `lib/admin/questions/file_import_export_screen.dart` | Admin | Admin only; low web priority |

**Recommended action:** For each file, either add `kIsWeb` guards around file-system calls
or use conditional imports to provide web-safe stubs.

---

## 10. Windows Build Prerequisite

### 10a. NuGet required for `flutter_inappwebview_windows`
- **Status:** prerequisite not satisfied on the current Windows environment
- **Observed failure:** Windows build fails with `NUGET-NOTFOUND` / exit code `9009`
  while resolving `flutter_inappwebview_windows` native dependencies.
- **Required fix:** install `nuget.exe` and make sure it is available on `PATH`
  before running `flutter build windows`.
- **Recommended verification:**
  - `nuget help`
  - `flutter clean`
  - `flutter pub get`
  - `flutter build windows`
- **Important note:** the CMake `CMP0175` warning from the plugin is noisy but was
  not the primary blocker in the reported failure; missing NuGet was the actual
  build-stopping condition.

---

## Release Readiness Checklist

| Item | Status |
|------|--------|
| Alpha handoff core frontend wiring | Complete for store/profile/questions; crypto + ML remain |
| Zero `debugPrint` in production business logic | Done |
| Zero `UnimplementedError` in user-facing paths | Done - intentional design-time guards documented |
| Web startup crash (`dart:io` cascade) | Fixed - `api_service.dart` + `auth_error_messages.dart` |
| Crash recovery tested on iOS and Android | Not yet validated on device |
| Test coverage >= 40% on `lib/game/` and `lib/core/` | Not yet, ~4.1% currently (45 test files) |
| No critical CVEs in dependency tree | Pending `flutter pub outdated` |
| No single Dart file exceeds 1,700 lines | Done |
| CI pipeline enforces coverage + lint + no raw prints | Not configured |
| All source-code TODO/FIXME resolved | Done (0 remaining) |
| Runtime validation of all Synaptix screens | Blocked (needs device) |
| Sprint 2 networking layer | Not started |
| Remaining `dart:io` in screen-level files (web) | 19 files - app loads, affected screens throw |
| Windows desktop prerequisite (`nuget.exe` for `flutter_inappwebview_windows`) | Pending on local machine |
