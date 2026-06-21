# Backend EF Migration Reference — `Synaptix.Backend.Migrations` / `Synaptix.MigrationService`

## Project structure

| Project | Path | Role |
|---|---|---|
| `Synaptix.Backend.Migrations` | `Synaptix.Backend.Migrations/` | Migration assembly — contains all `Migration` subclasses and `AppDbModelSnapshot` |
| `Synaptix.MigrationService` | `Synaptix.MigrationService/` | Startup project — runs migrations and seeds at container startup |
| `Synaptix.Backend.Infrastructure` | `Synaptix.Backend.Infrastructure/` | Defines `AppDb : DbContext` and entity configurations |
| `Synaptix.Backend.Domain` | `Synaptix.Backend.Domain/` | Entity classes (source of truth for schema shape) |

The solution file is `TycoonTycoon_Backend.slnx` at the repo root.

---

## Adding a new EF migration

Run from the **repo root**. The `--project` flag targets the migration assembly; `--startup-project` targets the service that wires up DI (needed to resolve `AppDb`).

```powershell
dotnet ef migrations add <PascalCaseName> `
  --project Synaptix.Backend.Migrations `
  --startup-project Synaptix.MigrationService `
  --context AppDb
```

Migrations are timestamped automatically: `YYYYMMDDHHmmss_PascalCaseName.cs`.

`dotnet ef` will generate **two** files per migration:

- `Synaptix.Backend.Migrations/Migrations/<timestamp>_<Name>.cs` — the `Up()`/`Down()` implementation
- `Synaptix.Backend.Migrations/Migrations/<timestamp>_<Name>.Designer.cs` — model snapshot at this point (contains `[Migration("...")]` attribute)

**Both files must be committed.**

---

## Critical: the `[Migration]` attribute

EF Core's `IMigrationsAssembly` discovers migrations by scanning for types that:
1. Inherit from `Migration`
2. Are annotated with `[Migration("timestamp_Name")]`

When `dotnet ef migrations add` generates a migration, the `[Migration]` attribute lands in the `.Designer.cs` file on the `partial class`. If a migration is written by hand **without** a `.Designer.cs` file, the attribute must be placed directly in the main `.cs` file, otherwise EF Core silently skips the migration — it compiles fine but is never applied.

### Symptom

```
[INF] Detected 27 migrations in assembly Synaptix.Backend.Migrations.
[INF] No migrations were applied. The database is already up to date.
[ERR] column s.age_min does not exist   ← column from an unapplied migration
```

The assembly has N migration files but EF only detects a smaller number, and the database schema is missing columns/tables from the "invisible" migrations.

### Fix for hand-crafted migrations without a Designer file

Add the attribute directly to the migration class:

```csharp
namespace Synaptix.Backend.Migrations.Migrations
{
    [Migration("20260616000000_AddCompliancePhase1")]   // ← required
    public partial class AddCompliancePhase1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) { ... }
        protected override void Down(MigrationBuilder migrationBuilder) { ... }
    }
}
```

The timestamp string must exactly match the file name prefix.

### Migrations fixed in June 2026 (were missing `[Migration]` attribute)

| File | Attribute added |
|---|---|
| `20260522100000_AddRewardReactor.cs` | `[Migration("20260522100000_AddRewardReactor")]` |
| `20260604223000_AddSetupReports.cs` | `[Migration("20260604223000_AddSetupReports")]` |
| `20260605233000_AddQuestionTaxonomy.cs` | `[Migration("20260605233000_AddQuestionTaxonomy")]` |
| `20260605234500_AddQuestionTaxonomySuggestions.cs` | `[Migration("20260605234500_AddQuestionTaxonomySuggestions")]` |
| `20260616000000_AddCompliancePhase1.cs` | `[Migration("20260616000000_AddCompliancePhase1")]` |
| `20260618000000_AddPlayerEntitlements.cs` | `[Migration("20260618000000_AddPlayerEntitlements")]` |

---

## Other useful CLI commands

**Preview the SQL that would be applied (dry run):**
```powershell
dotnet ef migrations script `
  --project Synaptix.Backend.Migrations `
  --startup-project Synaptix.MigrationService `
  --context AppDb `
  --idempotent
```

**Apply migrations directly to a local database (outside Docker):**
```powershell
dotnet ef database update `
  --project Synaptix.Backend.Migrations `
  --startup-project Synaptix.MigrationService `
  --context AppDb
```

**List all migrations and their applied status:**
```powershell
dotnet ef migrations list `
  --project Synaptix.Backend.Migrations `
  --startup-project Synaptix.MigrationService `
  --context AppDb
```

**Remove the last unapplied migration (before committing):**
```powershell
dotnet ef migrations remove `
  --project Synaptix.Backend.Migrations `
  --startup-project Synaptix.MigrationService `
  --context AppDb
```

---

## Docker workflow

Migrations run automatically inside the `migration` container on `docker compose up`. The startup sequence enforces ordering:

```
mongodb healthy  ─┐
postgres healthy  ─┤→ setup (exit 0) → migration (exit 0) → backend-api (healthy)
redis healthy     ─┘
```

Check migration output:
```powershell
docker logs synaptix_migration
```

A successful run ends with:
```
[INF] Detected 33 migrations in assembly Synaptix.Backend.Migrations.
[INF] No migrations were applied. The database is already up to date.   # OR: Applied N migration(s)
[INF] Seeding completed successfully
[INF] Seeding catalog data from MinIO (idempotent)…
[INF] Application is shutting down...
```

---

## Diagnosing schema/migration mismatches

**Check applied migrations directly in Postgres:**
```sql
SELECT migration_id, product_version
FROM "__EFMigrationsHistory"
ORDER BY migration_id;
```

**Check if a specific column exists:**
```sql
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'store_items'
ORDER BY ordinal_position;
```

**Check if a table exists:**
```sql
SELECT table_schema, table_name
FROM information_schema.tables
WHERE table_name ILIKE 'parental_purchase_controls';
```

Run via:
```powershell
docker exec -it synaptix_postgres psql -U <POSTGRES_USER> -d <POSTGRES_DB>
```

---

## Resetting the development database

If the schema and migration history are out of sync (e.g., after credential rotation or a volume created by an older build), wipe all named volumes and re-run:

```powershell
docker compose --env-file docker/.env -f docker/compose.yml -f docker/compose.dev.yml down --volumes
docker compose --env-file docker/.env -f docker/compose.yml -f docker/compose.dev.yml up -d --build
```

`down --volumes` removes `postgres_data`, `mongodb_data`, `redis_data`, and all other named volumes. Postgres and MongoDB will reinitialize from scratch using the current env file credentials. The setup container provisions users and the migration container applies all migrations in order.

---

## Migration naming convention

```
<YYYYMMDDHHmmss>_<PascalCaseName>.cs
```

- Timestamp uses UTC, zero-padded to 14 digits
- Name describes what the migration adds/changes, not what exists after it
- Examples: `AddCompliancePhase1`, `AddPlayerEntitlements`, `AddStoreItemAvatarFields`

Avoid names like `UpdateSchema` or `Fix` — they make `migrations list` output unreadable and `git blame` useless.
