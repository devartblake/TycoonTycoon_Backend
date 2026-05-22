---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Contract Testing Skill

Use this skill when adding or modifying API contracts consumed by Flutter or another service.

## Contract Test Goals

- Verify route exists.
- Verify auth behavior.
- Verify request schema.
- Verify response schema.
- Verify error behavior.
- Verify idempotency when relevant.
- Verify feature flag behavior when relevant.

## High-Priority Contracts

- auth/session endpoints.
- `/users/me/wallet`.
- match submission.
- reward claim.
- store catalog.
- missions.
- skill tree read/unlock.
- admin protected endpoints.
- sidecar fallback clients.

## Test Design

Tests should fail if the contract breaks even when implementation still compiles.

## Output

```md
## Contract
Endpoint:
Consumer:
Risk:

## Test Cases
-
```
