#!/bin/bash

# ================================
# PHOTO SERVICE STARTUP SCRIPT
# Comprehensive startup and development helper
# ================================

set -e  # Exit on any error

# Script configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SERVICE_NAME="Photo Service"
PORT=5003
PROJECT_FILE="PhotoService.csproj"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_header() {
    echo "================================"
    echo "  DATING APP - PHOTO SERVICE"
    echo "  Port: $PORT"
    echo "  Environment: Development"
    echo "================================"
    echo
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if .NET 8 is installed
    if ! command -v dotnet &> /dev/null; then
        log_error ".NET SDK is not installed. Please install .NET 8 SDK."
        exit 1
    fi
    
    # Check .NET version
    DOTNET_VERSION=$(dotnet --version)
    if [[ ! $DOTNET_VERSION == 8.* ]]; then
        log_warning "Expected .NET 8, found version $DOTNET_VERSION"
    fi
    
    # Check if MySQL is accessible
    if ! command -v mysql &> /dev/null; then
        log_warning "MySQL client not found. Database connectivity cannot be verified."
    fi
    
    log_success "Prerequisites check completed"
}

# Setup database
setup_database() {
    log_info "Setting up database..."
    
    # Create storage directory
    mkdir -p "$SCRIPT_DIR/wwwroot/uploads/photos"
    
    # Run EF migrations (if any exist)
    if [ -d "$SCRIPT_DIR/Migrations" ] && [ "$(ls -A "$SCRIPT_DIR/Migrations")" ]; then
        log_info "Applying database migrations..."
        dotnet ef database update --project "$PROJECT_FILE"
    else
        log_info "No migrations found. Database will be created on first run."
    fi
    
    log_success "Database setup completed"
}

# Restore dependencies
restore_dependencies() {
    log_info "Restoring NuGet packages..."
    dotnet restore "$PROJECT_FILE"
    log_success "Dependencies restored"
}

# Build application
build_application() {
    log_info "Building application..."
    dotnet build "$PROJECT_FILE" --configuration Debug --verbosity minimal
    log_success "Build completed"
}

# Start the service
start_service() {
    log_info "Starting $SERVICE_NAME..."
    log_info "Service will be available at: http://localhost:$PORT"
    log_info "Swagger UI: http://localhost:$PORT/swagger"
    log_info "Health Check: http://localhost:$PORT/health"
    echo
    log_info "Press Ctrl+C to stop the service"
    echo
    
    # Start with hot reload for development
    dotnet run --project "$PROJECT_FILE" --configuration Debug
}

# Clean build artifacts
clean_build() {
    log_info "Cleaning build artifacts..."
    dotnet clean "$PROJECT_FILE"
    rm -rf "$SCRIPT_DIR/bin" "$SCRIPT_DIR/obj"
    log_success "Clean completed"
}

# Run tests (if any exist)
run_tests() {
    if [ -d "$SCRIPT_DIR/../PhotoService.Tests" ]; then
        log_info "Running tests..."
        dotnet test "$SCRIPT_DIR/../PhotoService.Tests" --verbosity minimal
        log_success "Tests completed"
    else
        log_info "No test project found"
    fi
}

# Show service status
show_status() {
    log_info "Checking service status..."
    
    # Check if port is in use
    if command -v lsof &> /dev/null; then
        if lsof -i :$PORT &> /dev/null; then
            log_success "Service appears to be running on port $PORT"
        else
            log_info "Port $PORT is available"
        fi
    fi
    
    # Check storage directory
    if [ -d "$SCRIPT_DIR/wwwroot/uploads/photos" ]; then
        PHOTO_COUNT=$(find "$SCRIPT_DIR/wwwroot/uploads/photos" -type f 2>/dev/null | wc -l)
        log_info "Photo storage directory exists with $PHOTO_COUNT files"
    else
        log_warning "Photo storage directory not found"
    fi
}

# Display help
show_help() {
    echo "Usage: $0 [COMMAND]"
    echo
    echo "Commands:"
    echo "  start     - Start the photo service (default)"
    echo "  build     - Build the application only"
    echo "  clean     - Clean build artifacts"
    echo "  test      - Run tests"
    echo "  status    - Show service status"
    echo "  help      - Show this help message"
    echo
    echo "Examples:"
    echo "  $0                # Start the service"
    echo "  $0 start          # Start the service"
    echo "  $0 build          # Build only"
    echo "  $0 clean build    # Clean and build"
}

# Main execution
main() {
    cd "$SCRIPT_DIR"
    
    case "${1:-start}" in
        "start")
            print_header
            check_prerequisites
            restore_dependencies
            build_application
            setup_database
            start_service
            ;;
        "build")
            print_header
            check_prerequisites
            restore_dependencies
            build_application
            ;;
        "clean")
            print_header
            clean_build
            if [ "$2" == "build" ]; then
                restore_dependencies
                build_application
            fi
            ;;
        "test")
            print_header
            check_prerequisites
            restore_dependencies
            build_application
            run_tests
            ;;
        "status")
            print_header
            show_status
            ;;
        "help"|"-h"|"--help")
            show_help
            ;;
        *)
            log_error "Unknown command: $1"
            show_help
            exit 1
            ;;
    esac
}

# Handle script interruption
trap 'log_info "Service stopped"; exit 0' INT TERM

# Run main function with all arguments
main "$@"
