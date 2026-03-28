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

scan_matches() {
  if command -v rg >/dev/null 2>&1; then
    rg -n "$PATTERN" "${TARGETS[@]}"
    return $?
  fi

  # Fallback for environments without ripgrep.
  local status=1
  local file
  for target in "${TARGETS[@]}"; do
    while IFS= read -r -d '' file; do
      if grep -nE "$PATTERN" "$file"; then
        status=0
      fi
    done < <(find "$target" -type f -name '*.cs' -print0)
  done

  return $status
}

set +e
raw_matches="$(scan_matches 2>&1)"
scan_status=$?
set -e

if [[ $scan_status -eq 2 ]]; then
  echo "Failed to scan hardened surfaces:"
  echo "$raw_matches"
  exit 2
fi

# scan exit status:
#   0 => matches, 1 => no matches
if [[ $scan_status -eq 1 ]]; then
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
