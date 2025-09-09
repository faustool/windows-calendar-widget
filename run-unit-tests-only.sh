#!/bin/bash

echo "M365 Calendar App - Unit Tests Only"
echo "==================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() {
    if [ "$2" = "success" ]; then
        echo -e "${GREEN}✅ $1${NC}"
    elif [ "$2" = "error" ]; then
        echo -e "${RED}❌ $1${NC}"
    elif [ "$2" = "info" ]; then
        echo -e "${BLUE}ℹ️  $1${NC}"
    else
        echo "$1"
    fi
}

# Set test configuration
export DOTNET_ENVIRONMENT=Test

print_status "Running unit tests only (excluding integration tests)..." "info"
echo ""

# Run unit tests only, excluding integration tests
dotnet test M365CalendarApp.Tests \
    --configuration Release \
    --logger "console;verbosity=normal" \
    --filter "Category!=Integration&Category!=EndToEnd" \
    --no-build

TEST_EXIT_CODE=$?

echo ""

if [ $TEST_EXIT_CODE -ne 0 ]; then
    print_status "Unit tests failed!" "error"
    exit $TEST_EXIT_CODE
fi

print_status "All unit tests passed!" "success"
echo ""
print_status "Unit test run completed." "success"