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
  "Tycoon.Backend.Api/Features/Users"
  "Tycoon.Backend.Api/Features/Friends"
  "Tycoon.Backend.Api/Features/Missions"
)

PATTERN='Results\.(NotFound|BadRequest|Conflict|Unauthorized|Forbid|Problem|StatusCode)\('

set +e
# Search only C# files to reduce noise
raw_matches="$(rg -n --hidden -t cs "$PATTERN" "${TARGETS[@]}" 2>&1)"
rg_status=$?
set -e

if [[ $rg_status -eq 2 ]]; then
  echo "Failed to scan hardened surfaces:"
  echo "$raw_matches"
  exit 2
fi

# rg exit status:
#   0 => matches, 1 => no matches
if [[ $rg_status -eq 1 ]]; then
  echo "OK: Hardened surfaces use structured error envelopes for targeted error branches."
  exit 0
fi

violations=""
while IFS= read -r line; do
  [[ -z "$line" ]] && continue

  # If line does not contain at least two colons, it's not a file:line:content match (could be an rg error) — ignore
  colon_count="${line//[^:]}"
  if [[ ${#colon_count} -lt 2 ]]; then
    continue
  fi

  # format: file:line:content
  content="${line#*:*:}"
  content_trimmed="${content#${content%%[![:space:]]*}}"

  # ignore single-line comments, block comment markers, and explicit inline suppressions
  if [[ "$content_trimmed" =~ ^(//|/\*|\*|\*/).* ]]; then
    continue
  fi
  if [[ "$content" == *"HARDENING-IGNORE"* ]]; then
    continue
  fi

  violations+="$line"$'\n'
done <<< "$raw_matches"

if [[ -n "$violations" ]]; then
  echo "Found status-only error responses in hardened surfaces:"
  printf '%s' "$violations"
  exit 1
fi

echo "OK: Hardened surfaces use structured error envelopes for targeted error branches."
