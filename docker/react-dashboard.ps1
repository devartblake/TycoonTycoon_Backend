# React Operator Dashboard — Docker Management Script (PowerShell)
# Usage: .\react-dashboard.ps1 [command] [options]
#
# Commands:
#   build          Build the React dashboard Docker image
#   build-dev      Build development image
#   build-prod     Build production image with optimizations
#   run            Run the React dashboard container
#   run-dev        Run with development API URL
#   run-prod       Run with production API URL
#   logs           Show container logs
#   stop           Stop the container
#   shell          Open shell in running container
#   health         Check container health
#   push           Push image to registry
#   test           Test the build locally

param(
    [string]$Command = "help",
    [string]$ApiUrl = "",
    [string]$AppEnv = "",
    [string]$Tag = "",
    [string]$Port = "",
    [string]$Version = ""
)

# Configuration
$ProjectRoot = (Get-Item $PSScriptRoot).Parent.FullName
$DockerDir = Join-Path $ProjectRoot "docker"
$ImageName = if ($env:IMAGE_NAME) { $env:IMAGE_NAME } else { "synaptix/operator-dashboard-react" }
$ContainerName = if ($env:CONTAINER_NAME) { $env:CONTAINER_NAME } else { "synaptix_operator_dashboard_react" }
$Registry = if ($env:DOCKER_REGISTRY) { $env:DOCKER_REGISTRY } else { "docker.io" }

# Helper functions
function Log-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Log-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Log-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Build the image
function Build-Image {
    param(
        [string]$ApiUrl = "https://api.synaptixplay.com",
        [string]$AppEnv = "production",
        [string]$Tag = "latest"
    )

    Log-Info "Building React dashboard image..."
    Log-Info "API URL: $ApiUrl"
    Log-Info "Environment: $AppEnv"

    $DockerfilePath = Join-Path $DockerDir "Dockerfile.dashboard-react"
    & docker build `
        -f $DockerfilePath `
        --build-arg VITE_API_BASE_URL=$ApiUrl `
        --build-arg VITE_APP_ENV=$AppEnv `
        -t "${ImageName}:${Tag}" `
        $ProjectRoot

    if ($LASTEXITCODE -eq 0) {
        Log-Info "Build complete: ${ImageName}:${Tag}"
    } else {
        Log-Error "Build failed"
        exit 1
    }
}

# Build development image
function Build-Dev {
    Log-Info "Building development image..."
    Build-Image "http://localhost:5000" "development" "dev"
}

# Build production image
function Build-Prod {
    Log-Info "Building production image..."
    Build-Image "https://api.synaptixplay.com" "production" "latest"
}

# Run the container
function Run-Container {
    param(
        [string]$ApiUrl = "https://api.synaptixplay.com",
        [string]$Port = "8300",
        [string]$Tag = "latest"
    )

    Log-Info "Starting React dashboard container..."
    Log-Info "Listening on port: $Port"

    & docker run -p "${Port}:8300" `
        --name $ContainerName `
        --rm `
        "${ImageName}:${Tag}"
}

# Run with development settings
function Run-Dev {
    Log-Info "Running development container..."
    Build-Dev
    Run-Container "http://localhost:5000" "8300" "dev"
}

# Run with production settings
function Run-Prod {
    Log-Info "Running production container..."
    Build-Prod
    Run-Container "https://api.synaptixplay.com" "8300" "latest"
}

# Show logs
function Show-Logs {
    $running = & docker ps -q -f name=$ContainerName 2>$null
    if (-not $running) {
        Log-Error "Container is not running. Start it with: .\react-dashboard.ps1 run"
        exit 1
    }

    & docker logs -f $ContainerName
}

# Stop the container
function Stop-Container {
    $running = & docker ps -q -f name=$ContainerName 2>$null
    if ($running) {
        Log-Info "Stopping container..."
        & docker stop $ContainerName
        Log-Info "Container stopped"
    } else {
        Log-Warn "Container is not running"
    }
}

# Open shell
function Open-Shell {
    $running = & docker ps -q -f name=$ContainerName 2>$null
    if (-not $running) {
        Log-Error "Container is not running"
        exit 1
    }

    Log-Info "Opening shell in container..."
    & docker exec -it $ContainerName sh
}

