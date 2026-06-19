#Requires -Version 5.1
<#
.SYNOPSIS
    Build images, then bring up the stack so the one-shot setup + migration
    services run automatically, after validating the EF model snapshot.

.DESCRIPTION
    'docker compose build' only builds images — it never starts containers.
    This script wraps build + up so the ordered chain in docker/compose.yml
    (setup -> migration -> backend-api, gated by service_completed_successfully)
    actually executes.

    Flow:
      1. EF model snapshot drift check (dotnet ef has-pending-model-changes)
      2. docker compose build
      3. docker compose up -d  (runs setup, then migration, then API)

.PARAMETER NoSnapshot
    Skip the EF model snapshot drift check.

.PARAMETER NoBuild
    Skip 'docker compose build' (just bring the stack up).

.PARAMETER Reset
    Drop and recreate the database during migration.

.PARAMETER Dev
    Include dev-profile services (Grafana, pgAdmin, Kibana, Mongo Express, DBGate).

.EXAMPLE
    .\scripts\bootstrap-stack.ps1
    .\scripts\bootstrap-stack.ps1 -NoSnapshot
    .\scripts\bootstrap-stack.ps1 -Reset -Dev
#>
[CmdletBinding()]
param(
    [switch]$NoSnapshot,
    [switch]$NoBuild,
    [switch]$Reset,
    [switch]$Dev
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root        = Split-Path -Parent $PSScriptRoot
$ComposeFile = Join-Path $Root 'docker\compose.yml'

function Write-Step { param([string]$Msg) Write-Host "`n==> $Msg" -ForegroundColor Cyan }
function Write-Ok   { param([string]$Msg) Write-Host "  [OK]   $Msg" -ForegroundColor Green }
function Write-Fail { param([string]$Msg) Write-Host "  [FAIL] $Msg" -ForegroundColor Red; exit 1 }

# docker compose global args (--profile must precede the subcommand)
$globalArgs = @('-f', $ComposeFile)
if ($Dev) { $globalArgs += '--profile', 'dev' }

# Step 1: EF model snapshot drift check
if (-not $NoSnapshot) {
    Write-Step "Validating EF model snapshot for drift"
    & dotnet ef migrations has-pending-model-changes `
        --project (Join-Path $Root 'Synaptix.Backend.Migrations\Synaptix.Backend.Migrations.csproj') `
        --startup-project (Join-Path $Root 'Synaptix.MigrationService\Synaptix.MigrationService.csproj') `
        --context AppDb
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "EF model snapshot is out of sync. Add a migration and re-run, or pass -NoSnapshot to skip."
    }
    Write-Ok "EF model snapshot is in sync."
} else {
    Write-Step "Skipping EF model snapshot check (-NoSnapshot)"
}

# Step 2: Build images
if (-not $NoBuild) {
    Write-Step "Building Docker images"
    & docker compose @globalArgs build
    if ($LASTEXITCODE -ne 0) { Write-Fail "docker compose build failed." }
    Write-Ok "Images built."
} else {
    Write-Step "Skipping image build (-NoBuild)"
}

# Step 3: Bring up the stack (setup -> migration -> backend-api)
if ($Reset) {
    Write-Step "Reset mode enabled — database will be dropped and recreated during migration"
    $env:MIGRATION_RESET_DATABASE = 'true'
}

Write-Step "Bringing up the stack — setup and migration run automatically before the API"
& docker compose @globalArgs up -d
if ($LASTEXITCODE -ne 0) { Write-Fail "docker compose up failed." }

Write-Ok "Done. Tail progress with: docker compose -f docker/compose.yml logs -f setup migration"
