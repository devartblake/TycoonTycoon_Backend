#!/usr/bin/env bash
# =============================================================================
# Tycoon Backend - Development Environment Setup Script (Linux/macOS)
# =============================================================================
# This script automates the setup of your local development environment.
# It checks for required tools, validates configuration, and helps start services.
# =============================================================================

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DOCKER_DIR="$PROJECT_ROOT/docker"
ENV_FILE="$DOCKER_DIR/.env"
ENV_EXAMPLE="$DOCKER_DIR/.env.example"

# =============================================================================
# Helper Functions
# =============================================================================

print_header() {
    echo -e "\n${BLUE}=================================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}=================================================${NC}\n"
}

print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_info() {
    echo -e "${BLUE}ℹ️  $1${NC}"
}

# =============================================================================
# Prerequisite Checks
# =============================================================================

check_dotnet() {
    print_header "Checking .NET SDK"
    
    if command -v dotnet &> /dev/null; then
        DOTNET_VERSION=$(dotnet --version)
        print_success ".NET SDK is installed (version: $DOTNET_VERSION)"
        
        # Check if .NET 9 is installed
        if dotnet --list-sdks | grep -q "^9\."; then
            print_success ".NET 9 SDK is available"
            return 0
        else
            print_warning ".NET 9 SDK not found. This project requires .NET 9."
            print_info "Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
            return 1
        fi
    else
        print_error ".NET SDK is not installed"
        print_info "Download from: https://dotnet.microsoft.com/download"
        return 1
    fi
}

check_docker() {
    print_header "Checking Docker"
    
    if command -v docker &> /dev/null; then
        if docker info &> /dev/null; then
            DOCKER_VERSION=$(docker --version)
            print_success "Docker is installed and running ($DOCKER_VERSION)"
            return 0
        else
            print_error "Docker is installed but not running"
            print_info "Please start Docker Desktop and try again"
            return 1
        fi
    else
        print_error "Docker is not installed"
        print_info "Download from: https://www.docker.com/get-started"
        return 1
    fi
}

check_docker_compose() {
    print_header "Checking Docker Compose"
    
    if docker compose version &> /dev/null; then
        COMPOSE_VERSION=$(docker compose version)
        print_success "Docker Compose is available ($COMPOSE_VERSION)"
        return 0
    else
        print_error "Docker Compose is not available"
        print_info "Ensure you have Docker Desktop installed with Compose V2"
        return 1
    fi
}

check_make() {
    if command -v make &> /dev/null; then
        print_success "Make is installed"
        return 0
    else
        print_warning "Make is not installed (optional, but recommended)"
        print_info "Install via: brew install make (macOS) or apt-get install build-essential (Linux)"
        return 0  # Not critical, return success
    fi
}

# =============================================================================
# Configuration Setup
# =============================================================================

