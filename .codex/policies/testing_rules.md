# Testing Rules

## Test expectations

Codex should write tests when changing:

- endpoint behavior
- economy/wallet mutation
- match submission
- reward claims
- migrations
- auth/security behavior
- sidecar fallback behavior
- feature flags

## Preferred test layers

1. Unit tests for domain and application logic.
2. Contract/integration tests for API endpoints.
3. Testcontainers for database-backed behavior when feasible.
4. WireMock for external HTTP clients.
5. Smoke scripts for Docker-based Alpha verification.

Do not mark a task complete unless verification has been attempted or the reason it could not run is documented.
