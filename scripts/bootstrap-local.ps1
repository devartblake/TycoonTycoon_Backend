#Requires -Version 5.1
<#
.SYNOPSIS
    Bootstrap the Synaptix backend for local development.

.DESCRIPTION
    Generates secrets, starts infrastructure, provisions services, runs migrations,
    and starts the full backend stack. Run once after cloning; safe to re-run.

.PARAMETER SkipInfra
    Skip docker compose up (use if infrastructure is already running).

.PARAMETER SkipMigration
    Skip running the migration service.

.PARAMETER Force
    Regenerate docker/.env even if it already exists.

.PARAMETER DevTools
    Include dev-profile services (Grafana, pgAdmin, Kibana, Mongo Express, DBGate).

.EXAMPLE
    .\scripts\bootstrap-local.ps1
    .\scripts\bootstrap-local.ps1 -SkipInfra
    .\scripts\bootstrap-local.ps1 -Force -DevTools
#>
[CmdletBinding()]
param(
    [switch]$SkipInfra,
    [switch]$SkipMigration,
    [switch]$Force,
    [switch]$DevTools
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot

function Write-Step { param([string]$Msg) Write-Host "`n==> $Msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Msg) Write-Host "  [OK]   $Msg" -ForegroundColor Green }
function Write-Fail { param([string]$Msg) Write-Host "  [FAIL] $Msg" -ForegroundColor Red; exit 1 }
function Write-Warn { param([string]$Msg) Write-Host "  [!!]   $Msg" -ForegroundColor Yellow }

Write-Host ""
Write-Host "+==========================================+" -ForegroundColor Magenta
Write-Host "|  Synaptix Backend - Local Bootstrap      |" -ForegroundColor Magenta
Write-Host "+==========================================+" -ForegroundColor Magenta
Write-Host ""

# Step 1: Generate secrets / init .env
Write-Step "Generating docker/.env with secure secrets"
$setupArgs = @('run', '--project', "$Root\Synaptix.Setup", '--', 'init-local')
if ($Force) { $setupArgs += '--force' }
& dotnet @setupArgs
if ($LASTEXITCODE -ne 0) { Write-Fail "init-local failed." }
Write-Ok "docker/.env generated."

# Step 2: Validate secrets
Write-Step "Validating generated secrets"
& dotnet run --project "$Root\Synaptix.Setup" -- validate --local
if ($LASTEXITCODE -ne 0) { Write-Fail "Validation failed. Check docker/.env." }
Write-Ok "Secrets validated."

# Step 3: Start infrastructure
if (-not $SkipInfra) {
    Write-Step "Starting infrastructure services"
    # --profile is a global docker compose flag and must precede the subcommand.
    $globalArgs  = @('-f', "$Root\docker\compose.yml")
    if ($DevTools) { $globalArgs += '--profile', 'dev' }

    $services = @('postgres', 'mongodb', 'redis', 'rabbitmq', 'minio', 'elasticsearch')
    if ($DevTools) { $services += 'grafana', 'prometheus', 'kibana', 'pgadmin', 'mongo-express', 'dbgate' }

    & docker compose @globalArgs up -d @services
    if ($LASTEXITCODE -ne 0) { Write-Fail "docker compose up failed." }
    Write-Ok "Infrastructure started."

    # Wait for health checks
    Write-Warn "Waiting 15 seconds for services to become healthy..."
    Start-Sleep -Seconds 15
}

# Step 4: Provision services (MinIO bucket, RabbitMQ vhost, MongoDB user, etc.)
Write-Step "Provisioning infrastructure services"
& dotnet run --project "$Root\Synaptix.Setup" -- provision-services
if ($LASTEXITCODE -gt 1) { Write-Fail "provision-services failed critically." }
Write-Ok "Services provisioned."

# Step 5: Run EF migrations + seeders
if (-not $SkipMigration) {
    Write-Step "Running database migrations and seeding"
    & dotnet run --project "$Root\Synaptix.MigrationService"
    if ($LASTEXITCODE -ne 0) { Write-Fail "MigrationService failed." }
    Write-Ok "Migrations and seeding complete."
}

# Step 6: Start remaining services
Write-Step "Starting backend API and dashboards"
& docker compose -f "$Root\docker\compose.yml" up -d backend-api operator-dashboard
if ($LASTEXITCODE -ne 0) { Write-Warn "Some services may have failed to start - check docker ps." }

# Step 7: Status
Write-Step "Bootstrap status"
& dotnet run --project "$Root\Synaptix.Setup" -- status

Write-Host ""
Write-Host "+==========================================+" -ForegroundColor Green
Write-Host "|  Bootstrap complete!                     |" -ForegroundColor Green
Write-Host "|                                          |" -ForegroundColor Green
Write-Host "|  API:       http://localhost:5000        |" -ForegroundColor Green
Write-Host "|  Dashboard: http://localhost:8200        |" -ForegroundColor Green
Write-Host "|  MinIO:     http://localhost:9001        |" -ForegroundColor Green
Write-Host "+==========================================+" -ForegroundColor Green
Write-Host ""
Write-Host "Super admin credentials: .local\bootstrap\super-admin.local.txt" -ForegroundColor Yellow
Write-Host ""
