# =============================================================================
# Tycoon Backend - Development Environment Setup Script (Windows/PowerShell)
# =============================================================================
# This script automates the setup of your local development environment.
# It checks for required tools, validates configuration, and helps start services.
# =============================================================================

# Requires PowerShell 5.1 or higher
#Requires -Version 5.1

# Error handling
$ErrorActionPreference = "Stop"

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$DockerDir = Join-Path $ProjectRoot "docker"
$EnvFile = Join-Path $DockerDir ".env"
$EnvExample = Join-Path $DockerDir ".env.example"

# =============================================================================
# Helper Functions
# =============================================================================

function Write-Header {
    param([string]$Message)
    Write-Host "`n=================================================" -ForegroundColor Blue
    Write-Host $Message -ForegroundColor Blue
    Write-Host "=================================================`n" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-ErrorMsg {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

# =============================================================================
# Prerequisite Checks
# =============================================================================

function Test-DotNet {
    Write-Header "Checking .NET SDK"
    
    try {
        $dotnetVersion = dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success ".NET SDK is installed (version: $dotnetVersion)"
            
            # Check if .NET 9 is installed
            $sdks = dotnet --list-sdks 2>$null
            if ($sdks -match "^9\.") {
                Write-Success ".NET 9 SDK is available"
                return $true
            }
            else {
                Write-Warning ".NET 9 SDK not found. This project requires .NET 9."
                Write-Info "Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
                return $false
            }
        }
    }
    catch {
        Write-ErrorMsg ".NET SDK is not installed"
        Write-Info "Download from: https://dotnet.microsoft.com/download"
        return $false
    }
}

function Test-Docker {
    Write-Header "Checking Docker"
    
    try {
        $dockerVersion = docker --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            # Check if Docker daemon is running
            docker info 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Success "Docker is installed and running ($dockerVersion)"
                return $true
            }
            else {
                Write-ErrorMsg "Docker is installed but not running"
                Write-Info "Please start Docker Desktop and try again"
                return $false
            }
        }
        else {
            Write-ErrorMsg "Docker is not installed"
            Write-Info "Download from: https://www.docker.com/get-started"
            return $false
        }
    }
    catch {
        Write-ErrorMsg "Docker is not installed"
        Write-Info "Download from: https://www.docker.com/get-started"
        return $false
    }
}

function Test-DockerCompose {
    Write-Header "Checking Docker Compose"
    
    try {
        $composeVersion = docker compose version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Docker Compose is available ($composeVersion)"
            return $true
        }
        else {
            Write-ErrorMsg "Docker Compose is not available"
            Write-Info "Ensure you have Docker Desktop installed with Compose V2"
            return $false
        }
    }
    catch {
        Write-ErrorMsg "Docker Compose is not available"
        Write-Info "Ensure you have Docker Desktop installed with Compose V2"
        return $false
    }
}

function Test-Make {
    try {
        make --version 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Make is installed"
        }
        else {
            Write-Warning "Make is not installed (optional, but recommended)"
            Write-Info "You can use 'docker compose' commands directly"
        }
    }
    catch {
        Write-Warning "Make is not installed (optional, but recommended)"
        Write-Info "You can use 'docker compose' commands directly"
    }
    return $true  # Not critical
}

# =============================================================================
# Configuration Setup
# =============================================================================

function Initialize-EnvFile {
    Write-Header "Setting up Environment Configuration"
    
    if (Test-Path $EnvFile) {
        Write-Success "Environment file already exists: $EnvFile"
        
        # Validate env file has required variables
        $requiredVars = @("POSTGRES_DB", "POSTGRES_USER", "POSTGRES_PASSWORD", "REDIS_PASSWORD", "ELASTIC_PASSWORD")
        $envContent = Get-Content $EnvFile -Raw
        $missingVars = @()
        
        foreach ($var in $requiredVars) {
            if ($envContent -notmatch "^$var=") {
                $missingVars += $var
            }
        }
        
        if ($missingVars.Count -eq 0) {
            Write-Success "All required environment variables are present"
        }
        else {
            Write-Warning "Missing environment variables: $($missingVars -join ', ')"
            Write-Info "Consider updating your .env file from .env.example"
        }
    }
    else {
        if (Test-Path $EnvExample) {
            Write-Info "Creating .env from .env.example..."
            Copy-Item $EnvExample $EnvFile
            Write-Success "Created: $EnvFile"
            Write-Warning "Review and update passwords in $EnvFile before production use!"
        }
        else {
            Write-ErrorMsg ".env.example not found at: $EnvExample"
            return $false
        }
    }
    return $true
}

function Test-AppSettings {
    Write-Header "Validating Configuration Files"
    
    $apiSettings = Join-Path $ProjectRoot "Tycoon.Backend.Api\appsettings.json"
    $migrationSettings = Join-Path $ProjectRoot "Tycoon.MigrationService\appsettings.json"
    
    if (Test-Path $apiSettings) {
        Write-Success "Found: Tycoon.Backend.Api\appsettings.json"
    }
    else {
        Write-ErrorMsg "Missing: $apiSettings"
        return $false
    }
    
    if (Test-Path $migrationSettings) {
        Write-Success "Found: Tycoon.MigrationService\appsettings.json"
    }
    else {
        Write-ErrorMsg "Missing: $migrationSettings"
        return $false
    }
    
    # Check connection strings match Docker defaults
    $apiContent = Get-Content $apiSettings -Raw
    if ($apiContent -match "Database=tycoon_db") {
        Write-Success "API configuration uses correct database name"
    }
    else {
        Write-Warning "API configuration may not match Docker defaults"
    }
    
    return $true
}

