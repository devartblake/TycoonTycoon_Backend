# Architecture Rules

## Repo architecture

Respect the current .NET Clean Architecture layout:

- `Tycoon.Backend.Api`
- `Tycoon.Backend.Application`
- `Tycoon.Backend.Domain`
- `Tycoon.Backend.Infrastructure`
- `Tycoon.Backend.Migrations`
- `Tycoon.MigrationService`
- `Synaptix.Security.Kms.*`
- `Tycoon.Shared.*`
- test projects

## Rules

- Domain entities and invariants belong in Domain.
- Use Application for use cases, commands, queries, validators, and orchestration.
- Use Infrastructure for persistence, external integrations, queues, object storage, and sidecar clients.
- Use Api for endpoints, auth policies, request/response binding, and endpoint filters.
- Keep shared contracts stable and version-aware.
- Do not move code across layers without checking references and tests.
- Do not add circular dependencies.
- Favor explicit dependencies and typed options over service locator patterns.
