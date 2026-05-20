#!/usr/bin/env bash
set -euo pipefail

TASK_ID="${1:-}"
COMMAND="${2:-}"
RESULT="${3:-not-run}"
NOTES="${4:-}"

if [[ -z "$TASK_ID" || -z "$COMMAND" ]]; then
  echo "Usage: $0 TASK_ID \"COMMAND\" RESULT \"NOTES\""
  exit 1
fi

case "$RESULT" in
  pass|fail|skipped|not-run) ;;
  *)
    echo "Invalid RESULT: $RESULT. Use pass, fail, skipped, or not-run."
    exit 1
    ;;
esac

LOG_PATH=".codex/heartbeat/verification-log.md"
TIMESTAMP="$(date '+%Y-%m-%d %H:%M %z')"

mkdir -p "$(dirname "$LOG_PATH")"

if [[ ! -f "$LOG_PATH" ]]; then
  echo "# Verification Log" > "$LOG_PATH"
  echo "" >> "$LOG_PATH"
fi

SAFE_COMMAND="${COMMAND//|/\\|}"
SAFE_NOTES="${NOTES//|/\\|}"

echo "| $TIMESTAMP | $TASK_ID | \`$SAFE_COMMAND\` | $RESULT | $SAFE_NOTES |" >> "$LOG_PATH"
echo "Heartbeat verification entry added to $LOG_PATH"
