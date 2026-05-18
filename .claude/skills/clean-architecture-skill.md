---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Clean Architecture Skill

Use this skill when adding or modifying backend features.

## Layer Rules

### API Layer

- Routing.
- HTTP concerns.
- Authentication/authorization.
- Request/response mapping.
- No core business rules.

### Application Layer

- Use cases.
- Commands/queries.
- Validators.
- orchestration.
- transaction boundaries when applicable.

### Domain Layer

- Core entities.
- value objects.
- domain rules.
- invariants.

### Infrastructure Layer

- EF Core.
- external clients.
- MinIO.
- Redis.
- RabbitMQ.
- sidecar clients.
- email/payment providers.

### Shared Contracts

- Stable cross-service contracts.
- DTOs used by multiple bounded contexts.
- versionable external shapes.

## Decision Procedure

1. Identify the behavior being changed.
2. Decide whether it is HTTP, use-case, domain, persistence, or external integration.
3. Place code in the narrowest correct layer.
4. Add tests closest to the behavior.

## Anti-Patterns

- Route handlers with business logic.
- Domain objects depending on EF Core or HTTP.
- Infrastructure types leaking into API contracts.
- Generic abstractions without repeated usage.
- broad refactors during Alpha.
