#!/usr/bin/env bash
# =============================================================================
# Synaptix Backend — Local Bootstrap Script
# =============================================================================
# Generates secrets, starts infrastructure, provisions services, runs
# migrations, and starts the full backend stack.
# Usage:
#   ./scripts/bootstrap-local.sh [--skip-infra] [--skip-migration] [--force] [--dev-tools]
# =============================================================================
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

SKIP_INFRA=false
SKIP_MIGRATION=false
FORCE=false
DEV_TOOLS=false

for arg in "$@"; do
  case "$arg" in
    --skip-infra)      SKIP_INFRA=true ;;
    --skip-migration)  SKIP_MIGRATION=true ;;
    --force)           FORCE=true ;;
    --dev-tools)       DEV_TOOLS=true ;;
    *) echo "Unknown argument: $arg"; exit 1 ;;
  esac
done

step() { echo; echo "==> $1"; }
ok()   { echo "  ✓ $1"; }
warn() { echo "  ⚠ $1"; }
fail() { echo "  ✗ $1"; exit 1; }

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  Synaptix Backend — Local Bootstrap      ║"
echo "╚══════════════════════════════════════════╝"
echo ""

# Step 1: Generate secrets
step "Generating docker/.env with secure secrets"
SETUP_ARGS=("run" "--project" "$ROOT/Synaptix.Setup" "--" "init-local")
$FORCE && SETUP_ARGS+=("--force")
dotnet "${SETUP_ARGS[@]}" || fail "init-local failed."
ok "docker/.env generated."

# Step 2: Validate
step "Validating generated secrets"
dotnet run --project "$ROOT/Synaptix.Setup" -- validate --local || fail "Validation failed."
ok "Secrets validated."

# Step 3: Start infrastructure
if [ "$SKIP_INFRA" = false ]; then
  step "Starting infrastructure services"
  COMPOSE_SERVICES=(postgres mongodb redis rabbitmq minio elasticsearch)
  COMPOSE_ARGS=("-f" "$ROOT/docker/compose.yml" "up" "-d" "${COMPOSE_SERVICES[@]}")
  if [ "$DEV_TOOLS" = true ]; then
    COMPOSE_ARGS+=("--profile" "dev" "grafana" "prometheus" "kibana" "pgadmin" "mongo-express" "dbgate")
  fi
  docker compose "${COMPOSE_ARGS[@]}" || fail "docker compose up failed."
  ok "Infrastructure started."
  warn "Waiting 15 seconds for services to become healthy..."
  sleep 15
fi

# Step 4: Provision services
step "Provisioning infrastructure services"
dotnet run --project "$ROOT/Synaptix.Setup" -- provision-services
EXIT=$?
[ $EXIT -gt 1 ] && fail "provision-services failed critically."
ok "Services provisioned."

# Step 5: Migrations
if [ "$SKIP_MIGRATION" = false ]; then
  step "Running database migrations and seeding"
  dotnet run --project "$ROOT/Synaptix.MigrationService" || fail "MigrationService failed."
  ok "Migrations and seeding complete."
fi

# Step 6: Start application services
step "Starting backend API and dashboards"
docker compose -f "$ROOT/docker/compose.yml" up -d backend-api operator-dashboard \
  || warn "Some services may have failed to start — check 'docker ps'."

# Step 7: Status
step "Bootstrap status"
dotnet run --project "$ROOT/Synaptix.Setup" -- status

echo ""
echo "╔══════════════════════════════════════════╗"
echo "║  Bootstrap complete!                     ║"
echo "║                                          ║"
echo "║  API:       http://localhost:5000        ║"
echo "║  Dashboard: http://localhost:8200        ║"
echo "║  MinIO:     http://localhost:9001        ║"
echo "╚══════════════════════════════════════════╝"
echo ""
echo "Super admin credentials: .local/bootstrap/super-admin.local.txt"
echo ""
