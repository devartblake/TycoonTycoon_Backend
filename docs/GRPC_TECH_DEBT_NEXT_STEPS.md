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
- [ ] Add backpressure/size guard + cancellation handling tests.

### 1.3 SubmitInferenceResult persistence
- [x] Persist model/entity/score payload via repository abstraction.
- [ ] Replace in-memory store with durable persistence implementation.
- [ ] Add idempotency guard for duplicate inference submissions if required.

### 1.4 TriggerBackendAction dispatch
- [x] Introduce action map: `request.Action` -> MediatR command.
- [x] Validate unknown action paths return deterministic errors.

## Workstream 2 — MobileMatchGrpcService

### 2.1 Answer evaluation integration
- Wire answer evaluation through mediator/match engine.
- Populate correctness/points/running score from real domain logic.

### 2.2 Opponent score propagation
- Replace placeholder opponent score with live score state.
- Add stream consistency tests for concurrent participants.

### 2.3 Leaderboard streaming source
- Replace placeholder leaderboard snapshots with leaderboard service query.
- Keep polling initially; later replace with pub/sub subscription path.

## Definition of Done
- TODO comments removed for covered paths.
- Integration tests added for all gRPC methods touched.
- Performance sanity check: streaming methods validated with cancellation + bounded resource use.

## Immediate Progress (this branch)
- ✅ `SidecarGrpcService` now persists supported `question_answered` analytics events through `IAnalyticsEventWriter` instead of placeholder logging-only flow.
- ✅ `SidecarGrpcService` now stores inference results through `ISidecarInferenceStore` (in-memory implementation) instead of placeholder record IDs.
- ✅ `SidecarGrpcService` now supports deterministic backend action dispatch for `admin_event_queue_reprocess` via MediatR (`AdminReprocessEventQueue`), with explicit validation for unsupported actions and invalid params payloads.
