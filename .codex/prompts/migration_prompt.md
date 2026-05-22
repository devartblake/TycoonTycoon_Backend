# Migration Prompt

Create or adjust an EF Core/PostgreSQL migration.

## Prompt Template

Task:
`<describe schema change>`

Constraints:
- Prefer additive migration.
- No destructive change unless explicitly stated.
- Keep seed behavior idempotent.
- Verify with migration script or tests.
- Include rollback considerations.

Required output:
- Migration name.
- Tables/columns/indexes changed.
- Compatibility risk.
- Verification results.
