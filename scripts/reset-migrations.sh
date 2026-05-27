#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_ROOT"

MIGRATIONS_DIR="Synaptix.Backend.Migrations/Migrations"
PROJECT="Synaptix.Backend.Migrations/Synaptix.Backend.Migrations.csproj"
STARTUP_PROJECT="Synaptix.Backend.Api/Synaptix.Backend.Api.csproj"
CONTEXT="AppDb"
MIGRATION_NAME="InitialCreate"
SKIP_ADD=false
FORCE=false

usage() {
  cat <<USAGE
Usage: ./scripts/reset-migrations.sh [options]

Resets Synaptix.Backend.Migrations/Migrations and optionally recreates a fresh baseline migration.

Options:
  --name <MigrationName>  Name for the new baseline migration (default: InitialCreate)
  --skip-add              Only clear migration files; do not run 'dotnet ef migrations add'
  --force                 Skip confirmation prompt
  -h, --help              Show this help text
USAGE
}

log() {
  echo "[reset-migrations] $*"
}

confirm_or_exit() {
  if $FORCE; then
    return 0
  fi

  echo "This will permanently delete all files under '$MIGRATIONS_DIR'."
  read -r -p "Continue? [y/N] " reply
  case "$reply" in
    [yY]|[yY][eE][sS]) ;;
    *) log "Canceled."; exit 0 ;;
  esac
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --name)
      [[ $# -ge 2 ]] || { echo "Missing value for --name" >&2; exit 1; }
      MIGRATION_NAME="$2"
      shift 2
      ;;
    --skip-add)
      SKIP_ADD=true
      shift
      ;;
    --force)
      FORCE=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ ! -d "$MIGRATIONS_DIR" ]]; then
  echo "Migrations directory not found: $MIGRATIONS_DIR" >&2
  exit 1
fi

confirm_or_exit

log "Clearing migration files..."
find "$MIGRATIONS_DIR" -maxdepth 1 -type f -name '*.cs' -delete

if $SKIP_ADD; then
  log "Done. Migration files were cleared and no new migration was created (--skip-add)."
  exit 0
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required but not found in PATH." >&2
  exit 1
fi

log "Creating new baseline migration '$MIGRATION_NAME'..."
dotnet ef migrations add "$MIGRATION_NAME" \
  --project "$PROJECT" \
  --startup-project "$STARTUP_PROJECT" \
  --context "$CONTEXT" \
  --output-dir Migrations

if [[ -x "./scripts/validate-ef-schema.sh" ]]; then
  log "Validating that the new migration set has no pending model changes..."
  ./scripts/validate-ef-schema.sh
fi

log "Done. New baseline migration created in '$MIGRATIONS_DIR'."
