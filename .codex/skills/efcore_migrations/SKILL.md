# Skill: EF Core PostgreSQL Migrations

Use this skill for schema changes, migration scripts, seed data, MigrationService, and DB startup.

## Procedure

1. Inspect entity, DbContext, existing migrations, and tests.
2. Choose additive migration when possible.
3. Keep enum/string conversions explicit.
4. Avoid destructive changes unless explicitly authorized.
5. Generate or update migration.
6. Add/adjust tests for DB behavior.
7. Verify migration script or migration tests.

## Output expectations

Include:
- migration name
- changed tables/columns/indexes
- whether change is backward compatible
- verification command
- rollback considerations
