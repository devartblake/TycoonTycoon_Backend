# Project Status — 2026-05-09

_Derived from all docs in `/docs/`. Reflects state after Packets A–D completion, Security KMS implementation, Rewards/Spins backend endpoints, and BE Packet E completion. Last updated: 2026-05-26._

---

## Completed ✅

### Backend — Core Systems

| System | Status | Notes |
|---|---|---|
| Auth + JWT | ✅ | Issuer `SynaptixApi` / Audience `SynaptixApp` |
| Store (P0/P1/P2) | ✅ | Daily store, catalog, hub, flash sales, stock enforcement, admin overrides |
| Avatar purchase + MinIO | ✅ | GLB download, presigned upload-url, 18 handler tests |
| Crypto economy | ✅ | 10 endpoints: link-wallet, balance, history, withdraw, stake/unstake. Local Docker contract smoke passed 2026-05-10; staging validation still needs staging credentials |
| Missions | ✅ | List, progress (match/round), claim; 10 Synaptix titles seeded |
| Questions | ✅ | Set, preview, check, check-batch; `ORDER BY RANDOM()` performance fixed |
| Leaderboards / Tiers | ✅ | 6 Synaptix tiers upserted; `Tier.UpdateDefinition()` added |
| Seasons / Season Rewards | ✅ | Season reward claims, eligibility, admin recompute |
| Study Hub | ✅ | Category sets, weak-area, favorites, custom sets, resumable sessions |
| Personalization | ✅ | Mind profiles, behavior hooks, guardrails, audit trail, 8 admin routes |
| Personalization sidecar | ✅ | Score-player, recommendation-candidates gRPC |
| Friends / Social | ✅ | Search, requests, suggestions, unfriend, DM presence |
| Matchmaking / Matches / Party | ✅ | All wired |
| Skills / Pathways | ✅ | Nodes, unlocks, tree |
| Notifications | ✅ | Push, in-app, dead-letter replay |
| ML scoring | ✅ | Churn risk, match quality endpoints |
| Real-time (SignalR) | ✅ | Match, presence, notify WebSocket hubs |
| Secure Channel / KMS | ✅ | X25519 ECDH, HKDF, AES-256-GCM, Vault Transit, Redis replay; `SecureChannelFilter` live on 14 sensitive endpoints |
| Rewards (daily/weekly) | ✅ | `POST /rewards/daily/claim`, `GET /rewards/weekly-streak/{id}`, `POST /rewards/weekly/claim`, `GET /rewards/daily-config`, `GET /rewards/weekly-schedule`, `GET /rewards/spin-reward-steps` — implemented 2026-05-09 |
| Spins backend | ✅ | `POST /spins/result`, `GET /spins/stats/{id}`, `GET /spins/history/{id}` — implemented 2026-05-09 |

### Synaptix Rebrand — Packets A–D + E WS1

| Packet | Backend | Frontend |
|---|---|---|
| A — Branding + surface reframe | ✅ | ✅ |
| B — Mode/theme + preferences API | ✅ | ✅ |
| C — Feature surfaces (Arena/Labs/Pathways/Circles/Command) | ✅ | ✅ |
| D — Analytics dimensions + stabilization | ✅ | ✅ |
| E WS1 — Frontend symbol cleanup (`TriviaTycoonApp` → `SynaptixApp`) | N/A | ✅ commit `79bc788` (2026-05-08) |

### Operator Dashboard

| Item | Status |
|---|---|
| Wave A — original Blazor baseline | ✅ |
| Wave B — Questions, Events, Seasons | ✅ (2026-04-29) |
| Wave C — Moderation, Notifications, Economy, Anti-cheat, Event Queue | ✅ (2026-04-29) |
| DefaultPermissions fix (12 scopes) | ✅ |
| Django rollback drill | ✅ (2026-04-15) |
| Pending migrations SQL (`docs/pending_migrations_2026-04-29.sql`) | ✅ ready |

### Infrastructure

| Item | Status |
|---|---|
| .NET 10 upgrade (all 20 projects) | ✅ |
| MinIO seeding (questions, store items, skill nodes, season rewards) | ✅ |
| EF migrations (including `20260509120000_AddDailyAndWeeklyRewards`) | ✅ scripts ready |
| Docker compose security stack (Vault 1.17, KMS API, Redis) | ✅ |

---

## Remaining ⚠️

### Operational / DevOps — Time-Sensitive

| Item | Owner | Deadline | Notes |
|---|---|---|---|
| Apply `pending_migrations_2026-04-29.sql` to staging + prod | DBA | Before May 8 | Blocks operator parallel-run |
| Run `Tycoon.MigrationService` against dev/staging DB | Backend | ASAP | Applies tier renames + mission seeds |
| Staging parallel-run with real operator accounts | DevOps + Operators | **May 8–14** | 2-hour session; sign-off required |
| Operator sign-off (QA Lead + Backend Lead + On-call) | Leads | After parallel-run | Required for cutover |
| Nginx hard cutover — Django as sole operator UI | DevOps | **May 15** | Blazor rollback window closes June 12 |
| `dotnet build` clean compile verification | Backend | ASAP | Blocked by env (HTTP 403 on SDK bootstrap) |
| Smoke suite (`./scripts/alpha-p0-smoke.sh`) against running API | Backend | Before soft launch | Go/no-go blocker |
| Frontend ↔ backend terminology spot-check at runtime | Both teams | Before soft launch | Go/no-go blocker |

### Frontend — Remaining Work (Flutter repo)

