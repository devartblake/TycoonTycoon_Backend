#!/usr/bin/env bash
set -euo pipefail

PROJECT="Tycoon.Backend.Migrations/Tycoon.Backend.Migrations.csproj"
STARTUP="Tycoon.Backend.Api/Tycoon.Backend.Api.csproj"
CONTEXT="AppDb"
TMP_NAME="__SchemaCheck$(date +%s)"
TMP_DIR="Tycoon.Backend.Migrations/Migrations/__SchemaCheck"
AUTO_FIX=false
AUTO_FIX_NAME=""

usage() {
  cat <<USAGE
Usage: ./scripts/validate-ef-schema.sh [--auto-fix --name <MigrationName>]

Validates that EF model changes are represented in migrations.

Options:
  --auto-fix              If drift is detected, generate a migration automatically.
  --name <MigrationName>  Migration name to use with --auto-fix.
  -h, --help              Show this help text.
USAGE
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --auto-fix)
      AUTO_FIX=true
      shift
      ;;
    --name)
      [[ $# -ge 2 ]] || { echo "Missing value for --name" >&2; exit 1; }
      AUTO_FIX_NAME="$2"
      shift 2
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

cleanup() {
  rm -rf "$TMP_DIR"
}
trap cleanup EXIT

mkdir -p "$TMP_DIR"

echo "Running EF Core schema drift validation..."
set +e
OUTPUT=$(dotnet ef migrations add "$TMP_NAME" \
  --project "$PROJECT" \
  --startup-project "$STARTUP" \
  --context "$CONTEXT" \
  --output-dir "Migrations/__SchemaCheck" \
  --no-build 2>&1)
STATUS=$?
set -e

echo "$OUTPUT"

if [[ $STATUS -ne 0 ]]; then
  if echo "$OUTPUT" | grep -qi "No changes were found"; then
    echo "✅ EF schema validation passed (no pending model changes)."
    exit 0
  fi

  echo "❌ EF schema validation failed due to command error."
  exit $STATUS
fi

if find "$TMP_DIR" -type f | grep -q .; then
  echo "❌ Schema drift detected: model changes are not represented in migrations."
  if $AUTO_FIX; then
    if [[ -z "$AUTO_FIX_NAME" ]]; then
      echo "--auto-fix requires --name <MigrationName>." >&2
      exit 1
    fi
    echo "Attempting auto-fix with migration '$AUTO_FIX_NAME'..."
    ./scripts/update-ef-migration.sh --name "$AUTO_FIX_NAME"
    echo "Re-running schema validation..."
    ./scripts/validate-ef-schema.sh
    exit $?
  fi

  echo "Please add/update migrations in Tycoon.Backend.Migrations and commit them."
  echo "Tip: ./scripts/update-ef-migration.sh --name <MigrationName>"
  echo "Or:  ./scripts/validate-ef-schema.sh --auto-fix --name <MigrationName>"
  exit 1
fi

echo "✅ EF schema validation passed (no pending model changes)."
