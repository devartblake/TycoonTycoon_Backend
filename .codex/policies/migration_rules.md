# Migration Rules

## Migration safety

- No destructive migration without explicit task instruction.
- Prefer additive schema changes for Alpha.
- Make seed operations idempotent.
- Keep EF Core migrations aligned with PostgreSQL runtime behavior.
- Verify migration service startup behavior through local Docker or tests.
- Never hardcode environment-specific paths, bucket names, or secrets.
- Include rollback notes for risky changes.

## Required verification

For migration tasks, Codex must attempt at least one of:

- `dotnet test` for migration tests.
- `dotnet ef migrations script`.
- MigrationService test project.
- Local Docker migration startup check.

If verification cannot run, state exactly why.
