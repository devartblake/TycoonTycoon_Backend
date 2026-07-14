#!/usr/bin/env bash
###############################################################################
# migrate.sh — Migration automation for Synaptix Backend
#
# Usage:
#   ./scripts/migrate.sh                  # Run migrations (Docker)
#   ./scripts/migrate.sh --reset          # Drop DB, then run migrations
#   ./scripts/migrate.sh --status         # Show applied migrations
#   ./scripts/migrate.sh --local          # Run locally (dotnet) instead of Docker
#   ./scripts/migrate.sh --local --reset  # Combine flags
###############################################################################
set -euo pipefail
 
COMPOSE_FILE="docker/compose.yml"
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_ROOT"
 
# ── Defaults ────────────────────────────────────────────────────────────────
MODE="migrate"
USE_DOCKER=true
RESET=false
VERBOSE=false
 
# ── Parse args ──────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --reset)    RESET=true; shift ;;
    --status)   MODE="status"; shift ;;
    --local)    USE_DOCKER=false; shift ;;
    --verbose)  VERBOSE=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--reset] [--status] [--local] [--verbose]"
      echo ""
      echo "  --reset    Drop and recreate the database before migrating"
      echo "  --status   Show applied migration history (no changes)"
      echo "  --local    Use dotnet CLI instead of Docker"
      echo "  --verbose  Show extra diagnostic output"
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done
 
# ── Helpers ─────────────────────────────────────────────────────────────────
log()  { echo "[migrate] $*"; }
fail() { echo "[migrate] ERROR: $*" >&2; exit 1; }
 
wait_for_postgres() {
  local host="${PGHOST:-localhost}"
  local port="${PGPORT:-5432}"
  local max_wait=30
  local waited=0
 
  log "Waiting for PostgreSQL at $host:$port..."
  while ! pg_isready -h "$host" -p "$port" -q 2>/dev/null; do
    waited=$((waited + 1))
    if [[ $waited -ge $max_wait ]]; then
      fail "PostgreSQL not reachable at $host:$port after ${max_wait}s"
    fi
    sleep 1
  done
  log "PostgreSQL is ready."
}
 
# ── Status mode ─────────────────────────────────────────────────────────────
run_status() {
  if $USE_DOCKER; then
    log "Querying migration history via Docker..."
    docker compose -f "$COMPOSE_FILE" run --rm -e MIGRATION_MODE=Migrate migration \
      dotnet ef migrations list \
        --project Synaptix.Backend.Migrations/Synaptix.Backend.Migrations.csproj \
        --startup-project Synaptix.MigrationService/Synaptix.MigrationService.csproj \
        --context AppDb 2>/dev/null || {
      # Fallback: query __EFMigrationsHistory directly
      log "Falling back to direct DB query..."
      docker compose -f "$COMPOSE_FILE" exec -T postgres \
        psql -U postgres -d TycoonDb -c \
        "SELECT \"MigrationId\", \"ProductVersion\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";" 2>/dev/null || \
        fail "Could not retrieve migration status. Is the database running?"
    }
  else
    wait_for_postgres
    dotnet ef migrations list \
      --project Synaptix.Backend.Migrations/Synaptix.Backend.Migrations.csproj \
      --startup-project Synaptix.MigrationService/Synaptix.MigrationService.csproj \
      --context AppDb
  fi
}
 
# ── Migrate mode ────────────────────────────────────────────────────────────
run_migrate() {
  local env_vars=()
 
  if $RESET; then
    log "Reset mode enabled — database will be dropped and recreated."
    env_vars+=(-e "MIGRATION_RESET_DATABASE=true")
  fi
 
  if $USE_DOCKER; then
    log "Building migration service..."
    docker compose -f "$COMPOSE_FILE" build migration
 
    log "Starting PostgreSQL (if not running)..."
    docker compose -f "$COMPOSE_FILE" up -d postgres
    sleep 2  # Brief wait for postgres to accept connections
 
    log "Running migrations via Docker..."
    docker compose -f "$COMPOSE_FILE" run --rm "${env_vars[@]}" migration
 
    log "Verifying schema..."
    verify_schema_docker
  else
    wait_for_postgres
 
    if $RESET; then
      export MIGRATION_RESET_DATABASE=true
    fi
 
    export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Local}"
    log "Running migrations locally..."
    dotnet run --project Synaptix.MigrationService/Synaptix.MigrationService.csproj
 
    log "Verifying schema..."
    verify_schema_local
  fi
 
  log "Migration completed successfully."
}
 
# ── Schema verification ─────────────────────────────────────────────────────
verify_schema_docker() {
  local tables
  tables=$(docker compose -f "$COMPOSE_FILE" exec -T postgres \
    psql -U postgres -d TycoonDb -tAc \
    "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('users', 'missions', 'tiers', 'refresh_tokens');" 2>/dev/null) || return 0
 
  if [[ "$tables" -ge 4 ]]; then
    log "Schema verification passed ($tables/4 critical tables found)."
  else
    log "WARNING: Only $tables/4 critical tables found. Check migration logs."
  fi
}
 
verify_schema_local() {
  if command -v psql &>/dev/null; then
    local tables
    tables=$(psql -tAc \
      "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('users', 'missions', 'tiers', 'refresh_tokens');" 2>/dev/null) || return 0
 
    if [[ "$tables" -ge 4 ]]; then
      log "Schema verification passed ($tables/4 critical tables found)."
    else
      log "WARNING: Only $tables/4 critical tables found. Check migration logs."
    fi
  fi
}
 
# ── Main ────────────────────────────────────────────────────────────────────
case "$MODE" in
  status)  run_status ;;
  migrate) run_migrate ;;
esac
