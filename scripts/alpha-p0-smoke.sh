#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
EMAIL="${EMAIL:-demo@example.com}"
PASSWORD="${PASSWORD:-demo}"
SIGNUP_PASSWORD="${SIGNUP_PASSWORD:-Passw0rd!}"
SMOKE_MODE="${SMOKE_MODE:-live}"
EXPECT_IAP_STRICT_READY="${EXPECT_IAP_STRICT_READY:-false}"
AUTO_SIGNUP="${AUTO_SIGNUP:-true}"

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
  rg -n 'MapPost\("/login"' Synaptix.Backend.Api/Features/Auth/AuthEndpoints.cs >/dev/null
  rg -n 'MapGet\("/set"|MapPost\("/check"|MapPost\("/check-batch"' Synaptix.Backend.Api/Features/Questions/QuestionsEndpoints.cs >/dev/null
  rg -n 'MapGet\("/catalog"|MapPost\("/purchase"|MapPost\("/iap/validate"' Synaptix.Backend.Api/Features/Store/StoreEndpoints.cs >/dev/null
  rg -n 'MapPost\("/link-wallet"|MapGet\("/balance|MapGet\("/history|MapPost\("/withdraw"' Synaptix.Backend.Api/Features/Crypto/CryptoEconomyEndpoints.cs >/dev/null
  rg -n 'MapGet\("/tiers/\{tierId:int\}"' Synaptix.Backend.Api/Features/Leaderboards/LeaderboardsEndpoints.cs >/dev/null

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

extract_field() {
  local payload="$1"
  local field="$2"
  if [[ "$has_jq" == true ]]; then
    echo "$payload" | "$jq_bin" -r "$field // empty"
  else
    python3 - <<'PY' "$payload" "$field"
import json, sys
payload = sys.argv[1]
field = sys.argv[2]
try:
    obj = json.loads(payload)
except Exception:
    print("")
    raise SystemExit(0)
parts = [p for p in field.strip(".").split(".") if p]
cur = obj
for p in parts:
    if isinstance(cur, dict) and p in cur:
        cur = cur[p]
    else:
        print("")
        raise SystemExit(0)
print("" if cur is None else str(cur))
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

if [[ "$AUTO_SIGNUP" == "true" ]]; then
  EMAIL="smoke-$(date +%s)-$RANDOM@example.com"
  username="smoke_user_$(date +%s)_$RANDOM"
  device_id="smoke-device-$RANDOM"
  echo "[1/8] Signup"
  signup_payload=$(cat <<JSON
{"email":"$EMAIL","password":"$SIGNUP_PASSWORD","deviceId":"$device_id","username":"$username"}
JSON
)
  signup_response=$(curl_json "POST" "$BASE_URL/auth/signup" "$signup_payload")
  token=$(extract_token "$signup_response")
  player_id=$(extract_field "$signup_response" ".userId")
else
  echo "[1/8] Login"
  login_payload=$(cat <<JSON
{"email":"$EMAIL","password":"$PASSWORD"}
JSON
)
  login_response=$(curl_json "POST" "$BASE_URL/auth/login" "$login_payload")
  token=$(extract_token "$login_response")
  player_id="00000000-0000-0000-0000-000000000001"
fi

if [[ -z "$token" || "$token" == "null" ]]; then
  echo "ERROR: Failed to obtain access token from auth response" >&2
  exit 1
fi

auth_header="Authorization: Bearer $token"
if [[ -z "${player_id:-}" ]]; then
  player_id="00000000-0000-0000-0000-000000000001"
fi

echo "[2/8] Questions set"
questions_set=$(curl_json "GET" "$BASE_URL/questions/set?count=1")

question_id=$(extract_field "$questions_set" ".questions.0.id")
if [[ -n "$question_id" ]]; then
  echo "[3/8] Questions check"
  check_payload=$(cat <<JSON
{"questionId":"$question_id","selectedIndex":0}
JSON
)
  curl_json "POST" "$BASE_URL/questions/check" "$check_payload" >/dev/null
else
  echo "[3/8] Questions check skipped (no question returned)"
fi

echo "[4/8] Store catalog"
curl_json "GET" "$BASE_URL/store/catalog" >/dev/null

echo "[5/8] IAP validate (strict mode behavior check)"
iap_payload=$(cat <<JSON
{"playerId":"$player_id","platform":"apple","receipt":"test-receipt"}
JSON
)
iap_response=$(curl_json "POST" "$BASE_URL/store/iap/validate" "$iap_payload" "$auth_header")

if [[ "$EXPECT_IAP_STRICT_READY" == "true" ]]; then
  if [[ "$iap_response" == *"IAP_STRICT_CONFIG_MISSING"* ]]; then
    echo "ERROR: strict IAP mode is not fully configured (IAP_STRICT_CONFIG_MISSING)." >&2
    exit 1
  fi
fi

echo "[6/8] Store purchase contract check"
purchase_payload=$(cat <<JSON
{"playerId":"$player_id","sku":"coins_pack_small","quantity":1,"currency":"coins"}
JSON
)
if purchase_response=$(curl -sS -w '\n%{http_code}' -X POST "$BASE_URL/store/purchase" \
  -H 'Content-Type: application/json' -H "$auth_header" -d "$purchase_payload"); then
  purchase_status=$(echo "$purchase_response" | tail -n1)
  purchase_body=$(echo "$purchase_response" | sed '$d')
  validate_json "$purchase_body"
  if [[ "$purchase_status" =~ ^2|409$|400$|404$ ]]; then
    echo "Store purchase check completed (HTTP $purchase_status)."
  else
    echo "ERROR: unexpected /store/purchase status $purchase_status" >&2
    echo "$purchase_body" >&2
    exit 1
  fi
fi

echo "[7/8] Crypto history route health check"
curl_json "GET" "$BASE_URL/crypto/history/$player_id?page=1&pageSize=1" "" "$auth_header" >/dev/null

echo "[8/8] Leaderboards route health check"
curl_json "GET" "$BASE_URL/leaderboards/tiers/1?page=1&pageSize=10" >/dev/null

echo "P0 smoke script completed."
