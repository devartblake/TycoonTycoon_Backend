#!/bin/bash
# React Operator Dashboard — Docker Management Script
# Usage: ./react-dashboard.sh [command]
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

set -e

# Configuration
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOCKER_DIR="${PROJECT_ROOT}/docker"
IMAGE_NAME="${IMAGE_NAME:-synaptix/operator-dashboard-react}"
CONTAINER_NAME="${CONTAINER_NAME:-synaptix_operator_dashboard_react}"
REGISTRY="${DOCKER_REGISTRY:-docker.io}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper functions
log_info() {
  echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
  echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
  echo -e "${RED}[ERROR]${NC} $1"
}

# Build the image
build_image() {
  local api_url="${1:-https://api.synaptixplay.com}"
  local app_env="${2:-production}"
  local tag="${3:-latest}"

  log_info "Building React dashboard image..."
  log_info "API URL: $api_url"
  log_info "Environment: $app_env"

  docker build \
    -f "${DOCKER_DIR}/Dockerfile.dashboard-react" \
    --build-arg VITE_API_BASE_URL="$api_url" \
    --build-arg VITE_APP_ENV="$app_env" \
    -t "${IMAGE_NAME}:${tag}" \
    "$PROJECT_ROOT"

  log_info "Build complete: ${IMAGE_NAME}:${tag}"
}

# Build development image
build_dev() {
  log_info "Building development image..."
  build_image "http://localhost:5000" "development" "dev"
}

# Build production image
build_prod() {
  log_info "Building production image..."
  build_image "https://api.synaptixplay.com" "production" "latest"
}

# Run the container
run_container() {
  local api_url="${1:-https://api.synaptixplay.com}"
  local port="${2:-8300}"
  local tag="${3:-latest}"

  log_info "Starting React dashboard container..."
  log_info "Listening on port: $port"

  docker run -p "${port}:8300" \
    --name "$CONTAINER_NAME" \
    --rm \
    "${IMAGE_NAME}:${tag}"
}

# Run with development settings
run_dev() {
  log_info "Running development container..."
  build_dev
  run_container "http://localhost:5000" "8300" "dev"
}

# Run with production settings
run_prod() {
  log_info "Running production container..."
  build_prod
  run_container "https://api.synaptixplay.com" "8300" "latest"
}

# Show logs
show_logs() {
  if ! docker ps | grep -q "$CONTAINER_NAME"; then
    log_error "Container is not running. Start it with: ./react-dashboard.sh run"
    exit 1
  fi

  docker logs -f "$CONTAINER_NAME"
}

# Stop the container
stop_container() {
  if docker ps | grep -q "$CONTAINER_NAME"; then
    log_info "Stopping container..."
    docker stop "$CONTAINER_NAME"
    log_info "Container stopped"
  else
    log_warn "Container is not running"
  fi
}

# Open shell
open_shell() {
  if ! docker ps | grep -q "$CONTAINER_NAME"; then
    log_error "Container is not running"
    exit 1
  fi

  log_info "Opening shell in container..."
  docker exec -it "$CONTAINER_NAME" sh
}

# Check health
check_health() {
  if ! docker ps | grep -q "$CONTAINER_NAME"; then
    log_error "Container is not running"
    exit 1
  fi

  log_info "Checking container health..."
  if docker exec "$CONTAINER_NAME" wget -q -O- http://localhost:8300/index.html > /dev/null 2>&1; then
    log_info "✓ Container is healthy"
    return 0
  else
    log_error "✗ Container health check failed"
    return 1
  fi
}

# Push to registry
push_image() {
  local version="${1:-latest}"
  local tag="${IMAGE_NAME}:${version}"
  local registry_tag="${REGISTRY}/${tag}"

  log_info "Tagging image for registry..."
  docker tag "$tag" "$registry_tag"

  log_info "Pushing to registry: $REGISTRY"
  docker push "$registry_tag"

  log_info "Push complete: $registry_tag"
}

# Test the build
test_build() {
  log_info "Testing React dashboard build..."

  # Build dev version
  build_dev

  # Start container
  log_info "Starting test container..."
  docker run -p 18300:8300 \
    --name react-test-temp \
    --rm \
    -d \
    "${IMAGE_NAME}:dev"

  # Wait for container to be ready
  log_info "Waiting for container to be ready..."
  sleep 3

  # Test health
  if docker exec react-test-temp wget -q -O- http://localhost:8300/index.html > /dev/null 2>&1; then
    log_info "✓ Container started successfully"
    log_info "✓ Accessible at http://localhost:18300"

    # Keep running for inspection
    read -p "Press Enter to stop the test container: "
    docker stop react-test-temp
    log_info "Test complete"
  else
    log_error "✗ Container failed health check"
    docker stop react-test-temp
    exit 1
  fi
}

# Main command handler
case "${1:-help}" in
  build)
    build_image "${2:-https://api.synaptixplay.com}" "${3:-production}" "${4:-latest}"
    ;;
  build-dev)
    build_dev
    ;;
  build-prod)
    build_prod
    ;;
  run)
    run_container "${2:-https://api.synaptixplay.com}" "${3:-8300}" "${4:-latest}"
    ;;
  run-dev)
    run_dev
    ;;
  run-prod)
    run_prod
    ;;
  logs)
    show_logs
    ;;
  stop)
    stop_container
    ;;
  shell)
    open_shell
    ;;
  health)
    check_health
    ;;
  push)
    push_image "${2:-latest}"
    ;;
  test)
    test_build
    ;;
  *)
    cat << EOF
React Operator Dashboard — Docker Management

Usage: $0 [command] [options]

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
  $0 run-dev

  # Build production image with custom API
  $0 build https://api.staging.synaptixplay.com staging v1.0.0

  # Test the build locally
  $0 test

  # Push to registry
  $0 push v1.0.0

Documentation:
  See DOCKER-REACT.md for detailed Docker setup instructions
  See DEPLOYMENT.md for production deployment guide
EOF
    ;;
esac
