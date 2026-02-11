# Backend migration analysis for `Tycoon.MigrationService` / `Tycoon.Backend.Migrations`

## Scope and repository reality check

I inspected this repository for any .NET backend sources related to `Tycoon.MigrationService` and `Tycoon.Backend.Migrations`.

### What is present
- Flutter application code and assets.
- Dart/Flutter dependency configuration in `pubspec.yaml`.

### What is missing
- No `*.sln` files.
- No `*.csproj` files.
- No C# source files for `Tycoon.MigrationService` or `Tycoon.Backend.Migrations`.

Because of that, this repository **cannot directly be used to fix backend migration code** for the two services mentioned in the logs. The failure is likely occurring in a different backend repository or in a backend submodule/image source that is not present here.

## Interpreting the provided error log

Based on the log sequence, there are two distinct issues:

1. **Native dependency warning/error at startup**
   - `Cannot load library libgssapi_krb5.so.2`
   - This typically indicates the runtime image is missing Kerberos/GSSAPI system libs required by parts of Npgsql authentication stack.

2. **Schema mismatch during seeding**
   - EF reports: `Detected 1 migrations in assembly Tycoon.Backend.Migrations.`
   - EF then reports database is up-to-date (`No migrations were applied.`).
   - Seeder immediately fails querying `Tiers` with Postgres error `42P01: relation "Tiers" does not exist`.

This means the migration state and actual schema are inconsistent.

## Most likely root causes for `relation "Tiers" does not exist`

1. `__EFMigrationsHistory` contains a migration id that EF trusts, but the underlying tables were never created (or were dropped later).
2. The single migration in `Tycoon.Backend.Migrations` does not actually create `Tiers` (bad/partial initial migration).
3. `Tycoon.MigrationService` is connected to a different database/schema than expected (wrong connection string, `search_path`, or environment config).
4. Seeding assumes default schema/table naming while migrations created objects under another schema (for example `public` vs custom schema).

## Recommended investigation sequence (backend repo/container)

1. Verify you are targeting the expected DB in the migration service container:
   - Check effective connection string at runtime.
   - Confirm host/database/user values.

2. Inspect migration history and physical tables:

   ```sql
   SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
   SELECT table_schema, table_name
   FROM information_schema.tables
   WHERE table_name ILIKE 'tiers';
   ```

3. Generate SQL from the migration assembly and confirm `CREATE TABLE` for tiers exists:
   - `dotnet ef migrations script --project Tycoon.Backend.Migrations --startup-project Tycoon.MigrationService`

4. If history is ahead of schema (common in reset/reseed scenarios), reconcile by:
   - Dropping/recreating DB for non-production environments, or
   - Carefully resetting `__EFMigrationsHistory` and reapplying migrations.

5. Install missing native lib in container image for Npgsql Kerberos dependency:
   - Debian/Ubuntu family: `libgssapi-krb5-2` (and typically `libkrb5-3`)
   - Alpine family: matching `krb5-libs` package.

## Concrete hardening suggestions for migration service

1. Add a startup guard before seeding:
   - Ensure critical tables (`Tiers`, `Missions`) exist after `MigrateAsync()`.
   - If missing, log a high-signal fatal error explaining migration/schema mismatch.

2. Make migration count sanity checks stricter:
   - If migration count > 0 but no required tables exist, fail fast with explicit diagnostic guidance.

3. Improve operational diagnostics:
   - Log resolved DB name/schema/search_path at startup.
   - Log the current migration ids read from `__EFMigrationsHistory`.

4. CI gate:
   - Run ephemeral Postgres integration test that performs `MigrateAsync()` + seeding query for `Tiers`.

## Conclusion

The immediate blocker in your log is not in this Flutter repository; it is a backend migration/schema-state issue in the missing `.NET` migration projects or container image configuration. To resolve quickly:

1. Fix image dependency for `libgssapi_krb5.so.2`.
2. Reconcile migration history with actual schema.
3. Confirm the migration assembly truly creates `Tiers` in the schema queried by the seeder.
