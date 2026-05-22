# Codex Heartbeat Protocol

## When to use

Codex must use heartbeat updates for:

- Alpha/Beta release tasks
- Docker and compose changes
- EF Core migrations
- wallet/economy changes
- auth/security/KMS work
- contract tests
- CI fixes
- multi-file refactors
- unclear or long-running tasks

## Required checkpoints

1. Task start
2. Before file changes
3. After meaningful file changes
4. Before verification
5. After verification
6. When blocked
7. Task completion

## Status values

Use only:

- `not-started`
- `in-progress`
- `blocked`
- `needs-review`
- `verified`
- `failed`
- `deferred`
- `complete`

## Priority values

Use only:

- `P0 Alpha blocker`
- `P1 Alpha important`
- `P2 Post-Alpha`
- `P3 Long-term platform`

## Completion rule

A task is not complete until heartbeat status, changed files, verification, blockers, Alpha impact, and next action are documented.
