#!/bin/bash

# Comprehensive verification script for Hangfire Job Monitoring and Error Rate Tracking
# Usage: ./verify-monitoring.sh

set -e

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

TESTS_PASSED=0
TESTS_FAILED=0

print_header() {
    echo ""
    echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
    echo -e "${CYAN}  $1${NC}"
    echo -e "${CYAN}═══════════════════════════════════════════════════════════${NC}"
}

print_pass() {
    echo -e "${GREEN}✅ PASS: $1${NC}"
    ((TESTS_PASSED++))
}

print_fail() {
    echo -e "${RED}❌ FAIL: $1${NC}"
    ((TESTS_FAILED++))
}

print_warn() {
    echo -e "${YELLOW}⚠️  WARN: $1${NC}"
}

print_info() {
    echo -e "ℹ️  INFO: $1"
}

# ============================================================================
# PART 1: BUILD VERIFICATION
# ============================================================================

print_header "PART 1: Build Verification"

print_info "Checking if backend project exists..."
if [ -f "./Synaptix.Backend.Api/Synaptix.Backend.Api.csproj" ]; then
    print_pass "Backend project file found"
else
    print_fail "Backend project file not found"
    exit 1
fi

print_info "Checking if Synaptix.Monitoring project exists..."
if [ -f "./Synaptix.Monitoring/Synaptix.Monitoring.csproj" ]; then
    print_pass "Monitoring project file found"
else
    print_fail "Monitoring project file not found"
    exit 1
fi

print_info "Building backend API..."
cd Synaptix.Backend.Api
if dotnet build > /dev/null 2>&1; then
    print_pass "Backend builds successfully"
else
    print_fail "Backend build failed"
    dotnet build
    cd ..
    exit 1
fi
cd ..

# ============================================================================
# PART 2: CODE INTEGRATION VERIFICATION
# ============================================================================

print_header "PART 2: Code Integration Verification"

print_info "Checking Program.cs for monitoring imports..."
if grep -q "using Synaptix\.Monitoring" Synaptix.Backend.Api/Program.cs; then
    print_pass "Synaptix.Monitoring namespace imported"
else
    print_fail "Synaptix.Monitoring namespace not imported in Program.cs"
fi

if grep -q "using Synaptix\.Backend\.Api\.Features\.Monitoring" Synaptix.Backend.Api/Program.cs; then
    print_pass "Monitoring endpoints namespace imported"
else
    print_fail "Monitoring endpoints namespace not imported in Program.cs"
fi

print_info "Checking Program.cs for monitoring service registration..."
if grep -q "builder\.Services\.AddMonitoring" Synaptix.Backend.Api/Program.cs; then
    print_pass "Monitoring services registered"
else
    print_fail "Monitoring services not registered"
fi

print_info "Checking Program.cs for error tracking middleware..."
if grep -q "app\.UseErrorTracking" Synaptix.Backend.Api/Program.cs; then
    print_pass "Error tracking middleware registered"
else
    print_fail "Error tracking middleware not registered"
fi

print_info "Checking Program.cs for monitoring endpoints..."
if grep -q "app\.MapMonitoringEndpoints" Synaptix.Backend.Api/Program.cs; then
    print_pass "Monitoring endpoints mapped"
else
    print_fail "Monitoring endpoints not mapped"
fi

print_info "Checking .csproj for Synaptix.Monitoring reference..."
if grep -q "Synaptix\.Monitoring.*csproj" Synaptix.Backend.Api/Synaptix.Backend.Api.csproj; then
    print_pass "Synaptix.Monitoring project referenced"
else
    print_fail "Synaptix.Monitoring not referenced in .csproj"
fi

# ============================================================================
# PART 3: FILES VERIFICATION
# ============================================================================

print_header "PART 3: Monitoring Files Verification"

files=(
    "Synaptix.Monitoring/DependencyInjectionExtensions.cs"
    "Synaptix.Monitoring/Jobs/HangfireMetricsCollector.cs"
    "Synaptix.Monitoring/Errors/ErrorRateTracker.cs"
    "Synaptix.Backend.Api/Features/Monitoring/MonitoringEndpoints.cs"
    "Synaptix.Backend.Api/Features/Monitoring/ErrorTrackingMiddleware.cs"
    "Synaptix.Backend.Api/Features/Monitoring/HangfireJobFilter.cs"
)

for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        print_pass "Found: $file"
    else
        print_fail "Missing: $file"
    fi
done

# ============================================================================
# PART 4: PROMETHEUS & GRAFANA FILES
# ============================================================================

print_header "PART 4: Prometheus & Grafana Configuration Files"

config_files=(
    "docker/monitoring/prometheus/rules/alert-rules.yml"
    "docker/monitoring/grafana/provisioning/dashboards/hangfire-jobs.json"
    "docker/monitoring/grafana/provisioning/dashboards/error-rates.json"
)

for file in "${config_files[@]}"; do
    if [ -f "$file" ]; then
        print_pass "Found: $file"
        if [[ "$file" == *.json ]]; then
            if jq empty "$file" 2>/dev/null; then
                print_info "  ✓ Valid JSON structure"
            else
                print_warn "  ⚠ Invalid JSON: $file"
            fi
        fi
    else
        print_fail "Missing: $file"
    fi
done

# ============================================================================
# PART 5: ALERT RULES VERIFICATION
# ============================================================================

print_header "PART 5: AlertManager Alert Rules Verification"

print_info "Checking alert-rules.yml for Hangfire alerts..."

alerts=(
    "HighJobQueueDepth"
    "CriticalJobQueueDepth"
    "HighJobFailureRate"
    "JobFailureRateWarning"
    "LongJobProcessingTime"
    "HangfireServerDown"
    "EndpointHighErrorRate"
    "EndpointCriticalErrorRate"
    "ServerErrorSpike"
)

