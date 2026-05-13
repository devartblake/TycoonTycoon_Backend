param(
    [ValidateSet("docker", "local")]
    [string]$Mode = "docker",

    [switch]$ResetDev,
    [switch]$RebuildElastic
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

if ($RebuildElastic) {
    $env:MIGRATION_MODE = "MigrateSeedAndRebuildElastic"
} elseif ([string]::IsNullOrWhiteSpace($env:MIGRATION_MODE)) {
    $env:MIGRATION_MODE = "MigrateAndSeed"
}

if ([string]::IsNullOrWhiteSpace($env:MIGRATION_SEED_SOURCE)) { $env:MIGRATION_SEED_SOURCE = "Auto" }
if ([string]::IsNullOrWhiteSpace($env:MIGRATION_DASHBOARD_READINESS_ENABLED)) { $env:MIGRATION_DASHBOARD_READINESS_ENABLED = "true" }
if ([string]::IsNullOrWhiteSpace($env:MIGRATION_DASHBOARD_READINESS_STRICT)) { $env:MIGRATION_DASHBOARD_READINESS_STRICT = "true" }
if ($RebuildElastic) {
    $env:REBUILD_ELASTIC = "true"
} elseif ([string]::IsNullOrWhiteSpace($env:REBUILD_ELASTIC)) {
    $env:REBUILD_ELASTIC = "false"
}

if ($ResetDev) {
    $env:MIGRATION_RESET_DATABASE = "true"
    $env:MIGRATION_ALLOW_ENSURE_CREATED = "true"
}

if ($Mode -eq "docker") {
    if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) { $env:ASPNETCORE_ENVIRONMENT = "Development" }
    docker compose -f docker/compose.yml run --rm migration
} else {
    if ([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)) { $env:ASPNETCORE_ENVIRONMENT = "Local" }
    dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
}
