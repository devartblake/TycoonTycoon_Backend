#!/usr/bin/env bash
set -euo pipefail

# Reproduce the operator cutover CI blockers locally and collect evidence.
#
# Defaults mirror the failing GitHub Actions path:
# - dotnet restore/build
# - EF schema validation
# - full Tycoon.Backend.Api.Tests run with Redis
# - optional compose smoke
#
# Useful env overrides:
#   RUN_API_TESTS=false       Skip the full API test suite
#   RUN_COMPOSE_SMOKE=true    Also run scripts/compose-smoke.sh
#   TEST_FILTER='...'         Pass an xUnit filter to dotnet test
#   SKIP_BUILD=true           Skip restore/build and use existing binaries
#   KEEP_REDIS=true           Leave the local Redis container running

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACT_DIR="${ROOT_DIR}/artifacts/operator-cutover-ci-diagnostics"
ARTIFACT_DIR_REL="artifacts/operator-cutover-ci-diagnostics"
REDIS_CONTAINER="${REDIS_CONTAINER:-operator-cutover-ci-redis}"
REDIS_PASSWORD="${REDIS_PASSWORD:-synaptix_redis_password_123}"

RUN_API_TESTS="${RUN_API_TESTS:-true}"
RUN_COMPOSE_SMOKE="${RUN_COMPOSE_SMOKE:-false}"
SKIP_BUILD="${SKIP_BUILD:-false}"
KEEP_REDIS="${KEEP_REDIS:-false}"
TEST_FILTER="${TEST_FILTER:-}"
OVERALL_STATUS=0

