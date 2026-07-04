#!/usr/bin/env pwsh
# Simple verification script for monitoring integration

$passed = 0
$failed = 0

function pass {
    param([string]$msg)
    Write-Host "[PASS] $msg" -ForegroundColor Green
    $script:passed++
}

function fail {
    param([string]$msg)
    Write-Host "[FAIL] $msg" -ForegroundColor Red
    $script:failed++
}

function info {
    param([string]$msg)
    Write-Host "[INFO] $msg" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=== Monitoring Integration Verification ===" -ForegroundColor Cyan
Write-Host ""

# Check files exist
info "Checking monitoring files..."
$files = @(
    "Synaptix.Monitoring\Synaptix.Monitoring.csproj",
    "Synaptix.Monitoring\DependencyInjectionExtensions.cs",
    "Synaptix.Monitoring\Jobs\HangfireMetricsCollector.cs",
    "Synaptix.Monitoring\Errors\ErrorRateTracker.cs",
    "Synaptix.Backend.Api\Features\Monitoring\MonitoringEndpoints.cs",
    "Synaptix.Backend.Api\Features\Monitoring\ErrorTrackingMiddleware.cs"
)

foreach ($f in $files) {
    if (Test-Path $f) { pass $f } else { fail $f }
}

# Check Program.cs integration
info "Checking Program.cs integration..."
$prog = Get-Content Synaptix.Backend.Api\Program.cs -Raw

if ($prog -match "using Synaptix.Monitoring") { pass "Import: Synaptix.Monitoring" } else { fail "Import: Synaptix.Monitoring" }
if ($prog -match "using Synaptix.Backend.Api.Features.Monitoring") { pass "Import: Monitoring endpoints" } else { fail "Import: Monitoring endpoints" }
if ($prog -match "AddMonitoring") { pass "DI: AddMonitoring()" } else { fail "DI: AddMonitoring()" }
if ($prog -match "UseErrorTracking") { pass "Middleware: UseErrorTracking()" } else { fail "Middleware: UseErrorTracking()" }
if ($prog -match "MapMonitoringEndpoints") { pass "Routes: MapMonitoringEndpoints()" } else { fail "Routes: MapMonitoringEndpoints()" }

# Check project reference
info "Checking project references..."
$csproj = Get-Content Synaptix.Backend.Api\Synaptix.Backend.Api.csproj -Raw
if ($csproj -match "Synaptix.Monitoring") { pass "Project ref: Synaptix.Monitoring" } else { fail "Project ref: Synaptix.Monitoring" }

# Check config files
info "Checking Prometheus and Grafana files..."
if (Test-Path "docker\monitoring\prometheus\rules\alert-rules.yml") { pass "Alert rules" } else { fail "Alert rules" }
if (Test-Path "docker\monitoring\grafana\provisioning\dashboards\hangfire-jobs.json") { pass "Hangfire dashboard" } else { fail "Hangfire dashboard" }
if (Test-Path "docker\monitoring\grafana\provisioning\dashboards\error-rates.json") { pass "Error rates dashboard" } else { fail "Error rates dashboard" }

# Check alert rules
info "Checking AlertManager rules..."
$alerts = Get-Content "docker\monitoring\prometheus\rules\alert-rules.yml" -Raw
$rules = @("HighJobQueueDepth", "CriticalJobQueueDepth", "HighJobFailureRate", "EndpointHighErrorRate", "ServerErrorSpike")
foreach ($r in $rules) {
    if ($alerts -match $r) { pass "Alert: $r" } else { fail "Alert: $r" }
}

# Try to build
info "Building backend..."
Push-Location Synaptix.Backend.Api
$build = dotnet build 2>&1
if ($LASTEXITCODE -eq 0) {
    pass "Backend builds successfully"
} else {
    fail "Backend build failed"
    $build | Select-Object -First 20 | Write-Host
}
Pop-Location

# Summary
Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Passed: $passed" -ForegroundColor Green
Write-Host "Failed: $failed" -ForegroundColor Red

if ($failed -eq 0) {
    Write-Host ""
    Write-Host "All checks passed! Integration is complete." -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "Some checks failed. Please review above." -ForegroundColor Red
    exit 1
}
