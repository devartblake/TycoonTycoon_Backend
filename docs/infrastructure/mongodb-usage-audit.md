# MongoDB Usage Audit

## Summary

MongoDB is a live Docker dependency, but the current instance is not carrying application data. It is best treated as prepared infrastructure that should be kept only if analytics rebuilds and crypto settlement logging are intentionally Mongo-backed.

Recommendation: keep MongoDB, with analytics standardized on `synaptix_analytics` and crypto settlement logs kept in `synaptix_crypto`. Removal is not planned.

Implementation note: the follow-up hardening pass adds explicit Mongo DB environment variables, idempotent analytics/settlement indexes, service-JWT settlement authorization, an operator-facing `/admin/mongodb/status` diagnostics endpoint, and Synaptix Security/KMS wiring for secure-channel payload handling.

## Runtime Snapshot

Observed against `synaptix_mongodb` on 2026-05-25 UTC.

| Check | Result |
| --- | --- |
| Image | `mongo:7.0` / server `7.0.30` |
| Container status | running, healthy |
| Restart count | 0 |
| Published port | `27017` |
| Current connections | 14 |
| Rejected connections | 0 |
| App DBs | `tycoon_db`, `synaptix_crypto`; `synaptix_analytics` has no collections |
| App collections | all empty |

Collection counts:

| Database | Collection | Count | Classification |
| --- | --- | ---: | --- |
| `tycoon_db` | `analytics_events` | 0 | optional/prepared |
| `tycoon_db` | `game_events` | 0 | stale/prepared init scaffold |
| `synaptix_crypto` | `crypto_settlements` | 0 | required but currently blocked |
| `synaptix_analytics` | none | 0 | misconfigured/unused target |

Mongo users:

| User | Auth DB | Roles |
| --- | --- | --- |
| `tycoon_admin` | `admin` | `root@admin` |
| `tycoon_app_user` | `tycoon_db` | `readWrite@tycoon_db`, `readWrite@synaptix_analytics`, `readWrite@synaptix_crypto` |

## Usage Map

| Area | Mongo usage | Classification | Notes |
| --- | --- | --- | --- |
| Docker core service | `docker/compose.yml` runs `mongodb`; migration, backend API, sidecar, crypto service depend on its health | required for current compose startup | Disabling Mongo requires compose dependency edits, not just stopping the container. |
| Init script | `docker/init/mongo/01-init.js` creates app user, `game_events`, indexes, and extra DB grants | stale/prepared | `game_events` is not used by the backend game event feature; backend game events are EF/Postgres-backed. |
| Backend API analytics | `Analytics__Enabled=true` plus `ConnectionStrings__mongo` swaps analytics writer and rollup store to Mongo | required if Mongo analytics remains intended | Writes `question_answered_events`, `qa_daily_rollups`, `qa_player_daily_rollups`, but runtime config points database name to `synaptix_analytics`, which currently has no collections. |
| Backend admin analytics rebuild | `ElasticRollupRebuilder` reads Mongo rollups and writes them to Elasticsearch | required if admin rebuild endpoint is retained | `/admin/analytics/rebuild-elastic-rollups` depends on Mongo rollups through `MongoRollupReader`. |
| Sidecar analytics | `POST /analytics/events` writes `analytics_events`; summary/retention read `matches`, `events`, `player_sessions`, `player_activity` | optional/prepared | Sidecar runtime points at `MONGO_DB=tycoon_db`; only `analytics_events` exists and is empty. |
| Sidecar utilities | economy rebalance audit and season snapshots write/read Mongo collections | optional but real | Collections are created lazily and currently absent, so these endpoints have not persisted data in this instance. |
| Crypto settlement service | settlement worker and APIs use `crypto_settlements` for idempotency, retry count, pending list, and history | required but currently blocked | Worker reaches backend every 30 seconds, but `/crypto/withdraw/pending` returns `401 Unauthorized`, so settlement logs stay empty. |
| Migration service | compose provides a Mongo connection and `MongoSettings__Database` | unclear/prepared | Search did not find active migration code using Mongo beyond configuration. |
| Tests | .NET tests cover analytics compatibility, admin analytics rebuild, and gRPC analytics with fakes; sidecar tests fake Mongo collections | test-only | Python tests could not be run because `pytest` is not installed locally or in the containers. |

