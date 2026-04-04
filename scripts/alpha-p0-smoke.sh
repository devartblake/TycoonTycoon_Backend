#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
EMAIL="${EMAIL:-demo@example.com}"
PASSWORD="${PASSWORD:-demo}"
SMOKE_MODE="${SMOKE_MODE:-live}"
EXPECT_IAP_STRICT_READY="${EXPECT_IAP_STRICT_READY:-false}"

jq_bin="${JQ_BIN:-jq}"
has_jq=false
if command -v "$jq_bin" >/dev/null 2>&1; then
  has_jq=true
fi

if [[ "$has_jq" == false ]] && ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: neither jq nor python3 is available. Install one of them." >&2
  exit 1
fi

if [[ "$SMOKE_MODE" == "routes" ]]; then
  if ! command -v rg >/dev/null 2>&1; then
    echo "ERROR: SMOKE_MODE=routes requires ripgrep (rg)." >&2
    exit 1
  fi

  echo "[route-check] verifying required endpoint maps exist"
  rg -n 'MapPost\("/login"' Tycoon.Backend.Api/Features/Auth/AuthEndpoints.cs >/dev/null
  rg -n 'MapGet\("/set"|MapPost\("/check"|MapPost\("/check-batch"' Tycoon.Backend.Api/Features/Questions/QuestionsEndpoints.cs >/dev/null
  rg -n 'MapGet\("/catalog"|MapPost\("/purchase"|MapPost\("/iap/validate"' Tycoon.Backend.Api/Features/Store/StoreEndpoints.cs >/dev/null
  rg -n 'MapPost\("/link-wallet"|MapGet\("/balance|MapGet\("/history|MapPost\("/withdraw"' Tycoon.Backend.Api/Features/Crypto/CryptoEconomyEndpoints.cs >/dev/null
  rg -n 'MapGet\("/tiers/\{tierId:int\}"' Tycoon.Backend.Api/Features/Leaderboards/LeaderboardsEndpoints.cs >/dev/null

  echo "P0 smoke route-check completed."
  exit 0
fi

extract_token() {
  local payload="$1"
  if [[ "$has_jq" == true ]]; then
    echo "$payload" | "$jq_bin" -r '.data.accessToken // .accessToken // empty'
  else
    python3 - <<'PY' "$payload"
import json, sys
payload = sys.argv[1]
try:
    obj = json.loads(payload)
except Exception:
    print("")
    raise SystemExit(0)
token = ""
if isinstance(obj, dict):
    data = obj.get("data")
    if isinstance(data, dict):
        token = data.get("accessToken") or ""
    if not token:
        token = obj.get("accessToken") or ""
print(token)
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

curl_json() {
  local method="$1"
  local url="$2"
  local body="${3:-}"
  local auth="${4:-}"

  local tmp
  tmp=$(mktemp)

  local status
  if [[ -n "$body" ]]; then
    if [[ -n "$auth" ]]; then
      status=$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "$url" \
        -H 'Content-Type: application/json' \
        -H "$auth" \
        -d "$body")
    else
      status=$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "$url" \
        -H 'Content-Type: application/json' \
        -d "$body")
    fi
  else
    if [[ -n "$auth" ]]; then
      status=$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "$url" \
        -H "$auth")
    else
      status=$(curl -sS -o "$tmp" -w '%{http_code}' -X "$method" "$url")
    fi
  fi

  local payload
  payload=$(cat "$tmp")
  rm -f "$tmp"

  if [[ ! "$status" =~ ^2 ]]; then
    echo "ERROR: $method $url returned HTTP $status" >&2
    echo "$payload" >&2
    exit 1
  fi

  validate_json "$payload"
  echo "$payload"
}

echo "[1/6] Login"
login_payload=$(cat <<JSON
{"email":"$EMAIL","password":"$PASSWORD"}
JSON
)

login_response=$(curl_json "POST" "$BASE_URL/auth/login" "$login_payload")

token=$(extract_token "$login_response")
if [[ -z "$token" || "$token" == "null" ]]; then
  echo "ERROR: Failed to obtain access token from /auth/login response" >&2
  echo "$login_response" >&2
  exit 1
fi

auth_header="Authorization: Bearer $token"

echo "[2/6] Questions set"
curl_json "GET" "$BASE_URL/questions/set?count=5" >/dev/null

echo "[3/6] Store catalog"
curl_json "GET" "$BASE_URL/store/catalog" >/dev/null

echo "[4/6] IAP validate (strict mode behavior check)"
iap_payload='{"playerId":"00000000-0000-0000-0000-000000000001","platform":"apple","receipt":"test-receipt"}'
iap_response=$(curl_json "POST" "$BASE_URL/store/iap/validate" "$iap_payload" "$auth_header")

if [[ "$EXPECT_IAP_STRICT_READY" == "true" ]]; then
  if [[ "$iap_response" == *"IAP_STRICT_CONFIG_MISSING"* ]]; then
    echo "ERROR: strict IAP mode is not fully configured (IAP_STRICT_CONFIG_MISSING)." >&2
    exit 1
  fi
fi

echo "[5/6] Crypto history route health check"
curl_json "GET" "$BASE_URL/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1" "" "$auth_header" >/dev/null

echo "[6/6] Leaderboards route health check"
curl_json "GET" "$BASE_URL/leaderboards/tiers/1?page=1&pageSize=10" >/dev/null

echo "P0 smoke script completed."
