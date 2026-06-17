# Current Codex Task Heartbeat

## Task ID

`20260616-compliance-phase1-complete`

## Task title

Compliance production-readiness: email dispatch, privacy fulfillment, and store compliance gates

## Status

`complete`

## Release priority

`P1 Alpha/Beta operational visibility`

## Objective

Align local Elasticsearch service credentials and rollup write targets so setup, Backend, MigrationService, Kibana, and live analytics indexing use the same required configuration.

## Actual changes

| Area | Change summary |
|---|---|
| Canonical setup handoff | Documents implemented CLI/provisioning behavior, Django as canonical UI/BFF, read-only Phase 1, proposed `setup:read`, route conventions, and deferred mutations. |
| Bootstrap/seed plan | Adds implementation status and labels pre-implementation findings as historical. |
| KMS recommendation | Records implemented Phase 1 abstractions and explicitly defers KMS-backed setup-secret protection. |
| Docker and Django README | Replaces placeholder credential-copy instructions with `Synaptix.Setup init-local` and documents the setup-before-migration chain. |
| Changelog | Records commit `688e35b0` setup hardening and the missing PR #388 SignalR inventory summary. |
| Heartbeat | Records local verification evidence without altering staging blockers or release claims. |
| Backend API | Adds sanitized `/admin/setup/*` diagnostics and enforces `setup:read`. |
| Django dashboard | Adds `/api/operator/setup/*` BFF routes and `/settings/setup/*` read-only pages. |
| Tests | Adds Backend contract/secret-leak tests and Django client/permission/view tests. |
| Durable history | Adds `setup_reports`, sanitized Backend-generated report persistence, `/admin/setup/history`, `/admin/setup/history/latest`, `/api/operator/setup/history`, and `/settings/setup/history`. |
| Mongo analytics | Adds shared question-answered persistence for HTTP, gRPC, and mission job paths; setup ensures Mongo collections/indexes without seeding documents. |
| Elasticsearch | Setup receives Elastic credentials and waits for Elasticsearch health; Backend rollup indexing uses the authenticated DI client; local/Docker write aliases target `synaptix-daily-rollups-write` and `synaptix-player-daily-rollups-write`; Kibana no longer falls back to a stale password. |

## Verification status

- [x] Current branch/baseline checked (`main == origin/main == 688e35b0`)
- [x] Setup CLI commands checked against `Synaptix.Setup/Program.cs`
- [x] MongoDB/Redis configuration keys checked against code and Compose
- [x] Backend/Django route and permission conventions checked
- [x] Local setup verification evidence recorded
- [x] Staging/release claims left unchanged
- [x] Documentation references and diff hygiene checked
- [x] Backend setup API focused tests checked
- [x] Django setup client/view tests and system check checked
- [x] Backend setup durable history focused tests checked
- [x] Django setup history client/view tests checked
- [x] Full solution build checked
- [x] Compose configuration checked
- [x] `git diff --check` checked
- [x] Mongo analytics HTTP/gRPC focused tests checked
- [x] Setup provisioning re-run checked
- [x] Live Docker Mongo analytics smoke checked
- [x] Elasticsearch credentials checked and reset to the Compose-resolved `.env` value
- [x] Setup Elasticsearch validation checked
- [x] Live Docker Elasticsearch rollup indexing smoke checked

## Blockers

The live stack confirms unauthenticated Django setup routes redirect and the Backend rejects ops-key-only setup access. Authenticated live-page smoke remains outstanding because the existing local super-admin credentials do not authenticate against the current database. This does not change staging blockers or release readiness.

The broad Backend API suite also remains red with 28 failures outside the new AdminSetup tests; the focused AdminSetup suite passes and now covers durable setup history.

## Completion notes

Phase 1 read-only setup visibility is implemented with Django as the canonical UI/BFF, the Backend API as a sanitized status/history layer, and `Synaptix.Setup` retained as the privileged offline/one-shot engine. Mongo setup remains schema-only; valid analytics events now populate raw event and rollup collections. Live Docker smoke confirmed one `question_answered` event writes `question_answered_events`, `qa_daily_rollups`, and `qa_player_daily_rollups`. Mutating/destructive operations and KMS-backed setup-secret protection remain deferred.

Elasticsearch credential alignment is complete for the local Compose stack. The persisted Elasticsearch `elastic` user password was reset to the Compose-resolved `ELASTIC_PASSWORD`, setup validates Elasticsearch with `7` succeeded, `0` errors, and `0` warnings, and a live `/analytics/track` smoke confirmed rollup documents in both Elasticsearch write indices.
