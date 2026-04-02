#!/usr/bin/env bash
set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5000}"
EMAIL="${EMAIL:-demo@example.com}"
PASSWORD="${PASSWORD:-demo}"

jq_bin="${JQ_BIN:-jq}"
if ! command -v "$jq_bin" >/dev/null 2>&1; then
  echo "ERROR: jq is required. Install jq or set JQ_BIN to its path." >&2
  exit 1
fi

echo "[1/6] Login"
login_payload=$(cat <<JSON
{"email":"$EMAIL","password":"$PASSWORD"}
JSON
)

login_response=$(curl -sS -X POST "$BASE_URL/auth/login" \
  -H 'Content-Type: application/json' \
  -d "$login_payload")

token=$(echo "$login_response" | "$jq_bin" -r '.data.accessToken // .accessToken // empty')
if [[ -z "$token" || "$token" == "null" ]]; then
  echo "ERROR: Failed to obtain access token from /auth/login response" >&2
  echo "$login_response" >&2
  exit 1
fi

auth_header="Authorization: Bearer $token"

echo "[2/6] Questions set"
curl -sS "$BASE_URL/questions/set?count=5" | "$jq_bin" '.' >/dev/null

echo "[3/6] Store catalog"
curl -sS "$BASE_URL/store/catalog" | "$jq_bin" '.' >/dev/null

echo "[4/6] IAP validate (strict mode behavior check)"
iap_payload='{"playerId":"00000000-0000-0000-0000-000000000001","platform":"apple","receipt":"test-receipt"}'
curl -sS -X POST "$BASE_URL/store/iap/validate" \
  -H 'Content-Type: application/json' \
  -H "$auth_header" \
  -d "$iap_payload" | "$jq_bin" '.' >/dev/null

echo "[5/6] Crypto history route health check"
curl -sS "$BASE_URL/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1" \
  -H "$auth_header" | "$jq_bin" '.' >/dev/null

echo "[6/6] Leaderboards route health check"
curl -sS "$BASE_URL/leaderboards/tiers/1?page=1&pageSize=10" | "$jq_bin" '.' >/dev/null

echo "P0 smoke script completed."
