# Alpha Release Criteria

**Release:** alpha-beta-2026  
**Last updated:** 2026-05-16

All items in the **Must Pass** section must be green before the Alpha binary ships. Items in **Should Pass** are strongly recommended; any failures must have documented mitigations.

---

## Must Pass — Backend

### Build & Schema

- [ ] `dotnet build TycoonTycoon_Backend.slnx --configuration Release` completes with **0 errors**
- [ ] `dotnet test` — all existing test suites pass (security contract tests, personalization guardrail tests, gRPC tests)
- [ ] EF schema validation passes: `bash scripts/validate-ef-schema.sh` shows no pending migrations
- [ ] Idempotent SQL artifact generated in CI (`migration-artifacts` artifact on `main` branch)
- [ ] All 24 EF migrations applied to the **staging** PostgreSQL database
- [ ] `GET /health/ready` returns `200 OK` on staging with all dependencies healthy (PostgreSQL, Redis, RabbitMQ, MinIO)

### API Surface

- [ ] `GET /api/v1/app/config` (unauthenticated) returns `200 OK` with `minimumClientVersion` and correct feature flag map
  - Disabled flags: `realtimeMultiplayerEnabled`, `matchmakingEnabled`, `socialEnabled`, `skillTreeEnabled`, `notificationsEnabled`, `experimentsEnabled`, `tomPersonalizationEnabled`, `cryptoEnabled`, `aiSidecarEnabled` → all `false`
  - Enabled flags: `coreTriviaEnabled`, `walletEnabled`, `leaderboardEnabled`, `missionsEnabled` → all `true`
- [ ] `POST /auth/signup` → valid JWT returned
- [ ] `GET /users/me/wallet` → wallet balances returned for authenticated player
- [ ] `POST /quiz/complete` → XP/Coin grant applied; second call with same `EventId` returns `Duplicate` status (idempotency check)
- [ ] `POST /leaderboard` → score recorded; `GET /leaderboards/tiers/{tierId}` reflects update after recalc
- [ ] All disabled endpoints return `HTTP 403` with `code: "FeatureDisabled"` — not `503` or any other status

### Feature Flag Gates

- [ ] `realtime_multiplayer_enabled = false` → `GET /ws/match` (WebSocket upgrade) returns `403`
- [ ] `matchmaking_enabled = false` → `POST /matchmaking/enqueue` returns `403`
- [ ] `social_enabled = false` → `POST /friends/request` returns `403`
- [ ] `social_enabled = false` → `POST /messages/conversations/direct` returns `403`
- [ ] Admin can toggle a flag on and off via `PATCH /api/v1/admin/config` without API restart

### Migration Safety

- [ ] `artifacts/migrations/rollback-notes.md` exists and has been reviewed by at least one engineer
- [ ] Rollback procedure tested on a non-production environment (restore from pg_dump backup)
- [ ] `MigrationWorker` advisory lock confirmed: only one migrator container applies migrations if two start simultaneously (can be validated via Docker Compose with two migration containers)

---

## Must Pass — Integration

- [ ] Flutter smoke test passes against staging: `flutter test test/integration/live_backend_smoke_test.dart`
- [ ] Flutter client correctly detects `FeatureDisabledException` for a disabled endpoint (confirmed via staging test)
- [ ] Flutter client startup completes without error: `appConfigProvider` resolves `GET /api/v1/app/config` and maps feature flags
- [ ] Flutter client login + wallet sync + quiz flow + reward grant completes end-to-end on staging

---

## Must Pass — Operations

- [ ] `docs/releases/ALPHA_ROLLBACK_PLAN.md` reviewed and approved by on-call
- [ ] `docs/releases/ALPHA_KNOWN_ISSUES.md` reviewed; all P0/P1 issues either resolved or have approved mitigations
- [ ] At least one engineer has run through the rollback procedure on staging
- [ ] CI/CD pipeline: `release-gate.yml` workflow passes on the release SHA
- [ ] On-call rotation confirmed for the 72-hour period after Alpha launch

---

## Should Pass — Quality

- [ ] API response time P95 < 500ms for golden path endpoints on staging under 10 concurrent users
- [ ] `POST /quiz/complete` P99 < 1000ms under 5 concurrent requests (rate limiting active)
- [ ] No `ERROR` or `CRITICAL` log entries in Serilog output during smoke test run
- [ ] Hangfire dashboard shows no failed jobs after smoke test run
- [ ] OpenTelemetry traces visible in OTLP collector for golden path operations

---

## Sign-Off Checklist

| Role | Name | Date | Signature |
|------|------|------|-----------|
| Backend Lead | | | |
| QA Lead | | | |
| On-Call Engineer | | | |
| Product Owner | | | |

All sign-offs required before deployment to production.