mkdir -p "${ARTIFACT_DIR}"
rm -f "${ARTIFACT_DIR}"/*.log "${ARTIFACT_DIR}"/*.trx "${ARTIFACT_DIR}"/*.md

REPORT="${ARTIFACT_DIR}/summary.md"

log() {
  printf '[operator-cutover-ci] %s\n' "$*"
}

append_report() {
  printf '%s\n' "$*" >> "${REPORT}"
}

run_step() {
  local name="$1"
  shift
  local log_file="${ARTIFACT_DIR}/${name}.log"

  log "Running ${name}: $*"
  append_report ""
  append_report "## ${name}"
  append_report ""
  append_report '```text'

  set +e
  (cd "${ROOT_DIR}" && "$@") >"${log_file}" 2>&1
  local status=$?
  set -e

  sed -n '1,120p' "${log_file}" >> "${REPORT}" || true
  append_report '```'
  append_report ""
  append_report "- Exit code: \`${status}\`"
  append_report "- Log: \`${log_file#${ROOT_DIR}/}\`"

  if [[ ${status} -ne 0 ]]; then
    log "${name} failed with exit code ${status}"
    set +e
    return "${status}"
  fi

  log "${name} passed"
}

cleanup() {
  local rc=$?
  if [[ "${KEEP_REDIS}" != "true" ]]; then
    docker rm -f "${REDIS_CONTAINER}" >/dev/null 2>&1 || true
  fi
  exit "${rc}"
}
trap cleanup EXIT

cat > "${REPORT}" <<REPORT_HEADER
# Operator Cutover CI Diagnostics

- Generated UTC: $(date -u +'%Y-%m-%dT%H:%M:%SZ')
- Working tree commit: $(git -C "${ROOT_DIR}" rev-parse HEAD 2>/dev/null || echo unknown)
- API tests: ${RUN_API_TESTS}
- Compose smoke: ${RUN_COMPOSE_SMOKE}
- Test filter: ${TEST_FILTER:-<none>}

REPORT_HEADER

if ! command -v dotnet >/dev/null 2>&1; then
  for candidate in \
    "/c/Program Files/dotnet" \
    "/mnt/c/Program Files/dotnet"
  do
    if [[ -x "${candidate}/dotnet.exe" ]]; then
      mkdir -p "${ARTIFACT_DIR}/bin"
      cat > "${ARTIFACT_DIR}/bin/dotnet" <<SHIM
#!/usr/bin/env bash
exec "${candidate}/dotnet.exe" "\$@"
SHIM
      chmod +x "${ARTIFACT_DIR}/bin/dotnet"
      export PATH="${ARTIFACT_DIR}/bin:${PATH}"
      break
    fi
  done
fi

if ! command -v dotnet >/dev/null 2>&1; then
  log "dotnet SDK is required but was not found on PATH"
  append_report "## Blocked"
  append_report ""
  append_report "dotnet SDK is required but was not found on PATH."
  exit 127
fi

if ! command -v docker >/dev/null 2>&1; then
  log "Docker is required for the Redis dependency but was not found on PATH"
  append_report "## Blocked"
  append_report ""
  append_report "Docker is required for the Redis dependency but was not found on PATH."
  exit 127
fi

if [[ "${SKIP_BUILD}" != "true" ]]; then
  run_step restore dotnet restore TycoonTycoon_Backend.slnx
  run_step build dotnet build TycoonTycoon_Backend.slnx --configuration Release --no-restore
else
  log "Skipping restore/build because SKIP_BUILD=true"
fi

if ! run_step schema-validation bash scripts/validate-ef-schema.sh; then
  OVERALL_STATUS=1
fi

if [[ "${RUN_API_TESTS}" == "true" ]]; then
  log "Starting Redis dependency: ${REDIS_CONTAINER}"
  docker rm -f "${REDIS_CONTAINER}" >/dev/null 2>&1 || true
  docker run -d \
    --name "${REDIS_CONTAINER}" \
    -p 6379:6379 \
    redis:7-alpine \
    redis-server --requirepass "${REDIS_PASSWORD}" >/dev/null

  for _ in {1..20}; do
    if docker exec "${REDIS_CONTAINER}" redis-cli -a "${REDIS_PASSWORD}" ping 2>/dev/null | grep -q PONG; then
      log "Redis is ready"
      break
    fi
    sleep 1
  done

  test_cmd=(
    dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj
    --configuration Release
    --no-build
    --verbosity minimal
    --logger "trx;LogFileName=operator-cutover-api-tests.trx"
    --results-directory "${ARTIFACT_DIR_REL}"
  )
  if [[ -n "${TEST_FILTER}" ]]; then
    test_cmd+=(--filter "${TEST_FILTER}")
  fi

  set +e
  run_step api-tests "${test_cmd[@]}"
  api_status=$?
  set -e

  python3 - "${ARTIFACT_DIR}" "${REPORT}" <<'PY'
import pathlib
import sys
import xml.etree.ElementTree as ET

artifact_dir = pathlib.Path(sys.argv[1])
report = pathlib.Path(sys.argv[2])
trx_files = sorted(artifact_dir.glob("*.trx"))

with report.open("a", encoding="utf-8") as out:
    out.write("\n## API Test Failure Summary\n\n")
    if not trx_files:
        out.write("No TRX file was produced.\n")
        sys.exit(0)

    trx = trx_files[-1]
    root = ET.parse(trx).getroot()
    ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    counters = root.find(".//t:Counters", ns)
    if counters is not None:
        out.write(
            f"- Total: `{counters.get('total')}`\n"
            f"- Passed: `{counters.get('passed')}`\n"
            f"- Failed: `{counters.get('failed')}`\n"
            f"- Skipped/Inconclusive: `{counters.get('notExecuted')}`\n"
            f"- TRX: `{trx.relative_to(artifact_dir.parent.parent)}`\n\n"
        )

    failed = [
        r for r in root.findall(".//t:UnitTestResult", ns)
        if r.get("outcome") == "Failed"
    ]
    if not failed:
        out.write("No failed test cases were found in the TRX.\n")
        sys.exit(0)

    out.write("First failed tests:\n\n")
    for result in failed[:25]:
        name = result.get("testName", "<unknown>")
        msg = result.findtext(".//t:Message", default="", namespaces=ns).strip()
        msg = " ".join(msg.split())
        out.write(f"- `{name}`")
        if msg:
            out.write(f": {msg[:240]}")
        out.write("\n")
PY

  if [[ ${api_status} -ne 0 ]]; then
    OVERALL_STATUS=1
    log "API tests failed. See ${REPORT}"
  fi
fi

if [[ "${RUN_COMPOSE_SMOKE}" == "true" ]]; then
  export ADMIN_OPS_KEY="${ADMIN_OPS_KEY:-CHANGE_ME}"
  export SMOKE_ADMIN_EMAIL="${SMOKE_ADMIN_EMAIL:-smoke-admin@synaptix.local}"
  export SMOKE_ADMIN_PASSWORD="${SMOKE_ADMIN_PASSWORD:-SmokeTest123!}"
  export WAIT_TIMEOUT="${WAIT_TIMEOUT:-180}"

  set +e
  run_step compose-smoke bash scripts/compose-smoke.sh
  compose_status=$?
  set -e

  docker compose -f docker/compose.yml -f docker/compose.smoke.yml logs --tail=200 backend-api > "${ARTIFACT_DIR}/compose-backend-api.log" 2>&1 || true
  docker compose -f docker/compose.yml -f docker/compose.smoke.yml logs --tail=200 operator-dashboard > "${ARTIFACT_DIR}/compose-operator-dashboard.log" 2>&1 || true
  docker compose -f docker/compose.yml -f docker/compose.smoke.yml logs --tail=200 migration > "${ARTIFACT_DIR}/compose-migration.log" 2>&1 || true
  docker compose -f docker/compose.yml -f docker/compose.smoke.yml down -v --remove-orphans >/dev/null 2>&1 || true

  if [[ ${compose_status} -ne 0 ]]; then
    OVERALL_STATUS=1
    log "compose-smoke failed. Service logs were captured in ${ARTIFACT_DIR}"
  fi
fi

log "Diagnostics written to ${REPORT}"
exit "${OVERALL_STATUS}"
