---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Command: Implement Endpoint

Use this command when adding or modifying a backend API endpoint.

## Instructions

1. Read nearby route group and conventions.
2. Identify contract and auth requirement.
3. Keep endpoint thin.
4. Put use-case logic in Application layer.
5. Add validation.
6. Add or update tests.
7. Update docs/OpenAPI if relevant.
8. Run build/tests or state exact command if not run.

## Output

```md
# Endpoint Implementation Plan

## Endpoint
Method:
Route:
Auth:

## Contract
Request:
Response:
Errors:

## Code Changes
-

## Tests
-

## Verification Commands
```bash
dotnet build
dotnet test
```
```
