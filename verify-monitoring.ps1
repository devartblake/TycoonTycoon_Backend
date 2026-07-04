#!/usr/bin/env pwsh

<#
.SYNOPSIS
Comprehensive verification script for Hangfire Job Monitoring and Error Rate Tracking

.DESCRIPTION
Tests the integration of monitoring services including:
- Backend API build
- Monitoring endpoints
- Prometheus metrics collection
- Grafana dashboard availability
- AlertManager alert rules
- Error tracking middleware

.EXAMPLE
.\verify-monitoring.ps1

.EXAMPLE
.\verify-monitoring.ps1 -Verbose
#>

param(
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$WarningPreference = "Continue"

# Colors for output
$Green = [System.ConsoleColor]::Green
$Red = [System.ConsoleColor]::Red
$Yellow = [System.ConsoleColor]::Yellow
$Cyan = [System.ConsoleColor]::Cyan
$White = [System.ConsoleColor]::White

$testsPassed = 0
$testsFailed = 0

function Write-TestHeader {
    param([string]$Title)
    Write-Host "`n" -NoNewline
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $Cyan
    Write-Host "  $Title" -ForegroundColor $Cyan
    Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor $Cyan
}

function Write-TestPass {
    param([string]$Message)
    Write-Host "✅ PASS: $Message" -ForegroundColor $Green
    $script:testsPassed++
}

function Write-TestFail {
    param([string]$Message)
    Write-Host "❌ FAIL: $Message" -ForegroundColor $Red
    $script:testsFailed++
}

function Write-TestWarn {
    param([string]$Message)
    Write-Host "⚠️  WARN: $Message" -ForegroundColor $Yellow
}

function Write-TestInfo {
    param([string]$Message)
    Write-Host "ℹ️  INFO: $Message" -ForegroundColor $White
}

# ============================================================================
# PART 1: BUILD VERIFICATION
# ============================================================================

Write-TestHeader "PART 1: Build Verification"

Write-TestInfo "Checking if backend project exists..."
if (Test-Path ".\Synaptix.Backend.Api\Synaptix.Backend.Api.csproj") {
    Write-TestPass "Backend project file found"
} else {
    Write-TestFail "Backend project file not found"
    exit 1
}

Write-TestInfo "Checking if Synaptix.Monitoring project exists..."
if (Test-Path ".\Synaptix.Monitoring\Synaptix.Monitoring.csproj") {
    Write-TestPass "Monitoring project file found"
} else {
    Write-TestFail "Monitoring project file not found"
    exit 1
}

Write-TestInfo "Building backend API..."
Push-Location ".\Synaptix.Backend.Api"
try {
    $buildOutput = dotnet build 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-TestPass "Backend builds successfully"
    } else {
        Write-TestFail "Backend build failed"
        Write-Host $buildOutput
        Pop-Location
        exit 1
    }
} catch {
    Write-TestFail "Build command failed: $_"
    Pop-Location
    exit 1
}
Pop-Location

# ============================================================================
# PART 2: CODE INTEGRATION VERIFICATION
# ============================================================================

Write-TestHeader "PART 2: Code Integration Verification"

Write-TestInfo "Checking Program.cs for monitoring imports..."
$programContent = Get-Content ".\Synaptix.Backend.Api\Program.cs" -Raw
if ($programContent -match "using Synaptix\.Monitoring") {
    Write-TestPass "Synaptix.Monitoring namespace imported"
} else {
    Write-TestFail "Synaptix.Monitoring namespace not imported in Program.cs"
}

if ($programContent -match "using Synaptix\.Backend\.Api\.Features\.Monitoring") {
    Write-TestPass "Monitoring endpoints namespace imported"
} else {
    Write-TestFail "Monitoring endpoints namespace not imported in Program.cs"
}

Write-TestInfo "Checking Program.cs for monitoring service registration..."
if ($programContent -match "builder\.Services\.AddMonitoring") {
    Write-TestPass "Monitoring services registered"
} else {
    Write-TestFail "Monitoring services not registered"
}

Write-TestInfo "Checking Program.cs for error tracking middleware..."
if ($programContent -match "app\.UseErrorTracking") {
    Write-TestPass "Error tracking middleware registered"
} else {
    Write-TestFail "Error tracking middleware not registered"
}

Write-TestInfo "Checking Program.cs for monitoring endpoints..."
if ($programContent -match "app\.MapMonitoringEndpoints") {
    Write-TestPass "Monitoring endpoints mapped"
} else {
    Write-TestFail "Monitoring endpoints not mapped"
}

Write-TestInfo "Checking .csproj for Synaptix.Monitoring reference..."
$projContent = Get-Content ".\Synaptix.Backend.Api\Synaptix.Backend.Api.csproj" -Raw
if ($projContent -match 'Synaptix\.Monitoring.*csproj') {
    Write-TestPass "Synaptix.Monitoring project referenced"
} else {
    Write-TestFail "Synaptix.Monitoring not referenced in .csproj"
}