for alert in "${alerts[@]}"; do
    if grep -q "$alert" docker/monitoring/prometheus/rules/alert-rules.yml; then
        print_pass "Alert rule found: $alert"
    else
        print_fail "Alert rule missing: $alert"
    fi
done

# ============================================================================
# PART 6: DOCUMENTATION VERIFICATION
# ============================================================================

print_header "PART 6: Documentation Files"

doc_files=(
    "docs/MONITORING_HANGFIRE_ERRORS.md"
    "docs/MONITORING_INTEGRATION_GUIDE.md"
)

for file in "${doc_files[@]}"; do
    if [ -f "$file" ]; then
        lines=$(wc -l < "$file")
        print_pass "Found: $file ($lines lines)"
    else
        print_fail "Missing: $file"
    fi
done

# ============================================================================
# PART 7: RUNTIME VERIFICATION (if services are running)
# ============================================================================

print_header "PART 7: Runtime Verification (Optional - requires running backend)"

print_info "Checking if backend is running on port 5000..."
if timeout 2 bash -c "echo >/dev/tcp/localhost/5000" 2>/dev/null; then
    print_pass "Backend is running"

    print_info "Testing monitoring endpoints..."

    # Test job metrics endpoint
    if response=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/monitoring/jobs/metrics); then
        if [ "$response" = "200" ]; then
            print_pass "GET /monitoring/jobs/metrics returns 200"
            data=$(curl -s http://localhost:5000/monitoring/jobs/metrics)
            queue_depth=$(echo "$data" | jq -r '.queueDepth // empty')
            if [ -n "$queue_depth" ]; then
                print_info "  Queue depth: $queue_depth jobs"
            fi
        else
            print_warn "GET /monitoring/jobs/metrics returns $response"
        fi
    else
        print_warn "Could not reach /monitoring/jobs/metrics"
    fi

    # Test error summary endpoint
    if response=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/monitoring/errors/summary); then
        if [ "$response" = "200" ]; then
            print_pass "GET /monitoring/errors/summary returns 200"
            data=$(curl -s http://localhost:5000/monitoring/errors/summary)
            total_errors=$(echo "$data" | jq -r '.totalErrors // empty')
            if [ -n "$total_errors" ]; then
                print_info "  Total errors: $total_errors"
            fi
        else
            print_warn "GET /monitoring/errors/summary returns $response"
        fi
    else
        print_warn "Could not reach /monitoring/errors/summary"
    fi

    # Test Prometheus metrics
    print_info "Testing Prometheus metrics endpoint..."
    if metrics=$(curl -s http://localhost:5000/metrics); then
        if echo "$metrics" | grep -q "hangfire_jobs_enqueued"; then
            print_pass "Hangfire metrics present in /metrics"
        else
            print_warn "Hangfire metrics not found in /metrics yet (may need more time)"
        fi

        if echo "$metrics" | grep -q "http_requests_total"; then
            print_pass "HTTP request metrics present in /metrics"
        fi
    else
        print_warn "Could not reach /metrics"
    fi
else
    print_warn "Backend not running on port 5000 (this is optional)"
    print_info "To test runtime endpoints, start the backend with: dotnet run"
fi

# Check Prometheus
print_info "Checking if Prometheus is running..."
if timeout 2 bash -c "echo >/dev/tcp/localhost/9090" 2>/dev/null; then
    print_pass "Prometheus is running"
    print_info "  Use Prometheus UI at http://localhost:9090/targets to verify backend-api scrape target"
else
    print_warn "Prometheus not running on port 9090 (optional)"
    print_info "To start monitoring stack: docker compose --profile dev up"
fi

# Check Grafana
print_info "Checking if Grafana is running..."
if timeout 2 bash -c "echo >/dev/tcp/localhost/3000" 2>/dev/null; then
    print_pass "Grafana is running"
    print_info "  Access at http://localhost:3000 (admin/password)"
    print_info "  Look for dashboards: 'Hangfire Job Monitoring' and 'Error Rate Monitoring'"
else
    print_warn "Grafana not running on port 3000 (optional)"
fi

# Check AlertManager
print_info "Checking if AlertManager is running..."
if timeout 2 bash -c "echo >/dev/tcp/localhost/9093" 2>/dev/null; then
    print_pass "AlertManager is running"
    print_info "  Access at http://localhost:9093 to view alerts"
else
    print_warn "AlertManager not running on port 9093 (optional)"
fi

# ============================================================================
# SUMMARY
# ============================================================================

print_header "VERIFICATION SUMMARY"

TOTAL=$((TESTS_PASSED + TESTS_FAILED))
echo ""
echo "Total Tests: $TOTAL"
echo -e "${GREEN}Passed: $TESTS_PASSED${NC}"
echo -e "${RED}Failed: $TESTS_FAILED${NC}"

if [ $TESTS_FAILED -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ All tests passed! Monitoring integration is complete.${NC}"
    echo ""
    echo -e "${CYAN}Next steps:${NC}"
    echo "  1. Build and run the backend: dotnet run"
    echo "  2. Optionally start monitoring stack: docker compose --profile dev up"
    echo "  3. Access monitoring endpoints:"
    echo "     - Job metrics: curl http://localhost:5000/monitoring/jobs/metrics"
    echo "     - Error summary: curl http://localhost:5000/monitoring/errors/summary"
    echo "  4. Check Grafana dashboards at http://localhost:3000"
    exit 0
else
    echo ""
    echo -e "${RED}❌ Some tests failed. Please review the errors above.${NC}"
    exit 1
fi
