# Tycoon → Synaptix rename — Wave 1 complete

**Date:** 2026-07-13  
**Scope:** Low-risk identifiers, banners, defaults, AppHost resource names.  
**Not in this wave:** gRPC package rename (Wave 4), repo/slnx rename (Wave 5).  
**Completed later:** [Waves 2+3](TYCOON_RENAME_WAVE2_3.md) — `SynaptixApiFactory`, metric dual-write.

## Changed

| Area | Before | After |
|------|--------|--------|
| API startup log | Tycoon Backend API | Synaptix Backend API |
| Sidecar default store path | `/tmp/tycoon-sidecar/...` | `/tmp/synaptix-sidecar/...` |
| Scripts | Tycoon banners in setup-dev, migrate, compose-smoke | Synaptix |
| Compose projects | `tycoon-security`, `tycoon-compliance` | `synaptix-security`, `synaptix-compliance` |
| Smoke / admin emails | `@tycoon.local` | `@synaptix.local` |
| AppHost resources | `tycoon-db`, `tycoon-api`, … | `synaptix-db`, `synaptix-api`, … |
| AppHost dashboard | Broken Blazor project ref | Removed (React via Compose) |
| Connection string resolution | Prefer tycoon-* | Prefer `db` / `synaptix-*`; **legacy tycoon-* kept** |
| Mongo default DB | `tycoon_analytics` | `synaptix_analytics` |
| MinIO default bucket | `tycoon-assets` | `synaptix-assets` |
| In-memory test DB prefix | `tycoon-tests` | `synaptix-tests` |
| Observability ServiceName | `tycoon-backend` | `synaptix-backend` |
| Email defaults / copy | Trivia Tycoon | Synaptix |
| OpenAPI title/contact/servers | TycoonTycoon / tycoonplay | Synaptix placeholders |
| Docker MakeFile echoes | Tycoon Backend | Synaptix Backend |

## Compatibility retained

- Connection string keys `tycoon-db` / `tycoon_db` still resolve.
- Repo / solution still named `TycoonTycoon_Backend.slnx`.
- gRPC packages renamed to `synaptix.sidecar` / `synaptix.mobile` (Wave 4).
- Metric series dual-write `synaptix_*` + `tycoon_*` (Wave 3; see WAVE2_3).
- Test host factory renamed to `SynaptixApiFactory` (Wave 2).

## Ops notes

- If you pin **ES/MinIO/Mongo** names from old defaults, set explicit env/config (do not rely on new defaults in prod).
- Grafana service filters for `tycoon-backend` should accept `synaptix-backend` (or dual-label temporarily).
- Compose **project name** change for security/compliance stacks creates a new compose project; down the old `tycoon-*` project if leftover containers remain.
