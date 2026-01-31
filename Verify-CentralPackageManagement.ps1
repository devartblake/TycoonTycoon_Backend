#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies Central Package Management (CPM) configuration in the solution.

.DESCRIPTION
    Checks for:
    - Presence of Directory.Build.props and Directory.Packages.props
    - ManagePackageVersionsCentrally setting
    - PackageReference items with Version attributes (violations)
    - Missing package versions in Directory.Packages.props

.EXAMPLE
    .\Verify-CentralPackageManagement.ps1
#>

$ErrorActionPreference = 'Stop'

function Write-ColorOutput {
    param([string]$Message, [string]$Color = 'White')
    Write-Host $Message -ForegroundColor $Color
}

function Test-FileExists {
    param([string]$Path, [string]$Name)
    if (Test-Path $Path) {
        Write-ColorOutput "✓ $Name found" "Green"
        return $true
    } else {
        Write-ColorOutput "✗ $Name missing" "Red"
        return $false
    }
}

Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  CPM Configuration Verification" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

$solutionRoot = Get-Location
$allGood = $true

# Check for required files
Write-ColorOutput "Checking required files..." "Yellow"
$hasBuildProps = Test-FileExists "Directory.Build.props" "Directory.Build.props"
$hasPackagesProps = Test-FileExists "Directory.Packages.props" "Directory.Packages.props"
Write-Host ""

if (-not $hasBuildProps -or -not $hasPackagesProps) {
    $allGood = $false
}

# Check Directory.Build.props content
if ($hasBuildProps) {
    Write-ColorOutput "Checking Directory.Build.props configuration..." "Yellow"
    $buildProps = Get-Content "Directory.Build.props" -Raw
    if ($buildProps -match '<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>') {
        Write-ColorOutput "✓ ManagePackageVersionsCentrally is enabled" "Green"
    } else {
        Write-ColorOutput "✗ ManagePackageVersionsCentrally is not enabled" "Red"
        $allGood = $false
    }
    Write-Host ""
}

# Load packages from Directory.Packages.props
$packageVersions = @{}
if ($hasPackagesProps) {
    Write-ColorOutput "Loading package versions from Directory.Packages.props..." "Yellow"
    [xml]$packagesXml = Get-Content "Directory.Packages.props"
    $packagesXml.Project.ItemGroup.PackageVersion | ForEach-Object {
        if ($_.Include) {
            $packageVersions[$_.Include] = $_.Version
        }
    }
    Write-ColorOutput "✓ Loaded $($packageVersions.Count) package version(s)" "Green"
    Write-Host ""
}

# Scan all .csproj files
Write-ColorOutput "Scanning project files for violations..." "Yellow"
$projectFiles = Get-ChildItem -Path $solutionRoot -Filter *.csproj -Recurse
$violations = @()
$missingPackages = @{}

foreach ($project in $projectFiles) {
    try {
        [xml]$projectXml = Get-Content $project.FullName
        $packageRefs = $projectXml.Project.ItemGroup.PackageReference
        
        foreach ($packageRef in $packageRefs) {
            if ($packageRef.Include) {
                # Check for Version attribute (violation)
                if ($packageRef.Version) {
                    $violations += [PSCustomObject]@{
                        Project = $project.Name
                        Package = $packageRef.Include
                        Version = $packageRef.Version
                    }
                }
                
                # Check if package exists in Directory.Packages.props
                if (-not $packageVersions.ContainsKey($packageRef.Include)) {
                    if (-not $missingPackages.ContainsKey($packageRef.Include)) {
                        $missingPackages[$packageRef.Include] = @()
                    }
                    $missingPackages[$packageRef.Include] += $project.Name
                }
            }
        }
    }
    catch {
        Write-ColorOutput "⚠ Error reading $($project.Name): $($_.Exception.Message)" "Yellow"
    }
}

# Report violations
if ($violations.Count -gt 0) {
    Write-ColorOutput "✗ Found $($violations.Count) violation(s) - PackageReferences with Version attributes:" "Red"
    Write-Host ""
    $violations | Format-Table -AutoSize | Out-String | Write-Host
    Write-ColorOutput "Run Fix-CentralPackageManagement.ps1 to remove these versions" "Yellow"
    $allGood = $false
} else {
    Write-ColorOutput "✓ No violations found - all PackageReferences are version-free" "Green"
}
Write-Host ""

# Report missing packages
if ($missingPackages.Count -gt 0) {
    Write-ColorOutput "⚠ Found $($missingPackages.Count) package(s) referenced but not in Directory.Packages.props:" "Yellow"
    Write-Host ""
    foreach ($package in $missingPackages.Keys | Sort-Object) {
        Write-ColorOutput "  • $package" "Yellow"
        Write-ColorOutput "    Used in: $($missingPackages[$package] -join ', ')" "Gray"
    }
    Write-Host ""
    Write-ColorOutput "Add these to Directory.Packages.props with appropriate versions" "Yellow"
    $allGood = $false
} else {
    Write-ColorOutput "✓ All referenced packages have versions defined" "Green"
}
Write-Host ""

# Final summary
Write-ColorOutput "========================================" "Cyan"
if ($allGood) {
    Write-ColorOutput "✓ CPM configuration is correct!" "Green"
    Write-ColorOutput ""
    Write-ColorOutput "Next steps:" "Yellow"
    Write-ColorOutput "  1. dotnet clean" "White"
    Write-ColorOutput "  2. dotnet restore" "White"
    Write-ColorOutput "  3. dotnet build" "White"
} else {
    Write-ColorOutput "✗ Issues found - please review above" "Red"
    Write-ColorOutput ""
    Write-ColorOutput "To fix:" "Yellow"
    Write-ColorOutput "  1. Run .\Fix-CentralPackageManagement.ps1" "White"
    Write-ColorOutput "  2. Add missing packages to Directory.Packages.props" "White"
    Write-ColorOutput "  3. Run this verification script again" "White"
}
Write-ColorOutput "========================================" "Cyan"
Write-Host ""

exit $(if ($allGood) { 0 } else { 1 })