# ============================================================================
# PART 3: FILES VERIFICATION
# ============================================================================

Write-TestHeader "PART 3: Monitoring Files Verification"

$monitoringFiles = @(
    "Synaptix.Monitoring\DependencyInjectionExtensions.cs",
    "Synaptix.Monitoring\Jobs\HangfireMetricsCollector.cs",
    "Synaptix.Monitoring\Errors\ErrorRateTracker.cs",
    "Synaptix.Backend.Api\Features\Monitoring\MonitoringEndpoints.cs",
    "Synaptix.Backend.Api\Features\Monitoring\ErrorTrackingMiddleware.cs",
    "Synaptix.Backend.Api\Features\Monitoring\HangfireJobFilter.cs"
)

foreach ($file in $monitoringFiles) {
    if (Test-Path $file) {
        Write-TestPass "Found: $file"
    } else {
        Write-TestFail "Missing: $file"
    }
}

# ============================================================================
# PART 4: PROMETHEUS & GRAFANA FILES
# ============================================================================

Write-TestHeader "PART 4: Prometheus & Grafana Configuration Files"

$configFiles = @(
    "docker\monitoring\prometheus\rules\alert-rules.yml",
    "docker\monitoring\grafana\provisioning\dashboards\hangfire-jobs.json",
    "docker\monitoring\grafana\provisioning\dashboards\error-rates.json"
)

foreach ($file in $configFiles) {
    if (Test-Path $file) {
        Write-TestPass "Found: $file"
        # Validate JSON if applicable
        if ($file -match "\.json$") {
            try {
                $content = Get-Content $file -Raw
                $json = $content | ConvertFrom-Json -ErrorAction Stop
                Write-TestInfo "  ✓ Valid JSON structure"
            } catch {
                Write-TestWarn "  ⚠ Invalid JSON: $_"
            }
        }
    } else {
        Write-TestFail "Missing: $file"
    }
}

# ============================================================================
# PART 5: ALERT RULES VERIFICATION
# ============================================================================

Write-TestHeader "PART 5: AlertManager Alert Rules Verification"

Write-TestInfo "Checking alert-rules.yml for Hangfire alerts..."
$alertRules = Get-Content "docker\monitoring\prometheus\rules\alert-rules.yml" -Raw

$alertsToCheck = @(
    @{name = "HighJobQueueDepth"; pattern = "HighJobQueueDepth"},
    @{name = "CriticalJobQueueDepth"; pattern = "CriticalJobQueueDepth"},
    @{name = "HighJobFailureRate"; pattern = "HighJobFailureRate"},
    @{name = "JobFailureRateWarning"; pattern = "JobFailureRateWarning"},
    @{name = "LongJobProcessingTime"; pattern = "LongJobProcessingTime"},
    @{name = "HangfireServerDown"; pattern = "HangfireServerDown"},
    @{name = "EndpointHighErrorRate"; pattern = "EndpointHighErrorRate"},
    @{name = "EndpointCriticalErrorRate"; pattern = "EndpointCriticalErrorRate"},
    @{name = "ServerErrorSpike"; pattern = "ServerErrorSpike"}
)

foreach ($alert in $alertsToCheck) {
    if ($alertRules -match $alert.pattern) {
        Write-TestPass "Alert rule found: $($alert.name)"
    } else {
        Write-TestFail "Alert rule missing: $($alert.name)"
    }
}

# ============================================================================
# PART 6: DOCUMENTATION VERIFICATION
# ============================================================================

Write-TestHeader "PART 6: Documentation Files"

$docFiles = @(
    "docs\MONITORING_HANGFIRE_ERRORS.md",
    "docs\MONITORING_INTEGRATION_GUIDE.md"
)

foreach ($file in $docFiles) {
    if (Test-Path $file) {
        $lineCount = @(Get-Content $file).Count
        Write-TestPass "Found: $file ($lineCount lines)"
    } else {
        Write-TestFail "Missing: $file"
    }
}

# ============================================================================
# PART 7: RUNTIME VERIFICATION (if Docker is running)
# ============================================================================

Write-TestHeader "PART 7: Runtime Verification (Optional - requires running backend)"

