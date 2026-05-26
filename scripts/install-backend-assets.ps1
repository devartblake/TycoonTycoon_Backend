param(
    [Parameter(Mandatory = $true)]
    [string]$BundleRoot,

    [string]$FrontendAssetsRoot,

    [ValidateSet("docker", "local")]
    [string]$Mode = "docker",

    [switch]$RunMigration,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Import-DotEnv {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Get-Content -LiteralPath $Path | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#") -or -not $line.Contains("=")) {
            return
        }

        $parts = $line.Split("=", 2)
        $name = $parts[0].Trim()
        $value = $parts[1].Trim()
        $commentIndex = $value.IndexOf(" #")
        if ($commentIndex -ge 0) {
            $value = $value.Substring(0, $commentIndex).Trim()
        }
        $value = $value.Trim('"').Trim("'")
        if ($name) {
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
        }
    }
}

function Resolve-RequiredPath {
    param(
        [string]$BasePath,
        [string]$RelativePath
    )

    $candidate = if ([System.IO.Path]::IsPathRooted($RelativePath)) {
        $RelativePath
    }
    else {
        Join-Path $BasePath $RelativePath
    }

    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "Installer manifest references missing file: $RelativePath"
    }

    (Resolve-Path -LiteralPath $candidate).Path
}

function ConvertTo-ObjectKey {
    param([string]$Value)
    $Value.Replace("\", "/").TrimStart("/")
}

function Get-ContentType {
    param(
        [string]$Path,
        [string]$Override
    )

    if ($Override) {
        return $Override
    }

    switch ([System.IO.Path]::GetExtension($Path).ToLowerInvariant()) {
        ".json" { "application/json"; break }
        ".jsonl" { "application/x-ndjson"; break }
        ".glb" { "model/gltf-binary"; break }
        ".gltf" { "model/gltf+json"; break }
        ".fbx" { "application/octet-stream"; break }
        ".obj" { "text/plain"; break }
        ".mtl" { "text/plain"; break }
        ".zip" { "application/zip"; break }
        ".mp3" { "audio/mpeg"; break }
        ".wav" { "audio/wav"; break }
        ".ogg" { "audio/ogg"; break }
        ".m4a" { "audio/mp4"; break }
        ".png" { "image/png"; break }
        ".jpg" { "image/jpeg"; break }
        ".jpeg" { "image/jpeg"; break }
        ".webp" { "image/webp"; break }
        ".gif" { "image/gif"; break }
        ".svg" { "image/svg+xml"; break }
        ".mp4" { "video/mp4"; break }
        ".webm" { "video/webm"; break }
        ".css" { "text/css"; break }
        ".js" { "text/javascript"; break }
        ".html" { "text/html"; break }
        ".txt" { "text/plain"; break }
        ".frag" { "text/plain"; break }
        default { "application/octet-stream"; break }
    }
}

function Get-InferredAssetKey {
    param([string]$Source)

    $fileName = [System.IO.Path]::GetFileName($Source)
    switch ([System.IO.Path]::GetExtension($Source).ToLowerInvariant()) {
        { @(".glb", ".gltf") -contains $_ } { "avatars/$fileName"; break }
        ".zip" { "avatar-packages/$fileName"; break }
        { @(".mp3", ".wav", ".ogg", ".m4a") -contains $_ } { "songs/$fileName"; break }
        { @(".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg") -contains $_ } { "images/$fileName"; break }
        { @(".mp4", ".webm") -contains $_ } { "videos/$fileName"; break }
        default { "frontend/$fileName"; break }
    }
}

function Add-UploadItem {
    param(
        [System.Collections.Generic.List[object]]$Items,
        [string]$Source,
        [string]$Key,
        [string]$ContentType
    )

    $info = Get-Item -LiteralPath $Source
    $Items.Add([pscustomobject]@{
        Source = $Source
        Key = ConvertTo-ObjectKey $Key
        ContentType = $ContentType
        Bytes = $info.Length
    })
}

function Invoke-Mc {
    param([string[]]$Arguments)
    & mc @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "mc failed with exit code ${LASTEXITCODE}: $($Arguments -join ' ')"
    }
}

$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
Import-DotEnv -Path (Join-Path $repoRoot "docker/.env")

$bundlePath = (Resolve-Path -LiteralPath $BundleRoot).Path
$manifestPath = Join-Path $bundlePath "installer.manifest.json"
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Missing installer manifest: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$items = [System.Collections.Generic.List[object]]::new()
$seedKeyMap = @{
    storeItems = "seeds/store-items.json"
    skillNodes = "seeds/skill-nodes.json"
    seasonRewards = "seeds/season-rewards.json"
    questions = "seeds/questions.json"
}

if ($manifest.seeds) {
    foreach ($property in $manifest.seeds.PSObject.Properties) {
        $seedName = $property.Name
        $seedValue = $property.Value
        $source = if ($seedValue -is [string]) { $seedValue } else { $seedValue.source }
        if (-not $source) {
            throw "Seed '$seedName' is missing a source path."
        }

        $key = if ($seedValue -isnot [string] -and $seedValue.key) {
            $seedValue.key
        }
        elseif ($seedKeyMap.ContainsKey($seedName)) {
            $seedKeyMap[$seedName]
        }
        else {
            "seeds/$([System.IO.Path]::GetFileName($source))"
        }

        $resolvedSource = Resolve-RequiredPath -BasePath $bundlePath -RelativePath $source
        $contentType = if ($seedValue -isnot [string]) { Get-ContentType -Path $resolvedSource -Override $seedValue.contentType } else { Get-ContentType -Path $resolvedSource -Override $null }
        Add-UploadItem -Items $items -Source $resolvedSource -Key $key -ContentType $contentType
    }
}

