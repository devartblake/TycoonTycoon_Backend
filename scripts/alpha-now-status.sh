#!/usr/bin/env bash
set -euo pipefail

# Alpha NOW gate status helper (backend-only)
# - Safe by default: does not start services or run heavy build unless RUN_BUILD=true.

RUN_BUILD="${RUN_BUILD:-false}"
RUN_ROUTE_SMOKE="${RUN_ROUTE_SMOKE:-true}"
RUN_PWSH_ROUTE_SMOKE="${RUN_PWSH_ROUTE_SMOKE:-false}"
BUILD_TARGET="${BUILD_TARGET:-Tycoon.Backend.Api/Tycoon.Backend.Api.csproj}"

pass_count=0
warn_count=0
fail_count=0

report() {
  local state="$1"
  local label="$2"
  local details="${3:-}"

  case "$state" in
    PASS) ((pass_count+=1)); printf 'PASS  | %s\n' "$label" ;;
    WARN) ((warn_count+=1)); printf 'WARN  | %s\n' "$label" ;;
    FAIL) ((fail_count+=1)); printf 'FAIL  | %s\n' "$label" ;;
    *)    printf 'INFO  | %s\n' "$label" ;;
  esac

  if [[ -n "$details" ]]; then
    printf '      | %s\n' "$details"
  fi
}

echo "Alpha NOW status (backend-only)"
echo "Date (UTC): $(date -u +'%Y-%m-%d %H:%M:%S')"
echo

if rg -n "GetExtensionMethod\\(this Type t, string methodName\\)" Tycoon.Shared/Core/Extensions/TypeExtensions.cs >/dev/null 2>&1; then
  report PASS "TypeExtensions compile-fix guard (methodName signature present)"
else
  report FAIL "TypeExtensions compile-fix guard (methodName signature missing)" "Expected fixed GetExtensionMethod signature not found."
fi

if command -v dotnet >/dev/null 2>&1; then
  report PASS "dotnet SDK available" "$(dotnet --version)"
  if [[ "$RUN_BUILD" == "true" ]]; then
    if dotnet build "$BUILD_TARGET" >/dev/null; then
      report PASS "Build gate (dotnet build $BUILD_TARGET)"
    else
      report FAIL "Build gate (dotnet build $BUILD_TARGET)" "See build output above."
    fi
  else
    report WARN "Build gate not executed" "Set RUN_BUILD=true to execute dotnet build for $BUILD_TARGET."
  fi
else
  report WARN "dotnet SDK unavailable" "Cannot run build/migration gates in this environment."
fi

if [[ "$RUN_ROUTE_SMOKE" == "true" ]]; then
  if SMOKE_MODE=routes bash ./scripts/alpha-p0-smoke.sh >/dev/null; then
    report PASS "Route smoke gate (SMOKE_MODE=routes bash ./scripts/alpha-p0-smoke.sh)"
  else
    report FAIL "Route smoke gate (SMOKE_MODE=routes bash ./scripts/alpha-p0-smoke.sh)"
  fi
else
  report WARN "Route smoke gate skipped" "Set RUN_ROUTE_SMOKE=true to execute."
fi

if [[ "$RUN_PWSH_ROUTE_SMOKE" == "true" ]]; then
  if command -v pwsh >/dev/null 2>&1; then
    if pwsh ./scripts/alpha-p0-smoke.ps1 -SmokeMode routes >/dev/null; then
      report PASS "PowerShell route smoke gate (pwsh ./scripts/alpha-p0-smoke.ps1 -SmokeMode routes)"
    else
      report FAIL "PowerShell route smoke gate (pwsh ./scripts/alpha-p0-smoke.ps1 -SmokeMode routes)"
    fi
  else
    report WARN "pwsh unavailable" "Cannot execute PowerShell route smoke gate in this environment."
  fi
else
  report WARN "PowerShell route smoke gate skipped" "Set RUN_PWSH_ROUTE_SMOKE=true to execute."
fi

echo
echo "Summary: PASS=$pass_count WARN=$warn_count FAIL=$fail_count"
if [[ "$fail_count" -gt 0 ]]; then
  exit 1
fi
