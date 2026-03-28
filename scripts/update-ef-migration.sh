#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT_ROOT"

PROJECT="Tycoon.Backend.Migrations/Tycoon.Backend.Migrations.csproj"
STARTUP_PROJECT="Tycoon.Backend.Api/Tycoon.Backend.Api.csproj"
CONTEXT="AppDb"
OUTPUT_DIR="Migrations"
MIGRATION_NAME=""
REMOVE_LAST=false
APPLY_DATABASE=false
NO_BUILD=false

usage() {
  cat <<USAGE
Usage: ./scripts/update-ef-migration.sh --name <MigrationName> [options]

Creates a new EF migration for AppDb, with optional "remove last then re-add" flow.

Options:
  --name <MigrationName>   Required migration name to add.
  --remove-last            Remove the last migration first (uses '--force').
  --apply                  Run 'dotnet ef database update' after migration add.
  --no-build               Pass '--no-build' to ef commands.
  -h, --help               Show this help text.
USAGE
}

log() {
  echo "[update-ef-migration] $*"
}

require_dotnet() {
  if ! command -v dotnet >/dev/null 2>&1; then
    echo "dotnet CLI is required but not found in PATH." >&2
    echo "Tip: run 'bash scripts/setup-health-pass-prereqs.sh' first." >&2
    exit 1
  fi
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --name)
      [[ $# -ge 2 ]] || { echo "Missing value for --name" >&2; exit 1; }
      MIGRATION_NAME="$2"
      shift 2
      ;;
    --remove-last)
      REMOVE_LAST=true
      shift
      ;;
    --apply)
      APPLY_DATABASE=true
      shift
      ;;
    --no-build)
      NO_BUILD=true
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

if [[ -z "$MIGRATION_NAME" ]]; then
  echo "--name is required." >&2
  usage >&2
  exit 1
fi

EF_ARGS=(
  --project "$PROJECT"
  --startup-project "$STARTUP_PROJECT"
  --context "$CONTEXT"
)

if $NO_BUILD; then
  EF_ARGS+=(--no-build)
fi

require_dotnet

if $REMOVE_LAST; then
  log "Removing last migration (force)..."
  dotnet ef migrations remove --force "${EF_ARGS[@]}"
fi

log "Adding migration '$MIGRATION_NAME'..."
dotnet ef migrations add "$MIGRATION_NAME" "${EF_ARGS[@]}" --output-dir "$OUTPUT_DIR"

if [[ -x "./scripts/validate-ef-schema.sh" ]]; then
  log "Running EF schema validation..."
  ./scripts/validate-ef-schema.sh
fi

if $APPLY_DATABASE; then
  log "Updating database to latest migration..."
  dotnet ef database update "${EF_ARGS[@]}"
fi

log "Done."
log "Migration created under Tycoon.Backend.Migrations/$OUTPUT_DIR"
