# gRPC Technical Debt — Next Steps (Sequential)

This document operationalizes the next steps after dashboard-target alignment.

## Scope Sources
- `Tycoon.Backend.Api/Grpc/SidecarGrpcService.cs`
- `Tycoon.Backend.Api/Grpc/MobileMatchGrpcService.cs`

## Workstream 1 — SidecarGrpcService

### 1.1 SubmitAnalyticsEvent wiring
- Replace placeholder accept/echo path with application command/service dispatch.
- Add structured error responses and logging dimensions.

### 1.2 StreamAnalyticsEvents batching
- Implement batch write path (service/repository abstraction).
- Add backpressure/size guard + cancellation handling tests.

### 1.3 SubmitInferenceResult persistence
- Persist model/entity/score payload via repository abstraction.
- Add idempotency guard for duplicate inference submissions if required.

### 1.4 TriggerBackendAction dispatch
- Introduce action map: `request.Action` -> MediatR command.
- Validate unknown action paths return deterministic errors.

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
