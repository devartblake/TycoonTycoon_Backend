#!/usr/bin/env bash
# compose-smoke.sh — smoke-test operator login + core BFF flows against compose services.
#
# Usage:
#   ./scripts/compose-smoke.sh                       # start stack, run tests, tear down
#   STACK_RUNNING=true ./scripts/compose-smoke.sh    # run against already-running stack
#   KEEP_STACK=true ./scripts/compose-smoke.sh       # keep stack alive after tests
#
# Key env vars:
#   SMOKE_ADMIN_EMAIL     Operator email to test with  (default: smoke-admin@synaptix.local)
#   SMOKE_ADMIN_PASSWORD  Operator password            (default: SmokeTest123!)
#   API_URL               Backend API base URL         (default: http://localhost:5000)
#   DASHBOARD_URL         Operator dashboard base URL  (default: http://localhost:8200)
#   WAIT_TIMEOUT          Seconds to wait for healthy  (default: 120)
#   STACK_RUNNING         Skip compose up/down         (default: false)
#   KEEP_STACK            Skip compose down on exit    (default: false)
set -euo pipefail

COMPOSE_FILE="${COMPOSE_FILE:-docker/compose.yml}"
COMPOSE_SMOKE_FILE="${COMPOSE_SMOKE_FILE:-docker/compose.smoke.yml}"

STACK_RUNNING="${STACK_RUNNING:-false}"
KEEP_STACK="${KEEP_STACK:-false}"

API_URL="${API_URL:-http://localhost:5000}"
DASHBOARD_URL="${DASHBOARD_URL:-http://localhost:8200}"

SMOKE_ADMIN_EMAIL="${SMOKE_ADMIN_EMAIL:-smoke-admin@synaptix.local}"
SMOKE_ADMIN_PASSWORD="${SMOKE_ADMIN_PASSWORD:-SmokeTest123!}"

WAIT_TIMEOUT="${WAIT_TIMEOUT:-120}"

# ── Color helpers ─────────────────────────────────────────────────────────────
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
ok()   { echo -e "${GREEN}  ✅ $*${NC}"; }
fail() { echo -e "${RED}  ❌ $*${NC}" >&2; exit 1; }
info() { echo -e "${YELLOW}  ℹ  $*${NC}"; }

# ── JSON helpers ──────────────────────────────────────────────────────────────
jq_bin="${JQ_BIN:-jq}"
has_jq=false
if command -v "$jq_bin" >/dev/null 2>&1; then
  has_jq=true
fi

if [[ "$has_jq" == false ]] && ! command -v python3 >/dev/null 2>&1; then
  fail "neither jq nor python3 is available — install one of them"
fi

extract_field() {
  local payload="$1" field="$2"
  if [[ "$has_jq" == true ]]; then
    echo "$payload" | "$jq_bin" -r "$field // empty"
  else
    python3 - "$payload" "$field" <<'PY'
import json, sys
obj = json.loads(sys.argv[1])
for part in sys.argv[2].lstrip(".").split("."):
    if isinstance(obj, dict) and part in obj:
        obj = obj[part]
    else:
        obj = None
        break
print("" if obj is None else str(obj))
PY
  fi
}

validate_json() {
  local payload="$1"
  if [[ "$has_jq" == true ]]; then
    echo "$payload" | "$jq_bin" '.' >/dev/null
  else
    python3 -m json.tool >/dev/null <<<"$payload"
  fi
}

# ── curl helpers ──────────────────────────────────────────────────────────────
# curl_json METHOD URL [body] [extra_curl_args...]
#   Returns body; exits on non-2xx response.
curl_json() {
  local method="$1" url="$2" body="${3:-}" ; shift 3
  local tmp; tmp=$(mktemp)

  local args=( -sS -o "$tmp" -w '%{http_code}' -X "$method" "$url" )
  [[ -n "$body" ]] && args+=( -H 'Content-Type: application/json' -d "$body" )
  args+=( "$@" )

  local status; status=$(curl "${args[@]}")
  local payload; payload=$(cat "$tmp"); rm -f "$tmp"

  if [[ ! "$status" =~ ^2 ]]; then
    echo "ERROR: $method $url returned HTTP $status" >&2
    echo "$payload" >&2
    exit 1
  fi
  validate_json "$payload"
  echo "$payload"
}

# ── Health polling ────────────────────────────────────────────────────────────
wait_for_url() {
  local label="$1" url="$2"
  local deadline=$(( $(date +%s) + WAIT_TIMEOUT ))
  info "Waiting for $label ($url) ..."
  until curl -fsS -o /dev/null "$url" 2>/dev/null; do
    if [[ $(date +%s) -ge $deadline ]]; then
      fail "Timed out waiting for $label"
    fi
    sleep 3
  done
  ok "$label is ready"
}

# ── Compose lifecycle ─────────────────────────────────────────────────────────
COMPOSE_CMD="docker compose -f $COMPOSE_FILE -f $COMPOSE_SMOKE_FILE"

compose_up() {
  info "Starting compose stack (build may take a few minutes on first run)..."
  export SMOKE_ADMIN_EMAIL SMOKE_ADMIN_PASSWORD
  $COMPOSE_CMD up -d --build 2>&1
}

compose_down() {
  info "Tearing down compose stack..."
  $COMPOSE_CMD down -v --remove-orphans 2>&1 || true
}