# =============================================================================
# Docker Infrastructure
# =============================================================================

function Start-Infrastructure {
    Write-Header "Starting Docker Infrastructure"
    
    Push-Location $DockerDir
    
    try {
        Write-Info "Starting infrastructure services..."
        
        $makeFile = Join-Path $DockerDir "MakeFile"
        if (Test-Path $makeFile) {
            make -f MakeFile up
        }
        else {
            docker compose -f compose.yml up -d
        }
        
        Write-Info "Waiting for services to become healthy (15 seconds)..."
        Start-Sleep -Seconds 15
        
        Write-Success "Docker infrastructure started"
        Write-Info "Use 'make -f docker/MakeFile health' to check service health"
    }
    finally {
        Pop-Location
    }
}

function Test-ServiceHealth {
    Write-Header "Checking Service Health"
    
    Push-Location $DockerDir
    
    try {
        $makeFile = Join-Path $DockerDir "MakeFile"
        if (Test-Path $makeFile) {
            make -f MakeFile health
        }
        else {
            Write-Info "Checking PostgreSQL..."
            docker compose exec -T postgres pg_isready -U tycoon_user -d tycoon_db 2>$null
            
            Write-Info "Checking MongoDB..."
            docker compose exec -T mongodb mongosh --quiet --eval "db.adminCommand('ping')" 2>$null
            
            Write-Info "Checking Redis..."
            docker compose exec -T redis redis-cli -a "tycoon_redis_password_123" ping 2>$null
            
            Write-Info "Checking Elasticsearch..."
            try {
                $response = Invoke-WebRequest -Uri "http://localhost:9200/_cluster/health" `
                    -Credential (New-Object System.Management.Automation.PSCredential("elastic", (ConvertTo-SecureString "tycoon_elastic_password_123" -AsPlainText -Force))) `
                    -UseBasicParsing -ErrorAction SilentlyContinue
                if ($response.StatusCode -eq 200) {
                    Write-Host "  ✅ Healthy" -ForegroundColor Green
                }
            }
            catch {
                Write-Host "  Not ready"
            }
        }
    }
    finally {
        Pop-Location
    }
}

# =============================================================================
# Main Setup Flow
# =============================================================================

function Main {
    Write-Header "Tycoon Backend - Development Environment Setup"
    
    Write-Host "This script will:"
    Write-Host "  1. Check for required development tools"
    Write-Host "  2. Validate/create configuration files"
    Write-Host "  3. Optionally start Docker infrastructure"
    Write-Host ""
    
    # Check prerequisites
    $dotnetOk = Test-DotNet
    $dockerOk = Test-Docker
    $dockerComposeOk = Test-DockerCompose
    Test-Make | Out-Null
    
    if (-not $dotnetOk -or -not $dockerOk -or -not $dockerComposeOk) {
        Write-ErrorMsg "Missing required tools. Please install them and run this script again."
        exit 1
    }
    
    # Setup configuration
    if (-not (Initialize-EnvFile)) {
        exit 1
    }
    
    if (-not (Test-AppSettings)) {
        exit 1
    }
    
    # Ask if user wants to start infrastructure
    Write-Header "Docker Infrastructure"
    $response = Read-Host "Would you like to start the Docker infrastructure now? (y/N)"
    
    if ($response -match "^[yY]") {
        Start-Infrastructure
        Test-ServiceHealth
        
        Write-Header "Next Steps"
        Write-Host "1. Run database migrations:"
        Write-Host "   dotnet run --project Tycoon.MigrationService\Tycoon.MigrationService.csproj"
        Write-Host ""
        Write-Host "2. Start the API:"
        Write-Host "   dotnet run --project Tycoon.Backend.Api\Tycoon.Backend.Api.csproj"
        Write-Host ""
        Write-Host "3. Access the application:"
        Write-Host "   API:        http://localhost:5000"
        Write-Host "   Swagger:    http://localhost:5000/swagger"
        Write-Host "   Hangfire:   http://localhost:5000/hangfire"
        Write-Host ""
        Write-Host "4. View logs:"
        Write-Host "   make -f docker/MakeFile logs"
    }
    else {
        Write-Header "Setup Complete!"
        Write-Host "Configuration is ready. When you're ready to start:"
        Write-Host ""
        Write-Host "1. Start Docker infrastructure:"
        Write-Host "   make -f docker/MakeFile up"
        Write-Host "   OR: docker compose -f docker/compose.yml up -d"
        Write-Host ""
        Write-Host "2. Run migrations:"
        Write-Host "   dotnet run --project Tycoon.MigrationService\Tycoon.MigrationService.csproj"
        Write-Host ""
        Write-Host "3. Start the API:"
        Write-Host "   dotnet run --project Tycoon.Backend.Api\Tycoon.Backend.Api.csproj"
    }
    
    Write-Success "Development environment is ready! 🚀"
}

# Run main function
try {
    Main
}
catch {
    Write-ErrorMsg "An error occurred: $_"
    exit 1
}
