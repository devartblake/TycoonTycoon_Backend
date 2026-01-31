#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Fixes Central Package Management issues by removing Version attributes from PackageReference items.

.DESCRIPTION
    This script scans all .csproj files in the solution and removes Version attributes from 
    PackageReference items, making them compatible with Central Package Management (CPM).

.EXAMPLE
    .\Fix-CentralPackageManagement.ps1

.NOTES
    Run this script from your solution root directory where the .sln or .slnx file is located.
#>

param(
    [switch]$WhatIf = $false,
    [switch]$Verbose = $false
)

$ErrorActionPreference = 'Stop'

# Function to display colored output
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = 'White'
    )
    Write-Host $Message -ForegroundColor $Color
}

# Display header
Write-ColorOutput "`n========================================" "Cyan"
Write-ColorOutput "  Central Package Management Fixer" "Cyan"
Write-ColorOutput "========================================`n" "Cyan"

# Get solution root
$solutionRoot = Get-Location
Write-ColorOutput "Solution Root: $solutionRoot" "Gray"

# Find all .csproj files
$projectFiles = Get-ChildItem -Path $solutionRoot -Filter *.csproj -Recurse
Write-ColorOutput "Found $($projectFiles.Count) project file(s)`n" "Yellow"

if ($projectFiles.Count -eq 0) {
    Write-ColorOutput "No .csproj files found. Make sure you're running this from the solution root." "Red"
    exit 1
}

$totalUpdated = 0
$totalUnchanged = 0
$totalErrors = 0

foreach ($project in $projectFiles) {
    $relativePath = $project.FullName.Replace($solutionRoot, "").TrimStart('\', '/')
    Write-ColorOutput "Processing: $relativePath" "Cyan"
    
    try {
        # Read the file content
        $content = Get-Content $project.FullName -Raw
        $originalContent = $content
        
        # Pattern to match PackageReference with Version attribute (various formats)
        # Matches both self-closing and opening tags
        $patterns = @(
            # Self-closing with Version
            '<PackageReference\s+Include="([^"]+)"\s+Version="[^"]*"\s*/>',
            # Opening tag with Version (may be multi-line)
            '<PackageReference\s+Include="([^"]+)"\s+Version="[^"]*"\s*>'
        )
        
        $replacements = @(
            '<PackageReference Include="$1" />',
            '<PackageReference Include="$1">'
        )
        
        # Apply all patterns
        for ($i = 0; $i -lt $patterns.Count; $i++) {
            $content = $content -replace $patterns[$i], $replacements[$i]
        }
        
        # Check if any changes were made
        if ($content -ne $originalContent) {
            if (-not $WhatIf) {
                # Backup the original file
                $backupPath = "$($project.FullName).backup"
                Copy-Item -Path $project.FullName -Destination $backupPath -Force
                Write-ColorOutput "  ✓ Created backup: $($project.Name).backup" "Gray"
                
                # Write the updated content
                Set-Content -Path $project.FullName -Value $content -NoNewline
                Write-ColorOutput "  ✓ Updated successfully" "Green"
            } else {
                Write-ColorOutput "  ⚠ Would update (WhatIf mode)" "Yellow"
            }
            $totalUpdated++
            
            if ($Verbose) {
                # Show what changed
                $changes = @()
                foreach ($pattern in $patterns) {
                    $matches = [regex]::Matches($originalContent, $pattern)
                    foreach ($match in $matches) {
                        $changes += "    - Removed version from: $($match.Groups[1].Value)"
                    }
                }
                if ($changes.Count -gt 0) {
                    Write-ColorOutput "  Changes:" "Gray"
                    $changes | ForEach-Object { Write-ColorOutput $_ "Gray" }
                }
            }
        } else {
            Write-ColorOutput "  • No changes needed" "Gray"
            $totalUnchanged++
        }
    }
    catch {
        Write-ColorOutput "  ✗ Error: $($_.Exception.Message)" "Red"
        $totalErrors++
    }
    
    Write-Host ""
}

# Summary
Write-ColorOutput "========================================" "Cyan"
Write-ColorOutput "  Summary" "Cyan"
Write-ColorOutput "========================================" "Cyan"
Write-ColorOutput "Projects updated:   $totalUpdated" $(if ($totalUpdated -gt 0) { "Green" } else { "Gray" })
Write-ColorOutput "Projects unchanged: $totalUnchanged" "Gray"
Write-ColorOutput "Errors:             $totalErrors" $(if ($totalErrors -gt 0) { "Red" } else { "Gray" })
Write-ColorOutput ""

if ($WhatIf) {
    Write-ColorOutput "*** WhatIf mode - no changes were made ***" "Yellow"
    Write-ColorOutput "Run without -WhatIf to apply changes" "Yellow"
} elseif ($totalUpdated -gt 0) {
    Write-ColorOutput "✓ All changes applied successfully!" "Green"
    Write-ColorOutput "  Backups created with .backup extension" "Gray"
    Write-ColorOutput ""
    Write-ColorOutput "Next steps:" "Yellow"
    Write-ColorOutput "  1. dotnet clean" "White"
    Write-ColorOutput "  2. dotnet restore" "White"
    Write-ColorOutput "  3. dotnet build" "White"
}

Write-ColorOutput ""

# Return success/failure
exit $(if ($totalErrors -gt 0) { 1 } else { 0 })