cleanup() {
  local rc=$?
  rm -f "${cookie_jar:-}"
  if [[ "$STACK_RUNNING" == "false" && "$KEEP_STACK" == "false" ]]; then
    compose_down
  fi
  exit $rc
}

# Initialise the cookie jar path early so the single EXIT trap can always clean it up.
cookie_jar=$(mktemp)

# ── Main ──────────────────────────────────────────────────────────────────────
echo "════════════════════════════════════════════════════════"
echo "  Tycoon – Compose Smoke Test"
echo "  API:       $API_URL"
echo "  Dashboard: $DASHBOARD_URL"
echo "════════════════════════════════════════════════════════"

trap cleanup EXIT

if [[ "$STACK_RUNNING" == "false" ]]; then
  compose_up
fi

# ── Step 1: Wait for services ─────────────────────────────────────────────────
echo ""
echo "[wait] Waiting for backend API..."
wait_for_url "backend-api" "$API_URL/healthz"

echo ""
echo "[wait] Waiting for operator dashboard..."
wait_for_url "operator-dashboard" "$DASHBOARD_URL/healthz"

# ── Step 2: Backend API health ───────────────────────────────────────────────
echo ""
echo "[1/6] Backend API health check"
api_health=$(curl_json "GET" "$API_URL/healthz")
ok "GET /healthz → $(extract_field "$api_health" '.status // "ok"')"

# ── Step 3: Operator login (admin auth) ───────────────────────────────────────
echo ""
echo "[2/6] Operator login — POST /admin/auth/login"
login_payload=$(cat <<JSON
{"email":"${SMOKE_ADMIN_EMAIL}","password":"${SMOKE_ADMIN_PASSWORD}"}
JSON
)
login_response=$(curl_json "POST" "$API_URL/admin/auth/login" "$login_payload")
access_token=$(extract_field "$login_response" '.accessToken')
if [[ -z "$access_token" || "$access_token" == "null" ]]; then
  fail "Login succeeded (HTTP 2xx) but no accessToken in response"
fi
ok "Login succeeded — received accessToken"

# ── Step 4: Admin me ──────────────────────────────────────────────────────────
echo ""
echo "[3/6] Admin profile — GET /admin/auth/me"
me_response=$(curl_json "GET" "$API_URL/admin/auth/me" "" \
  -H "Authorization: Bearer $access_token")
admin_email=$(extract_field "$me_response" '.email')
ok "Admin me returned email: ${admin_email:-<present>}"

# ── Step 5: Admin dashboard overview (BFF source data) ───────────────────────
echo ""
echo "[4/6] Dashboard overview — GET /admin/dashboard"
dash_status=$(curl -sS -o /dev/null -w '%{http_code}' \
  -H "Authorization: Bearer $access_token" \
  "$API_URL/admin/dashboard")
# 200 (data) or 204 (no content) are both fine; 401/403 are failures
if [[ "$dash_status" =~ ^2 ]]; then
  ok "GET /admin/dashboard → HTTP $dash_status"
else
  fail "GET /admin/dashboard returned unexpected HTTP $dash_status"
fi

# ── Step 6: Django dashboard health ──────────────────────────────────────────
echo ""
echo "[5/6] Operator dashboard health — GET $DASHBOARD_URL/healthz"
dash_health=$(curl_json "GET" "$DASHBOARD_URL/healthz")
ok "GET /healthz → $(extract_field "$dash_health" '.status // "ok"')"

# ── Step 7: Django dashboard session login + BFF probe ───────────────────────
echo ""
echo "[6/6] Operator dashboard session login + BFF probe"

# 6a. Fetch login page to obtain CSRF cookie
curl -sS -c "$cookie_jar" -o /dev/null "$DASHBOARD_URL/login"
csrf_token=$(grep -i csrftoken "$cookie_jar" | awk '{print $NF}' | tail -1)

if [[ -z "$csrf_token" ]]; then
  info "CSRF token not found in cookie jar — skipping Django session smoke (CSRF may be disabled)"
else
  # 6b. POST credentials to login page
  login_http=$(curl -sS -L \
    -b "$cookie_jar" -c "$cookie_jar" \
    -w '%{http_code}' -o /dev/null \
    -X POST "$DASHBOARD_URL/login" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -H "Referer: $DASHBOARD_URL/login" \
    --data-urlencode "email=$SMOKE_ADMIN_EMAIL" \
    --data-urlencode "password=$SMOKE_ADMIN_PASSWORD" \
    -d "csrfmiddlewaretoken=$csrf_token")

  # After a successful login Django redirects to /, which returns 200
  if [[ ! "$login_http" =~ ^2 ]]; then
    fail "Dashboard session login returned HTTP $login_http (expected 2xx after redirect)"
  fi
  ok "Dashboard session login succeeded (final HTTP $login_http)"

  # 6c. Probe the operator health BFF endpoint
  health_http=$(curl -sS -b "$cookie_jar" -o /dev/null -w '%{http_code}' \
    "$DASHBOARD_URL/api/operator/health")
  if [[ "$health_http" =~ ^2 ]]; then
    ok "GET /api/operator/health → HTTP $health_http"
  else
    fail "GET /api/operator/health returned HTTP $health_http (expected 2xx)"
  fi
fi

echo ""
echo "════════════════════════════════════════════════════════"
echo -e "${GREEN}  Compose smoke test completed successfully.${NC}"
echo "════════════════════════════════════════════════════════"
