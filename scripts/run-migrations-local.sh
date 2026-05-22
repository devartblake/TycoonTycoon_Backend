#!/usr/bin/env bash
set -euo pipefail

if [[ -f docker/.env ]]; then
  set -a
  # shellcheck disable=SC1091
  source docker/.env
  set +a
fi

export ASPNETCORE_ENVIRONMENT=Local
export MIGRATION_MODE="${MIGRATION_MODE:-MigrateAndSeed}"
export MIGRATION_SEED_SOURCE="${MIGRATION_SEED_SOURCE:-Auto}"
export MIGRATION_DASHBOARD_READINESS_ENABLED="${MIGRATION_DASHBOARD_READINESS_ENABLED:-true}"
export MIGRATION_DASHBOARD_READINESS_STRICT="${MIGRATION_DASHBOARD_READINESS_STRICT:-true}"
export ConnectionStrings__db="${ConnectionStrings__db:-Host=${POSTGRES_HOST:-localhost};Port=${POSTGRES_PORT:-5432};Database=${POSTGRES_DB:-synaptix_db};Username=${POSTGRES_USER:-synaptix_user};Password=${POSTGRES_PASSWORD:-synaptix_password_123}}"
export SuperAdmin__Email="${SuperAdmin__Email:-${SUPER_ADMIN_EMAIL:-}}"
export SuperAdmin__Password="${SuperAdmin__Password:-${SUPER_ADMIN_PASSWORD:-}}"
export SuperAdmin__Handle="${SuperAdmin__Handle:-${SUPER_ADMIN_HANDLE:-superadmin}}"
exec dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
