#!/bin/bash

# Photo Service Test Script
# Quick validation that the photo service is working correctly

echo "🧪 Testing Photo Service..."
echo "=========================="

# Configuration
PHOTO_SERVICE_URL="http://localhost:5003"
AUTH_SERVICE_URL="http://localhost:8081"
TEST_USER_EMAIL="test.user@example.com"
TEST_USER_PASSWORD="TestPassword123!"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Test counter
TESTS_PASSED=0
TESTS_FAILED=0

# Function to log test results
log_test() {
    if [ $1 -eq 0 ]; then
        echo -e "✅ ${GREEN}PASS${NC}: $2"
        ((TESTS_PASSED++))
    else
        echo -e "❌ ${RED}FAIL${NC}: $2"
        ((TESTS_FAILED++))
    fi
}

# Function to test HTTP endpoint
test_endpoint() {
    local url="$1"
    local expected_status="$2"
    local description="$3"
    local method="${4:-GET}"
    local data="$5"
    local headers="$6"
    
    if [ -n "$data" ]; then
        response=$(curl -s -w "%{http_code}" -X "$method" "$url" -H "Content-Type: application/json" $headers -d "$data")
    else
        response=$(curl -s -w "%{http_code}" -X "$method" "$url" $headers)
    fi
    
    status_code="${response: -3}"
    
    if [ "$status_code" = "$expected_status" ]; then
        log_test 0 "$description"
    else
        log_test 1 "$description (Expected: $expected_status, Got: $status_code)"
    fi
}

# Check if services are running
echo "🔍 Checking service availability..."

# Test photo service health
test_endpoint "$PHOTO_SERVICE_URL/health" "200" "Photo Service health check"

# Test photo service API documentation
test_endpoint "$PHOTO_SERVICE_URL/swagger/index.html" "200" "Swagger documentation accessible"

# Test database connection
test_endpoint "$PHOTO_SERVICE_URL/health/ready" "200" "Database connection healthy"

echo ""
echo "🔐 Testing Authentication Integration..."

# Try to access protected endpoint without token
test_endpoint "$PHOTO_SERVICE_URL/api/photos/user/test-user-id" "401" "Protected endpoint requires authentication"

# Test with invalid token
test_endpoint "$PHOTO_SERVICE_URL/api/photos/user/test-user-id" "401" "Invalid token rejected" "GET" "" "-H 'Authorization: Bearer invalid-token'"

echo ""
echo "📊 Testing API Endpoints (without authentication)..."

# Test API endpoints that should be accessible
test_endpoint "$PHOTO_SERVICE_URL/api/photos/health" "200" "Photos health endpoint"

echo ""
echo "🗃️  Testing Database Schema..."

# Check if we can connect to the database
DB_TEST=$(mysql -h localhost -P 3311 -u root -proot123 -e "USE photo_service; SHOW TABLES;" 2>/dev/null)
if [[ $? -eq 0 && $DB_TEST == *"Photos"* ]]; then
    log_test 0 "Database schema exists"
else
    log_test 1 "Database schema missing"
fi

# Check table structure
COLUMNS_TEST=$(mysql -h localhost -P 3311 -u root -proot123 -e "USE photo_service; DESCRIBE Photos;" 2>/dev/null)
if [[ $? -eq 0 && $COLUMNS_TEST == *"UserId"* && $COLUMNS_TEST == *"FileName"* ]]; then
    log_test 0 "Photos table structure correct"
else
    log_test 1 "Photos table structure incorrect"
fi

echo ""
echo "📁 Testing Storage Setup..."

# Check storage directories
STORAGE_BASE="/home/m/development/DatingApp/photo-service/storage"
if [ -d "$STORAGE_BASE/photos/originals" ] && [ -d "$STORAGE_BASE/photos/thumbnails" ] && [ -d "$STORAGE_BASE/photos/medium" ] && [ -d "$STORAGE_BASE/photos/large" ]; then
    log_test 0 "Storage directories exist"
else
    log_test 1 "Storage directories missing"
fi

