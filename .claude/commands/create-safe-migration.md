---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Command: Create Safe Migration

Use this command when schema or seed changes are required.

## Instructions

1. Read entity/configuration changes.
2. Determine whether migration is additive or destructive.
3. Prefer additive changes for Alpha.
4. Create migration with descriptive name.
5. Review generated SQL.
6. Confirm MigrationService compatibility.
7. Add test/smoke check if the migration is release critical.

## Output

```md
# Migration Plan

## Purpose
-

## Tables/Entities
-

## Migration Name
-

## Destructive?
Yes/No

## Backfill Required?
Yes/No

## Commands
```bash
dotnet ef migrations add <Name>
dotnet test
```

## Verification
-
```
