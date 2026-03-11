#!/usr/bin/env bash
set -euo pipefail

# Hardened surfaces should not regress back to status-only error responses.
TARGETS=(
  "Tycoon.Backend.Api/Features/AdminAuth"
  "Tycoon.Backend.Api/Features/AdminNotifications"
  "Tycoon.Backend.Api/Features/AdminSeasons"
  "Tycoon.Backend.Api/Features/AdminAntiCheat"
  "Tycoon.Backend.Api/Features/Party"
  "Tycoon.Backend.Api/Features/Matches"
  "Tycoon.Backend.Api/Features/Matchmaking"
  "Tycoon.Backend.Api/Features/Mobile/Matches"
)

PATTERN='Results\.(NotFound|BadRequest|Conflict|Unauthorized|Forbid|Problem|StatusCode)\('

matches="$(rg -n "$PATTERN" "${TARGETS[@]}" || true)"
# Ignore commented examples and intentionally commented legacy snippets.
matches="$(printf '%s\n' "$matches" | rg -v '^\s*//' || true)"

if [[ -n "${matches// }" ]]; then
  echo "Found status-only error responses in hardened surfaces:"
  echo "$matches"
  exit 1
fi

echo "OK: Hardened surfaces use structured error envelopes for targeted error branches."