| Item | Priority | Backend dep? | Notes |
|---|---|---|---|
| `hybrid_mission_state.dart` stub fix (line 388) | 🔴 HIGH | No | Replace hardcoded `'current-user-id'` with `currentUserIdProvider` from `profile_providers.dart` |
| Avatar MinIO upload integration | HIGH | Ready | `POST /users/me/avatar/upload-url` exists; Flutter upload client needed |
| Retention hooks (streak, bonus challenge, session-end trigger) | HIGH | No | Sound layer still missing |
| ~~Crypto economy surfaces (wallet, history, staking UX)~~ | ~~HIGH~~ | ~~Ready~~ | ✅ Complete 2026-05-23 — `CryptoWalletScreen`, all providers, 11 test files |
| Onboarding runtime validation (restore, reward reveal, handoff) | HIGH | Yes | Needs live device + backend |
| Sprint 2 networking layer | HIGH | No | ~70 min; 4 files + 3 Riverpod providers |
| Full QA pass all modes (kids/teen/adult) | MED | No | Needs device |
| ~~Study Hub UI (`StudyHubScreen`, `/study` route, flashcard UI)~~ | ~~MED~~ | ~~Ready~~ | ✅ Complete 2026-05-23 — all 6 routes, session resume, custom sets |
| Synaptix runtime validation (all screens, no Trivia Tycoon strings) | MED | Yes | Blocked — needs device + live backend |
| Test coverage (~19.4% → 40% target on `lib/game/` + `lib/core/`) | MED | No | 240/1,239 files; `RichPresenceService`, auth edge cases, widget tree tests remain |
| ML signal consumption (`POST /ml/churn-risk`, `POST /ml/match-quality`) | LOW | Ready | Optional enhancement signals |
| 19 `dart:io` screen files — web guards needed | LOW | No | App loads on web but affected screens throw |
| Sound cue layer | LOW | No | Haptics + motion present; audio missing |

### Secure Channel — Rollout Remaining

| Item | Priority |
|---|---|
| Integrate `EncryptedApiClient` into one non-critical Flutter endpoint (first milestone) | HIGH |
| Phase rollout to refresh/match/economy/messages endpoints | MED |
| Tests: wrong nonce/sequence, expiry renewal, logout clear, web fallback | MED |
| Backend schema + replay/sequence semantics validated against staging | HIGH |

### BE Packet E — Complete ✅

| Item | Status | Notes |
|---|---|---|
| Elasticsearch alias rename (`tycoon-qa-*` → `synaptix-*`) | ✅ 2026-05-09 | ElasticOptions, ElasticAdmin, all appsettings |
| Docker container names rename (`tycoon_*` → `synaptix_*`) | ✅ 2026-05-09 | compose.yml + supporting compose files |
| Telemetry `ServiceName` rename | ✅ 2026-05-09 | All appsettings and compose env vars |
| CI/monitoring identifiers | ✅ 2026-05-09 | prometheus.yml, compose-smoke.yml, scripts |
| C# namespace rename (`Tycoon.Backend.*` → `Synaptix.Backend.*`) | ✅ 2026-05-22 | 987 namespace declarations + 1,491 using statements across 1,001 files; solution + csproj ProjectReferences updated |
| Non-C# service rename (CryptoService, OperatorDashboard.Django/Vue, Sidecar) | ✅ 2026-05-22 | `git mv` with Dockerfile + compose + CI references updated |
| JWT issuer/audience update | ✅ 2026-05-22 | `SynaptixBackendApi` / `SynaptixFrontendApp`; Flutter team handoff doc created |

### Deferred — Intentional (Post-Alpha)

| Item | Reason |
|---|---|
| FE Packet E WS2 — `package:trivia_tycoon` → `synaptix` (564 imports) | Blocked: awaiting store/legal bundle ID transition plan |
| Android app ID + iOS bundle ID change | Store/legal plan required; treated as new app by stores |
| IAP `GooglePackageName` (`com.tycoon.app.*` → `com.synaptix.app.*`) | Must coordinate with bundle ID change |
| Reward Reactor endpoints (`/arcade/reactor/spin`, `/arcade/reactor/claim`) | Planning docs exist; implementation not started; legacy `/arcade/spin/claim` serves Alpha |

---

## Key Dates

| Date | Event |
|---|---|
| 2026-04-15 | Django rollback drill completed |
| 2026-04-29 | Operator Dashboard Waves B + C complete |
| 2026-05-01 | .NET 10 upgrade complete; Personalization complete |
| 2026-05-02 | Security KMS full implementation |
| 2026-05-03 | Synaptix rebrand Packets A–D complete (backend) |
| 2026-05-08 | FE-E Workstream 1 complete (commit `79bc788`) |
| 2026-05-09 | Rewards + Spins backend endpoints implemented |
| 2026-05-09 | BE Packet E: Elasticsearch, Docker, CI, telemetry renames |
| 2026-05-22 | BE Packet E: C# namespace rename complete; non-C# services, JWT contracts, frontend handoff doc |
| 2026-05-26 | store_purchases_enabled flag; Designer.cs fix; production config template; CHANGELOG update |
| **2026-05-08–14** | Operator Dashboard staging parallel-run window |
| **2026-05-15** | Operator Dashboard hard cutover (Django only) |
| **2026-06-12** | Blazor rollback window closes |

---

## Overall Assessment

| Area | Completion | Notes |
|---|---|---|
| **Backend core systems** | ~97% | All gameplay, economy, personalization, security implemented |
| **BE Packet E (infra rename)** | 100% | ES, Docker, telemetry, C# namespaces, non-C# services, JWT contracts all complete (2026-05-22) |
| **Frontend** | ~75% | Rebrand, Hub, onboarding done; crypto/monetization/networking open |
| **Operator Dashboard** | 100% code | Awaiting May 8–14 parallel-run and May 15 cutover |
| **Soft launch readiness** | Blocked (staging) | Repo-side prep 100% complete; 8 P0 blockers all require live staging environment |
