---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# EF Core PostgreSQL Migration Skill

Use this skill when changing persistence models or seed behavior.

## Rules

- Prefer additive migrations before Alpha.
- Do not drop columns/tables without explicit approval.
- Use descriptive migration names.
- Keep unrelated changes separate.
- Confirm model snapshot updates.
- Add indexes intentionally.
- Make seed operations idempotent.

## Procedure

1. Read entity and DbContext configuration.
2. Determine schema delta.
3. Generate or write migration.
4. Review generated SQL.
5. Check destructive operations.
6. Run migration locally.
7. Add test or smoke validation.

## Verification Commands

```bash
dotnet build
dotnet test
dotnet ef migrations list
```

Use project-specific migration commands if the repo defines wrappers/scripts.