## Breakage If Mongo Is Disabled

- `backend-api`, `sidecar`, `crypto-service`, and `migration` will not start under current compose because they declare `mongodb` health dependencies.
- Backend analytics will lose the configured `IAnalyticsEventWriter`, `IRollupStore`, and `MongoRollupReader` path unless config is changed to fall back to Postgres analytics.
- `/admin/analytics/rebuild-elastic-rollups` will fail if Elasticsearch rebuilds remain tied to Mongo rollup documents.
- Sidecar startup creates the `analytics_events` unique index and expects `app.state.mongo_db` for analytics and utility endpoints.
- Crypto settlement idempotency and history APIs depend on `crypto_settlements`; removing Mongo requires a replacement store before enabling settlements.

## Recommended Follow-Up Changes

1. Align database names before relying on Mongo analytics.
   - Choose either `synaptix_analytics` or `tycoon_db` as the shared analytics DB.
   - Update `docker/compose.yml`, `docker/.env.example`, `Synaptix.Sidecar/app/config.py`, and backend appsettings so backend and sidecar use the same analytics database.
   - If `synaptix_analytics` is the target, add init indexes for `question_answered_events`, `qa_daily_rollups`, `qa_player_daily_rollups`, and `analytics_events`.

2. Fix crypto settlement authorization before treating `synaptix_crypto` as active.
   - Status: implemented with a dedicated service-JWT policy plus the existing `X-Admin-Ops-Key`.
   - The crypto service now sends `Authorization: Bearer <service JWT>` and `X-Admin-Ops-Key`.
   - Production should set `REQUIRE_CRYPTO_SERVICE_JWT=true` and provide the JWT through `CRYPTO_SERVICE_JWT` or `CRYPTO_SERVICE_JWT_FILE`.

3. Keep Synaptix Security/KMS active for encrypted payload features.
   - Backend secure-channel middleware now uses the internal KMS client guarded by `X-Service-Token`.
   - Internal KMS sessions use a shared payload key so BFF-to-backend encrypted requests and backend-to-BFF encrypted responses can both be processed through the same service session.
   - Configure `KMS_API_BASE_URL`, `KMS_SERVICE_TOKEN`, and `KMS_CLIENT_REQUIRED` when secure-channel traffic is enabled.
   - KMS secure-channel payloads are short-lived request/response envelopes; long-lived service JWTs should remain env/file secrets until a durable Synaptix secret-store API is added.

4. Remove stale Mongo scaffolding if it is not part of the product.
   - Delete or rename the `game_events` Mongo init collection after confirming no external client uses it.
   - Prefer Postgres-backed game event terminology in docs to avoid confusing it with Mongo `game_events`.

5. If Mongo is later removed, replace first, then decommission.
   - Move crypto settlement idempotency/history to Postgres or another durable store.
   - Move sidecar analytics/audit collections to Postgres/Elastic or make those endpoints unavailable.
   - Remove compose health dependencies and Mongo connection settings only after service startup passes without Mongo.

## Verification

Commands run:

| Command | Result |
| --- | --- |
| `docker ps` | MongoDB and dependent services running healthy |
| `docker inspect synaptix_mongodb` | healthy, restart count 0 |
| Authenticated `mongosh serverStatus` and `collStats` | all app collections empty; no rejected connections |
| `docker compose -f docker/compose.yml config --services` | MongoDB is part of the default service graph |
| `dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --filter "FullyQualifiedName~Analytics|FullyQualifiedName~AdminAnalytics|FullyQualifiedName~SidecarGrpcService" --no-restore` | passed: 23/23 |
| `python -m pytest Synaptix.Sidecar/tests` | not run: `pytest` missing locally |
| `docker exec synaptix_sidecar python -m pytest --version` | not run: `pytest` missing in container |
| `docker exec synaptix_crypto_service python -m pytest --version` | not run: `pytest` missing in container |

No Mongo documents were inserted, updated for product workflows, deleted, or migrated during this audit.
