# Reward Reactor Frontend Handoff for trivia_tycoon

Date: 2026-05-22
Audience: trivia_tycoon frontend team
Status: Backend complete, frontend integration unblocked

## Purpose

This handoff confirms that Reward Reactor backend scope from the backend implementation plan is complete and validated, so the frontend team in trivia_tycoon can proceed with implementation.

## Backend Completion Summary

The following backend capabilities are implemented and contract-documented:

- Reactor spin flow with server-authoritative outcome and claim token.
- Reactor claim flow with idempotency and chain ticket generation.
- Reactor chain activation endpoint with idempotent replay and expiry handling.
- Mission reward mechanism wiring with reactor payload support.
- Runtime event multiplier application and season key propagation.
- Reactor config endpoint for frontend asset switching.
- Arcade spin start and claim migration support using spinId plus claimToken.

Primary plan document:

- docs/backend/reward_reactor_backend_implementation_plan_2026-05-22.md

## Integration-Ready Endpoints

- POST /arcade/reactor/spin
- POST /arcade/reactor/claim
- POST /arcade/reactor/chain
- GET /arcade/reactor/config
- GET /events/active
- POST /arcade/spin/start
- POST /arcade/spin/claim

## Contract/Test Traceability

Contract examples and test mapping are fully documented in:

- docs/backend/reward_reactor_backend_implementation_plan_2026-05-22.md

The QA checklist there is fully checked, including:

- Reactor claim expired path coverage.
- Reactor chain missing ticket path coverage.

Validation result snapshot:

- RewardReactor test suite passed: 24 total, 24 passed, 0 failed.

## Frontend Work You Can Start Now in trivia_tycoon

Recommended implementation order:

1. Reactor chain state machine and chain banner UX.
2. Mission reactor overlay using rewardMechanismId and reactorSpinPayload.
3. Event badge and multiplier display using spin payload plus events endpoint.
4. Seasonal asset switching from reactor config endpoint.
5. Spin and Earn claim migration to spinId plus claimToken while keeping rollout compatibility.

## Notes for Frontend QA

- Treat backend as the only reward authority.
- Use event and season fields for display only.
- Do not calculate reward amounts or multipliers client-side.
- Treat duplicate claim responses as successful idempotent replays.

## Handoff Decision

Reward Reactor backend is complete for planned beta scope. Frontend implementation in trivia_tycoon can proceed immediately.