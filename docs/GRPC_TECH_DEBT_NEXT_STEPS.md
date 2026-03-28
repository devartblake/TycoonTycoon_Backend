# gRPC Technical Debt — Next Steps (Sequential)

This document operationalizes the next steps after dashboard-target alignment.

## Scope Sources
- `Tycoon.Backend.Api/Grpc/SidecarGrpcService.cs`
- `Tycoon.Backend.Api/Grpc/MobileMatchGrpcService.cs`

## Workstream 1 — SidecarGrpcService

### 1.1 SubmitAnalyticsEvent wiring
- [x] Replace placeholder accept/echo path with application command/service dispatch.
- [x] Add structured error responses and logging dimensions.

### 1.2 StreamAnalyticsEvents batching
- [x] Implement batch write path (service/repository abstraction).
- [x] Add backpressure/size guard + cancellation handling tests.

### 1.3 SubmitInferenceResult persistence
- [x] Persist model/entity/score payload via repository abstraction.
- [x] Replace in-memory store with durable persistence implementation.
- [x] Add idempotency guard for duplicate inference submissions if required.

### 1.4 TriggerBackendAction dispatch
- [x] Introduce action map: `request.Action` -> MediatR command.
- [x] Validate unknown action paths return deterministic errors.

## Workstream 2 — MobileMatchGrpcService

### 2.1 Answer evaluation integration
- [x] Wire answer evaluation through mediator/match engine.
- Populate correctness/points/running score from real domain logic.

### 2.2 Opponent score propagation
- [x] Replace placeholder opponent score with live score state.
- [x] Add stream consistency tests for concurrent participants.

### 2.3 Leaderboard streaming source
- [x] Replace placeholder leaderboard snapshots with leaderboard service query.
- Keep polling initially; later replace with pub/sub subscription path.

## Definition of Done
- TODO comments removed for covered paths.
- Integration tests added for all gRPC methods touched (execution pending environment/tool availability).
- Performance sanity check: streaming methods validated with cancellation + bounded resource use.

## Immediate Progress (this branch)
- ✅ `SidecarGrpcService` now persists supported `question_answered` analytics events through `IAnalyticsEventWriter` instead of placeholder logging-only flow.
- ✅ `SidecarGrpcService` now stores inference results through `ISidecarInferenceStore` (in-memory implementation) instead of placeholder record IDs.
- ✅ `SidecarGrpcService` now supports deterministic backend action dispatch for `admin_event_queue_reprocess` via MediatR (`AdminReprocessEventQueue`), with explicit validation for unsupported actions and invalid params payloads.
- ✅ Added `SidecarGrpcServiceTests` coverage for analytics acceptance/rejection, streamed summary counts, inference result storage, and backend action dispatch (pending environment execution).
- ✅ Added stream-cap and cancellation coverage for `SidecarGrpcService.StreamAnalyticsEvents`, and wired a bounded per-stream event cap in service logic.
- ✅ Added in-memory idempotency guard for duplicate inference submissions (same model/entity/score/metadata returns stable record id) with service-level test coverage.
- ✅ Added file-backed durable inference store (`FileSidecarInferenceStore`) with on-start index reload and tests for duplicate payload idempotency across process restarts.
- ✅ `MobileMatchGrpcService` leaderboard stream now uses live MediatR leaderboard queries (`GetMyTier` + `GetTierLeaderboard`) instead of static placeholder snapshots.
- ✅ `MobileMatchGrpcService` answer flow now evaluates correctness against persisted question answer keys and emits live running-score/correct-count updates to participants.
- ✅ Added initial `MatchSession` tests for score progression and fan-out broadcast behavior in streaming sessions.
- ✅ Added concurrent participant score-consistency coverage for `MatchSession.ApplyAnswerResult` under parallel updates.
- ✅ Added `MobileMatchGrpcServiceTests` coverage for streamed answer-result/running-score behavior and live leaderboard update streaming.
- ✅ Added `PlayMatch` action-cap guard (`MaxActionsPerStream`) and test coverage to ensure long-lived streams remain bounded.
- ✅ Added configurable + clamped leaderboard polling interval (`MOBILE_MATCH_LEADERBOARD_POLL_SECONDS`, 1-60s) with test coverage for defaults/range handling.
