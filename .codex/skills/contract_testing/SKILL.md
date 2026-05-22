# Skill: Contract Testing

Use this skill for API contract tests and client/backend alignment.

## Procedure

1. Identify contract owner: backend, frontend, sidecar, or admin dashboard.
2. Define request body, response body, status codes, auth requirement, and idempotency behavior.
3. Add tests that fail on contract drift.
4. Use WebApplicationFactory, Testcontainers, or WireMock as appropriate.
5. Keep test data deterministic.
6. Run relevant test project.

## Priority endpoints

- auth/session identity
- `/users/me/wallet`
- match submission
- store catalog
- reward claims
- skill/mission progress
- admin moderation
