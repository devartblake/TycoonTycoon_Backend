#!/usr/bin/env bash
###############################################################################
# bootstrap-stack.sh — Build images, then bring up the stack so the one-shot
# setup + migration services run automatically, after validating the EF model
# snapshot for drift.
#
# `docker compose build` only builds images — it never starts containers.
# This script wraps build + up so the ordered chain in docker/compose.yml
# (setup -> migration -> backend-api, gated by service_completed_successfully)
# actually executes.
#
# Flow:
#   1. EF model snapshot drift check  (scripts/validate-ef-schema.sh)
#   2. docker compose build
#   3. docker compose up -d           (runs setup, then migration, then API)
#
# Usage:
#   ./scripts/bootstrap-stack.sh                 # snapshot check, build, up
#   ./scripts/bootstrap-stack.sh --no-snapshot   # skip the EF drift check
#   ./scripts/bootstrap-stack.sh --no-build      # skip build, just up
#   ./scripts/bootstrap-stack.sh --reset         # drop & recreate DB on migrate
#   ./scripts/bootstrap-stack.sh --dev           # include dev-profile services
###############################################################################
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$PROJECT_ROOT"

COMPOSE_FILE="docker/compose.yml"

# ── Defaults ──────────────────────────────────────────────────────────────
RUN_SNAPSHOT=true
RUN_BUILD=true
RESET=false
DEV=false

# ── Parse args ──────────────────────────────────────────────────────────────
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-snapshot) RUN_SNAPSHOT=false; shift ;;
    --no-build)    RUN_BUILD=false; shift ;;
    --reset)       RESET=true; shift ;;
    --dev)         DEV=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--no-snapshot] [--no-build] [--reset] [--dev]"
      echo ""
      echo "  --no-snapshot  Skip the EF model snapshot drift check"
      echo "  --no-build     Skip 'docker compose build' (just bring the stack up)"
      echo "  --reset        Drop and recreate the database during migration"
      echo "  --dev          Include dev-profile services (Grafana, pgAdmin, etc.)"
      exit 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      exit 1
      ;;
  esac
done

log()  { echo "[bootstrap] $*"; }
fail() { echo "[bootstrap] ERROR: $*" >&2; exit 1; }

COMPOSE=(docker compose -f "$COMPOSE_FILE")
$DEV && COMPOSE+=(--profile dev)

# ── Step 1: EF model snapshot drift check ─────────────────────────────────────
if $RUN_SNAPSHOT; then
  log "Validating EF model snapshot for drift..."
  ./scripts/validate-ef-schema.sh \
    || fail "EF model snapshot is out of sync. Add a migration and re-run, or pass --no-snapshot to skip."
else
  log "Skipping EF model snapshot check (--no-snapshot)."
fi

# ── Step 2: Build images ──────────────────────────────────────────────────────
if $RUN_BUILD; then
  log "Building Docker images..."
  "${COMPOSE[@]}" build
else
  log "Skipping image build (--no-build)."
fi

# ── Step 3: Bring up the stack (setup -> migration -> backend-api) ────────────
if $RESET; then
  log "Reset mode enabled — database will be dropped and recreated during migration."
  export MIGRATION_RESET_DATABASE=true
fi

log "Bringing up the stack — setup and migration run automatically before the API..."
"${COMPOSE[@]}" up -d

log "Done. Tail progress with: docker compose -f $COMPOSE_FILE logs -f setup migration"
