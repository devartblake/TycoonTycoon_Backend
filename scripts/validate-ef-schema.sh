#!/usr/bin/env bash
set -euo pipefail

# Reduce first-time prompts and telemetry noise during CI.
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_ENVIRONMENT=Development
export ASPNETCORE_ENVIRONMENT=Development

PROJECT="Tycoon.Backend.Migrations/Tycoon.Backend.Migrations.csproj"
STARTUP="Tycoon.MigrationService/Tycoon.MigrationService.csproj"
CONTEXT="AppDb"
AUTO_FIX=false
AUTO_FIX_NAME=""
DOTNET_CMD=()
EF_CMD=()

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

resolve_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    DOTNET_CMD=(dotnet)
    return
  fi

  local candidates=(
    "/c/Program Files/dotnet/dotnet.exe"
    "/mnt/c/Program Files/dotnet/dotnet.exe"
    "C:/Program Files/dotnet/dotnet.exe"
  )

  for candidate in "${candidates[@]}"; do
    if [[ -x "$candidate" ]]; then
      DOTNET_CMD=("$candidate")
      return
    fi
  done

  echo "dotnet was not found on PATH, and no Windows dotnet.exe fallback was found." >&2
  echo "Install the .NET SDK or add dotnet to PATH before running EF schema validation." >&2
  exit 127
}

resolve_ef_tool() {
  EF_CMD=("${DOTNET_CMD[@]}" ef)
}

ensure_local_ef_tool() {
  local tool_dir="artifacts/dotnet-tools"
  local tool_exe="$tool_dir/dotnet-ef"

  if [[ "$OSTYPE" == msys* || "$OSTYPE" == cygwin* || -n "${WINDIR:-}" ]]; then
    tool_exe="$tool_exe.exe"
  fi

  mkdir -p "$tool_dir"

  if [[ ! -x "$tool_exe" ]]; then
    echo "dotnet-ef is not available on PATH; installing repo-local tool in $tool_dir..."
    "${DOTNET_CMD[@]}" tool install dotnet-ef --tool-path "$tool_dir"
  fi

  EF_CMD=("$tool_exe")
}

run_schema_validation() {
  "${EF_CMD[@]}" migrations has-pending-model-changes \
    --project "$PROJECT" \
    --startup-project "$STARTUP" \
    --context "$CONTEXT" 2>&1
}

resolve_dotnet
resolve_ef_tool

echo "Running EF Core schema drift validation..."
set +e
OUTPUT=$(run_schema_validation)
STATUS=$?
set -e

if [[ $STATUS -ne 0 ]] && echo "$OUTPUT" | grep -Eqi "dotnet-ef does not exist|specified command or file was not found|could not execute"; then
  echo "$OUTPUT"
  ensure_local_ef_tool
  echo "Re-running EF Core schema drift validation with repo-local dotnet-ef..."
  set +e
  OUTPUT=$(run_schema_validation)
  STATUS=$?
  set -e
fi

echo "$OUTPUT"

if [[ $STATUS -eq 0 ]]; then
  echo "EF schema validation passed (no pending model changes)."
  exit 0
fi

if ! echo "$OUTPUT" | grep -qi "Changes have been made to the model"; then
  echo "EF schema validation failed due to command error."
  exit $STATUS
fi

echo "Schema drift detected: model changes are not represented in migrations."
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