if ($manifest.assets) {
    foreach ($asset in $manifest.assets) {
        if (-not $asset.source) {
            throw "Asset entry is missing a source path."
        }

        $resolvedSource = Resolve-RequiredPath -BasePath $bundlePath -RelativePath $asset.source
        $key = if ($asset.key) { $asset.key } else { Get-InferredAssetKey -Source $asset.source }
        $contentType = Get-ContentType -Path $resolvedSource -Override $asset.contentType
        Add-UploadItem -Items $items -Source $resolvedSource -Key $key -ContentType $contentType
    }
}

if ($FrontendAssetsRoot) {
    $frontendRoot = (Resolve-Path -LiteralPath $FrontendAssetsRoot).Path
    $frontendRootPrefix = $frontendRoot.TrimEnd("\", "/") + [System.IO.Path]::DirectorySeparatorChar
    Get-ChildItem -LiteralPath $frontendRoot -File -Recurse | ForEach-Object {
        $relative = $_.FullName.Substring($frontendRootPrefix.Length)
        $key = ConvertTo-ObjectKey "frontend/$relative"
        $contentType = Get-ContentType -Path $_.FullName -Override $null
        Add-UploadItem -Items $items -Source $_.FullName -Key $key -ContentType $contentType
    }
}

$deduped = @($items | Group-Object Key | ForEach-Object {
    if ($_.Count -gt 1) {
        throw "Duplicate MinIO object key in install set: $($_.Name)"
    }
    $_.Group[0]
})

$endpoint = $env:MINIO_INSTALL_ENDPOINT
if (-not $endpoint) {
    $endpoint = $env:MINIO_ENDPOINT
}
if (-not $endpoint) {
    $endpoint = "http://localhost:9000"
}

$bucket = $env:MINIO_BUCKET
if (-not $bucket) {
    $bucket = $env:OBJECT_STORAGE_BUCKET
}
if (-not $bucket) {
    $bucket = "synaptix-assets"
}

$accessKey = $env:MINIO_ROOT_USER
if (-not $accessKey) {
    $accessKey = $env:MINIO_ACCESS_KEY
}

$secretKey = $env:MINIO_ROOT_PASSWORD
if (-not $secretKey) {
    $secretKey = $env:MINIO_SECRET_KEY
}

if (-not $accessKey -or -not $secretKey) {
    throw "MinIO credentials are not configured. Set MINIO_ROOT_USER/MINIO_ROOT_PASSWORD or MINIO_ACCESS_KEY/MINIO_SECRET_KEY."
}

$totalBytes = ($deduped | Measure-Object -Property Bytes -Sum).Sum
Write-Host "Backend asset installer"
Write-Host "Bundle: $bundlePath"
Write-Host "Endpoint: $endpoint"
Write-Host "Bucket: $bucket"
Write-Host "Objects: $($deduped.Count)"
Write-Host "Bytes: $totalBytes"

if ($DryRun) {
    foreach ($item in $deduped) {
        Write-Host "[dry-run] $($item.Source) -> s3://$bucket/$($item.Key) ($($item.ContentType), $($item.Bytes) bytes)"
    }
    if ($RunMigration) {
        Write-Host "[dry-run] Would run migration service with MIGRATION_SEED_SOURCE=MinIO."
    }
    exit 0
}

if (-not (Get-Command mc -ErrorAction SilentlyContinue)) {
    throw "MinIO client 'mc' is required for uploads. Install it or run a dry-run with -DryRun."
}

$alias = "synaptix-installer"
Invoke-Mc -Arguments @("alias", "set", $alias, $endpoint, $accessKey, $secretKey)
Invoke-Mc -Arguments @("mb", "--ignore-existing", "$alias/$bucket")

$uploaded = 0
$failed = 0
foreach ($item in $deduped) {
    try {
        Invoke-Mc -Arguments @("cp", "--attr", "Content-Type=$($item.ContentType)", $item.Source, "$alias/$bucket/$($item.Key)")
        $uploaded++
    }
    catch {
        $failed++
        Write-Error $_
    }
}

$migrationResult = "not-run"
if ($RunMigration) {
    $env:MIGRATION_SEED_SOURCE = "MinIO"
    $bootstrap = Join-Path $repoRoot "scripts/run-dashboard-bootstrap.ps1"
    if (-not (Test-Path -LiteralPath $bootstrap -PathType Leaf)) {
        throw "Migration bootstrap script not found: $bootstrap"
    }
    & $bootstrap -Mode $Mode
    if ($LASTEXITCODE -ne 0) {
        throw "Migration service run failed with exit code $LASTEXITCODE."
    }
    $migrationResult = "completed"
}

Write-Host "Installer summary"
Write-Host "Uploaded: $uploaded"
Write-Host "Failed: $failed"
Write-Host "Total bytes: $totalBytes"
Write-Host "Migration: $migrationResult"

if ($failed -gt 0) {
    exit 1
}
