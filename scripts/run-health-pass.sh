#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
REPORT_PATH="${ROOT_DIR}/docs/PROJECT_HEALTH_REPORT.md"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT

declare -a COMMANDS=(
  "dotnet restore"
  "dotnet build --configuration Release --no-restore"
  "dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build"
  "bash scripts/check-error-envelope-hardening.sh"
  "bash scripts/validate-ef-schema.sh"
  "docker compose -f docker/compose.yml build operator-dashboard"
)

timestamp_utc="$(date -u +%Y-%m-%d)"

run_cmd() {
  local cmd="$1"
  local idx="$2"
  local out_file="${TMP_DIR}/cmd_${idx}.log"
  local status notes
  local runner_cmd="${cmd}"

  # If local dotnet is unavailable but docker is present, run dotnet commands
  # in the .NET 9 SDK container so health pass can still run on Docker-only hosts.
  if [[ "${cmd}" == dotnet* ]] && ! command -v dotnet >/dev/null 2>&1 && command -v docker >/dev/null 2>&1; then
    runner_cmd="docker run --rm -v \"${ROOT_DIR}\":/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 bash -lc \"${cmd}\""
  fi

  # Same fallback for EF validation script (which internally shells out to dotnet).
  if [[ "${cmd}" == "bash scripts/validate-ef-schema.sh" ]] && ! command -v dotnet >/dev/null 2>&1 && command -v docker >/dev/null 2>&1; then
    runner_cmd="docker run --rm -v \"${ROOT_DIR}\":/workspace -w /workspace mcr.microsoft.com/dotnet/sdk:9.0 bash -lc \"bash scripts/validate-ef-schema.sh\""
  fi

  set +e
  (cd "${ROOT_DIR}" && bash -lc "${runner_cmd}") >"${out_file}" 2>&1
  local exit_code=$?
  set -e

  if [[ ${exit_code} -eq 0 ]]; then
    status="✅ Pass"
    notes="Command completed successfully."
  else
    if grep -qiE "command not found|not installed|No such file or directory" "${out_file}"; then
      status="❌ Blocked"
      notes="$(head -n 1 "${out_file}" | sed 's/|/\\|/g')"
    else
      status="❌ Fail"
      notes="$(head -n 1 "${out_file}" | sed 's/|/\\|/g')"
      if [[ -z "${notes}" ]]; then
        notes="Exited with code ${exit_code}."
      fi
    fi
  fi

  printf '%s\t%s\t%s\n' "${cmd}" "${status}" "${notes}" >> "${TMP_DIR}/results.txt"
}

idx=0
for cmd in "${COMMANDS[@]}"; do
  run_cmd "${cmd}" "${idx}"
  idx=$((idx + 1))
done

{
  echo "# Project Health Report"
  echo
  echo "Date: ${timestamp_utc} (UTC)"
  echo
  echo "## Scope"
  echo "Health-pass commands requested for:"
  echo "- restore / build / test"
  echo "- hardened error-envelope guard"
  echo "- EF schema drift validation"
  echo "- operator dashboard container build (authoritative target)"
  echo
  echo "## Results"
  echo
  echo "| Command | Status | Notes |"
  echo "|---|---|---|"
  while IFS=$'\t' read -r cmd status notes; do
    echo "| \`${cmd}\` | ${status} | ${notes} |"
  done < "${TMP_DIR}/results.txt"
  echo
  echo "## Dashboard Target Decision"
  echo "- Authoritative target remains **Blazor Operator Dashboard** via \`docker/Dockerfile.dashboard\` as configured in compose."
  echo "- Archived alternate dashboard-web Dockerfiles as \`.txt\` to avoid split build paths without deleting project artifacts."
  echo
  echo "## Next Actions"
  echo "1. Re-run this health pass in CI/dev with .NET 9 SDK + Docker installed."
  echo "2. Attach full command logs if any command fails."
  echo "3. Mark blockers cleared and update final pass/fail summary."
} > "${REPORT_PATH}"

echo "Wrote health report to ${REPORT_PATH}"
