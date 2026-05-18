---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Minimal API Skill

Use this skill when creating or modifying ASP.NET Core Minimal API endpoints.

## Endpoint Pattern

1. Locate existing route group.
2. Follow existing naming and authorization conventions.
3. Define request/response DTOs.
4. Validate request.
5. Call Application service/handler.
6. Return explicit status codes.
7. Add or update OpenAPI metadata where conventions exist.
8. Add contract/integration tests.

## Endpoint Checklist

- Route path matches current API conventions.
- Auth policy is correct.
- Admin endpoints are protected.
- Request body is validated.
- Response shape is stable.
- Errors are explicit.
- No business rules are buried in route delegate.
- Tests cover success and important failure cases.

## Alpha Bias

For Alpha, prefer fewer stable endpoints over many partially implemented endpoints.
