param(
    [ValidateSet("docker", "local")]
    [string]$Mode = "docker",

    [switch]$ResetDev,
    [switch]$RebuildElastic
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $repoRoot

function Import-DotEnv {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return
    }

    Get-Content $Path | ForEach-Object {
        $line = $_.Trim()
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            return
        }

        $equals = $line.IndexOf("=")
        if ($equals -lt 1) {
            return
        }

        $name = $line.Substring(0, $equals).Trim()
        $value = $line.Substring($equals + 1).Trim()
        $comment = $value.IndexOf(" #")
        if ($comment -ge 0) {
            $value = $value.Substring(0, $comment).Trim()
        }

        $value = $value.Trim('"').Trim("'")
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($name))) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

Import-DotEnv "docker/.env"

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
    if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__db)) {
        if ([string]::IsNullOrWhiteSpace($env:POSTGRES_DB)) { $env:POSTGRES_DB = "synaptix_db" }
        if ([string]::IsNullOrWhiteSpace($env:POSTGRES_USER)) { $env:POSTGRES_USER = "synaptix_user" }
        if ([string]::IsNullOrWhiteSpace($env:POSTGRES_PASSWORD)) { $env:POSTGRES_PASSWORD = "synaptix_password_123" }

        $postgresHost = $env:POSTGRES_HOST
        if ([string]::IsNullOrWhiteSpace($postgresHost)) { $postgresHost = "localhost" }

        $postgresPort = $env:POSTGRES_PORT
        if ([string]::IsNullOrWhiteSpace($postgresPort)) { $postgresPort = "5432" }

        $env:ConnectionStrings__db = "Host=$postgresHost;Port=$postgresPort;Database=$($env:POSTGRES_DB);Username=$($env:POSTGRES_USER);Password=$($env:POSTGRES_PASSWORD)"
    }

    if ([string]::IsNullOrWhiteSpace($env:SuperAdmin__Email) -and -not [string]::IsNullOrWhiteSpace($env:SUPER_ADMIN_EMAIL)) {
        $env:SuperAdmin__Email = $env:SUPER_ADMIN_EMAIL
    }
    if ([string]::IsNullOrWhiteSpace($env:SuperAdmin__Password) -and -not [string]::IsNullOrWhiteSpace($env:SUPER_ADMIN_PASSWORD)) {
        $env:SuperAdmin__Password = $env:SUPER_ADMIN_PASSWORD
    }
    if ([string]::IsNullOrWhiteSpace($env:SuperAdmin__Handle) -and -not [string]::IsNullOrWhiteSpace($env:SUPER_ADMIN_HANDLE)) {
        $env:SuperAdmin__Handle = $env:SUPER_ADMIN_HANDLE
    }

    dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
}