Write-TestInfo "Checking if backend is running on port 5000..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -Method Get -TimeoutSec 2 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-TestPass "Backend is running and responding"

        # Test monitoring endpoints
        Write-TestInfo "Testing monitoring endpoints..."

        try {
            $jobMetrics = Invoke-WebRequest -Uri "http://localhost:5000/monitoring/jobs/metrics" -Method Get -TimeoutSec 2 -ErrorAction Stop
            if ($jobMetrics.StatusCode -eq 200) {
                Write-TestPass "GET /monitoring/jobs/metrics returns 200"
                $jobData = $jobMetrics.Content | ConvertFrom-Json
                Write-TestInfo "  Queue depth: $($jobData.queueDepth) jobs"
            }
        } catch {
            Write-TestWarn "Could not reach /monitoring/jobs/metrics: $_"
        }

        try {
            $errorSummary = Invoke-WebRequest -Uri "http://localhost:5000/monitoring/errors/summary" -Method Get -TimeoutSec 2 -ErrorAction Stop
            if ($errorSummary.StatusCode -eq 200) {
                Write-TestPass "GET /monitoring/errors/summary returns 200"
                $errorData = $errorSummary.Content | ConvertFrom-Json
                Write-TestInfo "  Total errors: $($errorData.totalErrors)"
                Write-TestInfo "  Error rate: $($errorData.averageErrorRate * 100)%"
            }
        } catch {
            Write-TestWarn "Could not reach /monitoring/errors/summary: $_"
        }

        # Test Prometheus metrics
        Write-TestInfo "Testing Prometheus metrics endpoint..."
        try {
            $metrics = Invoke-WebRequest -Uri "http://localhost:5000/metrics" -Method Get -TimeoutSec 2 -ErrorAction Stop
            $content = $metrics.Content

            if ($content -match "hangfire_jobs_enqueued") {
                Write-TestPass "Hangfire metrics present in /metrics"
            } else {
                Write-TestWarn "Hangfire metrics not found in /metrics yet (may need more time)"
            }

            if ($content -match "http_requests_total") {
                Write-TestPass "HTTP request metrics present in /metrics"
            }
        } catch {
            Write-TestWarn "Could not reach /metrics: $_"
        }
    }
} catch {
    Write-TestWarn "Backend not running on port 5000 (this is optional) - $($_.Exception.Message)"
    Write-TestInfo "To test runtime endpoints, start the backend with: dotnet run"
}

Write-TestInfo "Checking if Prometheus is running..."
try {
    $promResponse = Invoke-WebRequest -Uri "http://localhost:9090" -Method Get -TimeoutSec 2 -ErrorAction Stop
    if ($promResponse.StatusCode -eq 200) {
        Write-TestPass "Prometheus is running"

        Write-TestInfo "Checking Prometheus targets..."
        try {
            $targets = Invoke-WebRequest -Uri "http://localhost:9090/api/v1/targets" -Method Get -TimeoutSec 2 -ErrorAction Stop
            Write-TestInfo "  Use Prometheus UI at http://localhost:9090/targets to verify backend-api scrape target"
        } catch {
            Write-TestInfo "  Could not query targets API"
        }
    }
} catch {
    Write-TestWarn "Prometheus not running on port 9090 (optional) - $($_.Exception.Message)"
    Write-TestInfo "To start monitoring stack: docker compose --profile dev up"
}

Write-TestInfo "Checking if Grafana is running..."
try {
    $grafanaResponse = Invoke-WebRequest -Uri "http://localhost:3000" -Method Get -TimeoutSec 2 -ErrorAction Stop
    if ($grafanaResponse.StatusCode -eq 200) {
        Write-TestPass "Grafana is running"
        Write-TestInfo "  Access at http://localhost:3000 (admin/password)"
        Write-TestInfo "  Look for dashboards: 'Hangfire Job Monitoring' and 'Error Rate Monitoring'"
    }
} catch {
    Write-TestWarn "Grafana not running on port 3000 (optional) - $($_.Exception.Message)"
}

Write-TestInfo "Checking if AlertManager is running..."
try {
    $alertResponse = Invoke-WebRequest -Uri "http://localhost:9093" -Method Get -TimeoutSec 2 -ErrorAction Stop
    if ($alertResponse.StatusCode -eq 200) {
        Write-TestPass "AlertManager is running"
        Write-TestInfo "  Access at http://localhost:9093 to view alerts"
    }
} catch {
    Write-TestWarn "AlertManager not running on port 9093 (optional)"
}

# ============================================================================
# SUMMARY
# ============================================================================

Write-TestHeader "VERIFICATION SUMMARY"

$totalTests = $testsPassed + $testsFailed
Write-Host "`nTotal Tests: $totalTests"
Write-Host "Passed: $testsPassed" -ForegroundColor $Green
Write-Host "Failed: $testsFailed" -ForegroundColor $Red

if ($testsFailed -eq 0) {
    Write-Host "`n✅ All tests passed! Monitoring integration is complete." -ForegroundColor $Green
    Write-Host "`nNext steps:" -ForegroundColor $Cyan
    Write-Host "  1. Build and run the backend: dotnet run"
    Write-Host "  2. Optionally start monitoring stack: docker compose --profile dev up"
    Write-Host "  3. Access monitoring endpoints:"
    Write-Host "     - Job metrics: curl http://localhost:5000/monitoring/jobs/metrics"
    Write-Host "     - Error summary: curl http://localhost:5000/monitoring/errors/summary"
    Write-Host "  4. Check Grafana dashboards at http://localhost:3000"
    exit 0
} else {
    Write-Host "`n❌ Some tests failed. Please review the errors above." -ForegroundColor $Red
    exit 1
}
