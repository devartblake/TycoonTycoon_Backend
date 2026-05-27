#!/usr/bin/env bash
set -euo pipefail

# Executes the NOW gates in order.
# This script is intended for .NET-capable runners/workstations.

RUN_MIGRATIONS="${RUN_MIGRATIONS:-false}"
RUN_LIVE_SMOKE="${RUN_LIVE_SMOKE:-false}"
EXPECT_IAP_STRICT_READY="${EXPECT_IAP_STRICT_READY:-false}"
BUILD_TARGET="${BUILD_TARGET:-Synaptix.Backend.Api/Synaptix.Backend.Api.csproj}"
BASE_URL="${BASE_URL:-http://localhost:5000}"
EMAIL="${EMAIL:-demo@example.com}"
PASSWORD="${PASSWORD:-demo}"

echo "[NOW 1/5] Static + route gate"
./scripts/alpha-now-status.sh

echo "[NOW 2/5] Build gate"
if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet SDK is required for NOW build gate." >&2
  exit 1
fi
dotnet build "$BUILD_TARGET"

echo "[NOW 3/5] Migration gate"
if [[ "$RUN_MIGRATIONS" == "true" ]]; then
  dotnet ef database update --project Synaptix.Backend.Migrations --startup-project Synaptix.MigrationService
else
  echo "SKIP: RUN_MIGRATIONS=false (set true to execute DB migration gate)."
fi

echo "[NOW 4/5] Live smoke gate"
if [[ "$RUN_LIVE_SMOKE" == "true" ]]; then
  SMOKE_MODE=live \
  EXPECT_IAP_STRICT_READY="$EXPECT_IAP_STRICT_READY" \
  BASE_URL="$BASE_URL" \
  EMAIL="$EMAIL" \
  PASSWORD="$PASSWORD" \
  bash ./scripts/alpha-p0-smoke.sh
else
  echo "SKIP: RUN_LIVE_SMOKE=false (set true when API is running)."
fi

echo "[NOW 5/5] Go/No-Go note"
echo "Record Go/No-Go decision in docs/alpha_release_priority_2026-04-01.md after successful gates."