setup_env_file() {
    print_header "Setting up Environment Configuration"
    
    if [ -f "$ENV_FILE" ]; then
        print_success "Environment file already exists: $ENV_FILE"
        
        # Validate env file has required variables
        REQUIRED_VARS=("POSTGRES_DB" "POSTGRES_USER" "POSTGRES_PASSWORD" "REDIS_PASSWORD" "ELASTIC_PASSWORD")
        MISSING_VARS=()
        
        for VAR in "${REQUIRED_VARS[@]}"; do
            if ! grep -q "^${VAR}=" "$ENV_FILE"; then
                MISSING_VARS+=("$VAR")
            fi
        done
        
        if [ ${#MISSING_VARS[@]} -eq 0 ]; then
            print_success "All required environment variables are present"
        else
            print_warning "Missing environment variables: ${MISSING_VARS[*]}"
            print_info "Consider updating your .env file from .env.example"
        fi
    else
        if [ -f "$ENV_EXAMPLE" ]; then
            print_info "Creating .env from .env.example..."
            cp "$ENV_EXAMPLE" "$ENV_FILE"
            print_success "Created: $ENV_FILE"
            print_warning "Review and update passwords in $ENV_FILE before production use!"
        else
            print_error ".env.example not found at: $ENV_EXAMPLE"
            return 1
        fi
    fi
}

validate_appsettings() {
    print_header "Validating Configuration Files"
    
    API_SETTINGS="$PROJECT_ROOT/Tycoon.Backend.Api/appsettings.json"
    MIGRATION_SETTINGS="$PROJECT_ROOT/Tycoon.MigrationService/appsettings.json"
    
    if [ -f "$API_SETTINGS" ]; then
        print_success "Found: Tycoon.Backend.Api/appsettings.json"
    else
        print_error "Missing: $API_SETTINGS"
        return 1
    fi
    
    if [ -f "$MIGRATION_SETTINGS" ]; then
        print_success "Found: Tycoon.MigrationService/appsettings.json"
    else
        print_error "Missing: $MIGRATION_SETTINGS"
        return 1
    fi
    
    # Check connection strings match Docker defaults
    if grep -q "Database=tycoon_db" "$API_SETTINGS"; then
        print_success "API configuration uses correct database name"
    else
        print_warning "API configuration may not match Docker defaults"
    fi
}

# =============================================================================
# Docker Infrastructure
# =============================================================================

start_infrastructure() {
    print_header "Starting Docker Infrastructure"
    
    cd "$DOCKER_DIR"
    
    print_info "Starting infrastructure services..."
    if [ -f "MakeFile" ]; then
        make -f MakeFile up
    else
        docker compose -f compose.yml up -d
    fi
    
    print_info "Waiting for services to become healthy (15 seconds)..."
    sleep 15
    
    print_success "Docker infrastructure started"
    print_info "Use 'make -f docker/MakeFile health' to check service health"
}

check_service_health() {
    print_header "Checking Service Health"
    
    cd "$DOCKER_DIR"
    
    if [ -f "MakeFile" ]; then
        make -f MakeFile health
    else
        print_info "Checking PostgreSQL..."
        docker compose exec -T postgres pg_isready -U tycoon_user -d tycoon_db || echo "  Not ready"
        
        print_info "Checking MongoDB..."
        docker compose exec -T mongodb mongosh --quiet --eval "db.adminCommand('ping')" || echo "  Not ready"
        
        print_info "Checking Redis..."
        docker compose exec -T redis redis-cli -a "tycoon_redis_password_123" ping || echo "  Not ready"
        
        print_info "Checking Elasticsearch..."
        curl -fsS -u elastic:tycoon_elastic_password_123 http://localhost:9200/_cluster/health?pretty 2>/dev/null | grep -q '"status"' && echo "  ✅ Healthy" || echo "  Not ready"
    fi
}

# =============================================================================
# Main Setup Flow
# =============================================================================

main() {
    print_header "Tycoon Backend - Development Environment Setup"
    
    echo "This script will:"
    echo "  1. Check for required development tools"
    echo "  2. Validate/create configuration files"
    echo "  3. Optionally start Docker infrastructure"
    echo ""
    
    # Check prerequisites
    DOTNET_OK=0
    DOCKER_OK=0
    
    check_dotnet && DOTNET_OK=1 || DOTNET_OK=0
    check_docker && DOCKER_OK=1 || DOCKER_OK=0
    check_docker_compose || DOCKER_OK=0
    check_make
    
    if [ $DOTNET_OK -eq 0 ] || [ $DOCKER_OK -eq 0 ]; then
        print_error "Missing required tools. Please install them and run this script again."
        exit 1
    fi
    
    # Setup configuration
    setup_env_file || exit 1
    validate_appsettings || exit 1
    
    # Ask if user wants to start infrastructure
    print_header "Docker Infrastructure"
    echo "Would you like to start the Docker infrastructure now? (y/N)"
    read -r response
    
    if [[ "$response" =~ ^([yY][eE][sS]|[yY])$ ]]; then
        start_infrastructure
        check_service_health
        
        print_header "Next Steps"
        echo "1. Run database migrations:"
        echo "   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj"
        echo ""
        echo "2. Start the API:"
        echo "   dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj"
        echo ""
        echo "3. Access the application:"
        echo "   API:        http://localhost:5000"
        echo "   Swagger:    http://localhost:5000/swagger"
        echo "   Hangfire:   http://localhost:5000/hangfire"
        echo ""
        echo "4. View logs:"
        echo "   make -f docker/MakeFile logs"
    else
        print_header "Setup Complete!"
        echo "Configuration is ready. When you're ready to start:"
        echo ""
        echo "1. Start Docker infrastructure:"
        echo "   make -f docker/MakeFile up"
        echo ""
        echo "2. Run migrations:"
        echo "   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj"
        echo ""
        echo "3. Start the API:"
        echo "   dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj"
    fi
    
    print_success "Development environment is ready! 🚀"
}

# Run main function
main "$@"
