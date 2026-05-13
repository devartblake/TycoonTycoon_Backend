#!/usr/bin/env bash
set -euo pipefail

export ASPNETCORE_ENVIRONMENT=Local
export MIGRATION_MODE="${MIGRATION_MODE:-MigrateAndSeed}"
export MIGRATION_SEED_SOURCE="${MIGRATION_SEED_SOURCE:-Auto}"
export MIGRATION_DASHBOARD_READINESS_ENABLED="${MIGRATION_DASHBOARD_READINESS_ENABLED:-true}"
export MIGRATION_DASHBOARD_READINESS_STRICT="${MIGRATION_DASHBOARD_READINESS_STRICT:-true}"
exec dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