# Check health
function Check-Health {
    $running = & docker ps -q -f name=$ContainerName 2>$null
    if (-not $running) {
        Log-Error "Container is not running"
        exit 1
    }

    Log-Info "Checking container health..."
    $healthResult = & docker exec $ContainerName wget -q -O- http://localhost:8300/index.html 2>$null

    if ($LASTEXITCODE -eq 0) {
        Log-Info "✓ Container is healthy"
        return $true
    } else {
        Log-Error "✗ Container health check failed"
        return $false
    }
}

# Push to registry
function Push-Image {
    param([string]$Version = "latest")

    $tag = "${ImageName}:${Version}"
    $registryTag = "${Registry}/${tag}"

    Log-Info "Tagging image for registry..."
    & docker tag $tag $registryTag

    Log-Info "Pushing to registry: $Registry"
    & docker push $registryTag

    if ($LASTEXITCODE -eq 0) {
        Log-Info "Push complete: $registryTag"
    } else {
        Log-Error "Push failed"
        exit 1
    }
}

# Test the build
function Test-Build {
    Log-Info "Testing React dashboard build..."

    # Build dev version
    Build-Dev

    # Start container
    Log-Info "Starting test container..."
    & docker run -p 18300:8300 `
        --name react-test-temp `
        --rm `
        -d `
        "${ImageName}:dev" | Out-Null

    # Wait for container to be ready
    Log-Info "Waiting for container to be ready..."
    Start-Sleep -Seconds 3

    # Test health
    $healthResult = & docker exec react-test-temp wget -q -O- http://localhost:8300/index.html 2>$null

    if ($LASTEXITCODE -eq 0) {
        Log-Info "✓ Container started successfully"
        Log-Info "✓ Accessible at http://localhost:18300"

        Read-Host "Press Enter to stop the test container"
        & docker stop react-test-temp
        Log-Info "Test complete"
    } else {
        Log-Error "✗ Container failed health check"
        & docker stop react-test-temp
        exit 1
    }
}

# Main command handler
switch ($Command.ToLower()) {
    "build" {
        $ApiUrl = if ($ApiUrl) { $ApiUrl } else { "https://api.synaptixplay.com" }
        $AppEnv = if ($AppEnv) { $AppEnv } else { "production" }
        $Tag = if ($Tag) { $Tag } else { "latest" }
        Build-Image $ApiUrl $AppEnv $Tag
    }
    "build-dev" {
        Build-Dev
    }
    "build-prod" {
        Build-Prod
    }
    "run" {
        $ApiUrl = if ($ApiUrl) { $ApiUrl } else { "https://api.synaptixplay.com" }
        $Port = if ($Port) { $Port } else { "8300" }
        $Tag = if ($Tag) { $Tag } else { "latest" }
        Run-Container $ApiUrl $Port $Tag
    }
    "run-dev" {
        Run-Dev
    }
    "run-prod" {
        Run-Prod
    }
    "logs" {
        Show-Logs
    }
    "stop" {
        Stop-Container
    }
    "shell" {
        Open-Shell
    }
    "health" {
        Check-Health
    }
    "push" {
        $Version = if ($Version) { $Version } else { "latest" }
        Push-Image $Version
    }
    "test" {
        Test-Build
    }
    default {
        Write-Host @"
React Operator Dashboard — Docker Management

Usage: .\react-dashboard.ps1 [command] [options]

Commands:
  build               Build the React dashboard image
  build-dev           Build development image (http://localhost:5000)
  build-prod          Build production image (https://api.synaptixplay.com)
  run                 Run container with default production settings
  run-dev             Build and run with development settings
  run-prod            Build and run with production settings
  logs                Show container logs
  stop                Stop the running container
  shell               Open shell in running container
  health              Check container health status
  push [version]      Push image to registry (default: latest)
  test                Test the build locally (starts on :18300)
  help                Show this help message

Environment Variables:
  IMAGE_NAME          Docker image name (default: synaptix/operator-dashboard-react)
  CONTAINER_NAME      Docker container name (default: synaptix_operator_dashboard_react)
  DOCKER_REGISTRY     Docker registry URL (default: docker.io)

Examples:
  # Build development image and run
  .\react-dashboard.ps1 run-dev

  # Build production image with custom API
  .\react-dashboard.ps1 build https://api.staging.synaptixplay.com staging v1.0.0

  # Test the build locally
  .\react-dashboard.ps1 test

  # Push to registry
  .\react-dashboard.ps1 push v1.0.0

Documentation:
  See DOCKER-REACT.md for detailed Docker setup instructions
  See DEPLOYMENT.md for production deployment guide
"@
    }
}
