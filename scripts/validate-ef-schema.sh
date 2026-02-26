#!/usr/bin/env bash
set -euo pipefail

PROJECT="Tycoon.Backend.Migrations/Tycoon.Backend.Migrations.csproj"
STARTUP="Tycoon.Backend.Api/Tycoon.Backend.Api.csproj"
CONTEXT="AppDb"
TMP_NAME="__SchemaCheck$(date +%s)"
TMP_DIR="Tycoon.Backend.Migrations/Migrations/__SchemaCheck"

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
  echo "Please add/update migrations in Tycoon.Backend.Migrations and commit them."
  exit 1
fi

echo "✅ EF schema validation passed (no pending model changes)."
