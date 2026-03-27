# GitHub Issues Checklist — Backend Debt + Health Pass

This checklist converts the actionable plan into issue-ready work items with dependencies and acceptance criteria.

## Sequence Legend
- `BLOCKER`: Must be completed first.
- `SEQ`: Follow this exact order.
- `PARALLEL`: Can run in parallel after dependencies.

---

## 1) Dashboard Build Target Decision (`BLOCKER`, `SEQ-1`)
- [x] **Issue: Decide and document authoritative Operator Dashboard container target (Blazor vs dashboard-web).**
  - **Context:** `docker/compose.yml` currently builds `docker/Dockerfile.dashboard` for `operator-dashboard`.
  - **Decision:** Keep Blazor dashboard as source of truth unless an explicit architecture decision is approved.
  - **Acceptance Criteria:**
    - [x] Decision recorded in README/docs.
    - [x] Any non-authoritative Dockerfiles are archived or marked experimental.
    - [x] CI/build docs reference only the authoritative target.

## 2) Votes Migration Changelog Drift (`SEQ-2`)
- [x] **Issue: Close stale changelog item that says votes migration is pending.**
  - **Context:** `votes` schema already exists in migration `20260319000000_AddGameEventTables`.
  - **Acceptance Criteria:**
    - [x] Changelog pending section updated to reflect that vote schema migration is already landed.

## 3) Sidecar gRPC Technical Debt (`SEQ-3`)
- [ ] **Issue: Replace SidecarGrpcService placeholders with real application/infrastructure wiring.**
  - **Scope:**
    - [ ] `SubmitAnalyticsEvent` forwards to analytics service/command.
    - [ ] `StreamAnalyticsEvents` uses batch insert path.
    - [ ] `SubmitInferenceResult` persists via repository/service.
    - [ ] `TriggerBackendAction` dispatches action-specific command.
  - **Acceptance Criteria:**
    - [ ] TODO comments removed from the above paths.
    - [ ] Unit/integration tests cover success + validation + failure.

## 4) MobileMatch gRPC Technical Debt (`SEQ-4`)
- [ ] **Issue: Replace MobileMatchGrpcService placeholders with real match/leaderboard integration.**
  - **Scope:**
    - [ ] Answer evaluation integrated with match engine/mediator.
    - [ ] Real correctness, points, and running score values emitted.
    - [ ] Leaderboard watch endpoint returns real leaderboard data source.
  - **Acceptance Criteria:**
    - [ ] Placeholder TODO comments removed from answer and leaderboard flow.
    - [ ] Streaming tests cover match answer and leaderboard update behavior.

## 5) Project Health Pass (`SEQ-5`)
- [ ] **Issue: Run and publish project health pass report.**
  - **Command checklist:**
    - [ ] `dotnet restore`
    - [ ] `dotnet build --configuration Release --no-restore`
    - [ ] `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build`
    - [ ] `bash scripts/check-error-envelope-hardening.sh`
    - [ ] `bash scripts/validate-ef-schema.sh`
    - [ ] `docker compose -f docker/compose.yml build operator-dashboard` *(if Blazor target remains authoritative)*
  - **Acceptance Criteria:**
    - [ ] `docs/PROJECT_HEALTH_REPORT.md` added with command outputs, pass/fail status, and blockers.

---

## Immediate Progress (this branch)
- [x] Changelog updated to mark vote schema migration as already landed.
- [x] Checklist created and ordered sequentially for issue tracking.
- [x] Archived alternate `dashboard-web` Dockerfiles as `.txt` to keep one authoritative dashboard container path without deleting artifacts.
- [x] Added `docs/PROJECT_HEALTH_REPORT.md` with current command outcomes and environment blockers.
- [x] Added `docs/GRPC_TECH_DEBT_NEXT_STEPS.md` as the next-sequence implementation tracker for gRPC TODO debt.
- [x] Started SEQ-3 implementation: wired `SidecarGrpcService` analytics/inference/backend-action paths to concrete services/dispatch.
- [x] Updated README with Sidecar gRPC current-status matrix and planned follow-up items.