# Check permissions
if [ -w "$STORAGE_BASE" ]; then
    log_test 0 "Storage directory writable"
else
    log_test 1 "Storage directory not writable"
fi

echo ""
echo "🏗️  Testing Build and Dependencies..."

# Test dotnet build
cd /home/m/development/DatingApp/photo-service
BUILD_OUTPUT=$(dotnet build --verbosity quiet 2>&1)
if [ $? -eq 0 ]; then
    log_test 0 "Project builds successfully"
else
    log_test 1 "Build failed: $BUILD_OUTPUT"
fi

# Check critical dependencies
DEPS_CHECK=$(dotnet list package | grep -E "(ImageSharp|EntityFramework|Authentication)")
if [[ $DEPS_CHECK == *"SixLabors.ImageSharp"* && $DEPS_CHECK == *"EntityFrameworkCore"* ]]; then
    log_test 0 "Critical dependencies installed"
else
    log_test 1 "Missing critical dependencies"
fi

echo ""
echo "🔧 Testing Configuration..."

# Check appsettings files
if [ -f "appsettings.json" ] && [ -f "appsettings.Development.json" ]; then
    log_test 0 "Configuration files exist"
else
    log_test 1 "Configuration files missing"
fi

# Check JWT configuration
JWT_CONFIG=$(grep -o "JwtSettings" appsettings.json 2>/dev/null)
if [ -n "$JWT_CONFIG" ]; then
    log_test 0 "JWT configuration present"
else
    log_test 1 "JWT configuration missing"
fi

echo ""
echo "🐳 Testing Docker Configuration..."

# Check Dockerfile
if [ -f "Dockerfile" ]; then
    log_test 0 "Dockerfile exists"
else
    log_test 1 "Dockerfile missing"
fi

# Check if service is in docker-compose
COMPOSE_CHECK=$(grep -o "photo-service" ../docker-compose.yml 2>/dev/null)
if [ -n "$COMPOSE_CHECK" ]; then
    log_test 0 "Service configured in docker-compose"
else
    log_test 1 "Service not in docker-compose"
fi

echo ""
echo "🌐 Testing Gateway Integration..."

# Check YARP configuration
YARP_CONFIG=$(grep -o "photoRoute" ../dejting-yarp/src/dejting-yarp/appsettings.json 2>/dev/null)
if [ -n "$YARP_CONFIG" ]; then
    log_test 0 "YARP routing configured"
else
    log_test 1 "YARP routing missing"
fi

echo ""
echo "📝 Testing Documentation..."

# Check README and documentation
if [ -f "README.md" ]; then
    log_test 0 "README.md exists"
else
    log_test 1 "README.md missing"
fi

if [ -f "PHOTO_SERVICE_DOCUMENTATION.md" ]; then
    log_test 0 "Comprehensive documentation exists"
else
    log_test 1 "Comprehensive documentation missing"
fi

echo ""
echo "🎯 Test Summary"
echo "==============="
echo -e "Tests Passed: ${GREEN}$TESTS_PASSED${NC}"
echo -e "Tests Failed: ${RED}$TESTS_FAILED${NC}"
TOTAL_TESTS=$((TESTS_PASSED + TESTS_FAILED))
echo "Total Tests: $TOTAL_TESTS"

if [ $TESTS_FAILED -eq 0 ]; then
    echo -e "\n🎉 ${GREEN}All tests passed! Photo Service is ready for MVP.${NC}"
    echo ""
    echo "Next steps:"
    echo "1. Start all services: ./start_backend.sh"
    echo "2. Test photo upload via Swagger: http://localhost:5003/swagger"
    echo "3. Integrate with Flutter app"
    echo "4. Begin Phase 2: Messaging Service"
else
    echo -e "\n⚠️  ${YELLOW}Some tests failed. Please review and fix issues before proceeding.${NC}"
fi

echo ""
echo "For detailed testing with authentication:"
echo "1. Start all services: ./start_backend.sh"
echo "2. Register test user via auth service"
echo "3. Use token to test photo upload endpoints"
