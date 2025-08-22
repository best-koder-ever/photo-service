#!/bin/bash

# Photo Service Startup Script
# Ensures database is ready and starts the photo service with proper logging

echo "🖼️  Starting Photo Service..."
echo "================================"

# Configuration
SERVICE_NAME="photo-service"
PROJECT_DIR="/home/m/development/DatingApp/photo-service"
LOG_FILE="$PROJECT_DIR/photo-service.log"

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to log with timestamp
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "$LOG_FILE"
}

# Function to check if MySQL is ready
check_mysql() {
    log "📊 Checking MySQL connection..."
    
    # Wait for MySQL to be ready (max 30 seconds)
    for i in {1..30}; do
        if mysql -h localhost -P 3311 -u root -proot123 -e "SELECT 1;" >/dev/null 2>&1; then
            log "✅ MySQL is ready"
            return 0
        fi
        echo -n "."
        sleep 1
    done
    
    log "❌ MySQL connection failed after 30 seconds"
    return 1
}

# Function to ensure database exists
ensure_database() {
    log "🗃️  Ensuring photo_service database exists..."
    
    mysql -h localhost -P 3311 -u root -proot123 -e "
        CREATE DATABASE IF NOT EXISTS photo_service 
        CHARACTER SET utf8mb4 
        COLLATE utf8mb4_unicode_ci;
        
        GRANT ALL PRIVILEGES ON photo_service.* TO 'root'@'%';
        FLUSH PRIVILEGES;
    " 2>/dev/null
    
    if [ $? -eq 0 ]; then
        log "✅ Database ready"
    else
        log "❌ Database setup failed"
        return 1
    fi
}

# Function to run migrations
run_migrations() {
    log "🔄 Running Entity Framework migrations..."
    
    cd "$PROJECT_DIR"
    
    # Check if migrations exist, create initial if needed
    if [ ! -d "Migrations" ] || [ -z "$(ls -A Migrations 2>/dev/null)" ]; then
        log "📝 Creating initial migration..."
        dotnet ef migrations add InitialCreate --output-dir Migrations
    fi
    
    # Apply migrations
    log "⚡ Applying database migrations..."
    dotnet ef database update
    
    if [ $? -eq 0 ]; then
        log "✅ Migrations completed successfully"
    else
        log "❌ Migration failed"
        return 1
    fi
}

# Function to create storage directories
create_storage() {
    log "📁 Setting up photo storage directories..."
    
    STORAGE_BASE="/home/m/development/DatingApp/photo-service/storage"
    
    mkdir -p "$STORAGE_BASE/photos/originals"
    mkdir -p "$STORAGE_BASE/photos/thumbnails"
    mkdir -p "$STORAGE_BASE/photos/medium"
    mkdir -p "$STORAGE_BASE/photos/large"
    mkdir -p "$STORAGE_BASE/temp"
    
    # Set proper permissions
    chmod -R 755 "$STORAGE_BASE"
    
    log "✅ Storage directories ready"
}

# Function to start the service
start_service() {
    log "🚀 Starting Photo Service API..."
    
    cd "$PROJECT_DIR"
    
    # Kill any existing process
    pkill -f "PhotoService.dll" 2>/dev/null || true
    
    # Start the service
    echo -e "${GREEN}Starting Photo Service on https://localhost:7003${NC}"
    echo -e "${YELLOW}Logs will be written to: $LOG_FILE${NC}"
    echo ""
    
    # Run with proper environment
    export ASPNETCORE_ENVIRONMENT=Development
    export ASPNETCORE_URLS="https://localhost:7003;http://localhost:5003"
    
    dotnet run --launch-profile https 2>&1 | tee -a "$LOG_FILE"
}

# Main execution
main() {
    # Clear previous log
    > "$LOG_FILE"
    
    log "🖼️  Photo Service Startup - $(date)"
    log "================================"
    
    # Check dependencies
    if ! check_mysql; then
        echo -e "${RED}❌ MySQL not available. Please start MySQL first.${NC}"
        exit 1
    fi
    
    # Setup database
    if ! ensure_database; then
        echo -e "${RED}❌ Database setup failed${NC}"
        exit 1
    fi
    
    # Setup storage
    create_storage
    
    # Run migrations
    if ! run_migrations; then
        echo -e "${RED}❌ Migration failed${NC}"
        exit 1
    fi
    
    # Start service
    start_service
}

# Handle script interruption
trap 'echo -e "\n${YELLOW}🛑 Photo Service stopped${NC}"; exit 0' INT TERM

# Run main function
main "$@"
