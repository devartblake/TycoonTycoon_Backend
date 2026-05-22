# Codex Heartbeat Policy

## Required use

Codex must use the heartbeat workflow for:

- Alpha/Beta release tasks
- Docker/environment changes
- EF Core/PostgreSQL migrations
- wallet/economy mutations
- auth/security/KMS work
- contract tests
- CI fixes
- multi-file refactors
- tasks with unclear scope

## Required updates

Always update:

- `.codex/heartbeat/current-task.md`
- `.codex/heartbeat/verification-log.md`

When applicable, update:

- `.codex/heartbeat/alpha-status.md`
- `.codex/heartbeat/current-blockers.md`
- `.codex/heartbeat/deferred-post-alpha.md`

## Completion requirement

A Codex task cannot be marked complete unless:

1. status is final,
2. files changed are listed,
3. verification is recorded,
4. blockers are resolved or documented,
5. Alpha/Beta impact is stated.

## Failure behavior

If verification cannot run, Codex must document:

- skipped command,
- reason it could not run,
- what the user should run locally,
- expected result.
