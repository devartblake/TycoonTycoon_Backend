#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
EMAIL="${EMAIL:-demo@example.com}"
PASSWORD="${PASSWORD:-demo}"

jq_bin="${JQ_BIN:-jq}"
has_jq=false
if command -v "$jq_bin" >/dev/null 2>&1; then
  has_jq=true
fi

if [[ "$has_jq" == false ]] && ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: neither jq nor python3 is available. Install one of them." >&2
  exit 1
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

echo "[1/6] Login"
login_payload=$(cat <<JSON
{"email":"$EMAIL","password":"$PASSWORD"}
JSON
)

login_response=$(curl -sS -X POST "$BASE_URL/auth/login" \
  -H 'Content-Type: application/json' \
  -d "$login_payload")

token=$(extract_token "$login_response")
if [[ -z "$token" || "$token" == "null" ]]; then
  echo "ERROR: Failed to obtain access token from /auth/login response" >&2
  echo "$login_response" >&2
  exit 1
fi

auth_header="Authorization: Bearer $token"

echo "[2/6] Questions set"
validate_json "$(curl -sS "$BASE_URL/questions/set?count=5")"

echo "[3/6] Store catalog"
validate_json "$(curl -sS "$BASE_URL/store/catalog")"

echo "[4/6] IAP validate (strict mode behavior check)"
iap_payload='{"playerId":"00000000-0000-0000-0000-000000000001","platform":"apple","receipt":"test-receipt"}'
iap_response=$(curl -sS -X POST "$BASE_URL/store/iap/validate" \
  -H 'Content-Type: application/json' \
  -H "$auth_header" \
  -d "$iap_payload")
validate_json "$iap_response"

echo "[5/6] Crypto history route health check"
crypto_history_response=$(curl -sS "$BASE_URL/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1" \
  -H "$auth_header")
validate_json "$crypto_history_response"

echo "[6/6] Leaderboards route health check"
validate_json "$(curl -sS "$BASE_URL/leaderboards/tiers/1?page=1&pageSize=10")"

echo "P0 smoke script completed."
