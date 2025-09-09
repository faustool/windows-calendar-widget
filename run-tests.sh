#!/bin/bash

echo "M365 Calendar App - Test Runner"
echo "==============================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    if [ "$2" = "success" ]; then
        echo -e "${GREEN}✅ $1${NC}"
    elif [ "$2" = "error" ]; then
        echo -e "${RED}❌ $1${NC}"
    elif [ "$2" = "warning" ]; then
        echo -e "${YELLOW}⚠️  $1${NC}"
    elif [ "$2" = "info" ]; then
        echo -e "${BLUE}ℹ️  $1${NC}"
    else
        echo "$1"
    fi
}

# Set test configuration
export DOTNET_ENVIRONMENT=Test
export ASPNETCORE_ENVIRONMENT=Test

print_status "Running unit tests..." "info"
echo ""

# Clean previous test results
if [ -d "TestResults" ]; then
    rm -rf TestResults
fi

# Check if test project exists
if [ ! -f "M365CalendarApp.Tests/M365CalendarApp.Tests.csproj" ]; then
    print_status "Test project not found!" "error"
    exit 1
fi

# Run all tests with coverage
dotnet test M365CalendarApp.Tests \
    --configuration Release \
    --logger "console;verbosity=normal" \
    --logger "trx;LogFileName=TestResults.trx" \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults

TEST_EXIT_CODE=$?

echo ""

if [ $TEST_EXIT_CODE -ne 0 ]; then
    print_status "Tests failed!" "error"
    echo "Check the test output above for details."
    exit $TEST_EXIT_CODE
fi

print_status "All tests passed!" "success"
echo ""

# Check if coverage report exists
if find TestResults -name "coverage.cobertura.xml" -type f | grep -q .; then
    print_status "Code coverage report generated in TestResults folder" "info"
else
    print_status "Code coverage report not found" "warning"
fi

echo ""
print_status "Test Results Summary:" "info"
echo "- Test results: TestResults/TestResults.trx"
echo "- Coverage report: TestResults/[guid]/coverage.cobertura.xml"
echo ""

# Generate coverage report if reportgenerator is available
if command -v reportgenerator &> /dev/null; then
    print_status "Generating HTML coverage report..." "info"
    
    COVERAGE_FILE=$(find TestResults -name "coverage.cobertura.xml" -type f | head -1)
    if [ -n "$COVERAGE_FILE" ]; then
        reportgenerator \
            -reports:"$COVERAGE_FILE" \
            -targetdir:"TestResults/CoverageReport" \
            -reporttypes:Html
        
        if [ $? -eq 0 ]; then
            print_status "HTML coverage report generated: TestResults/CoverageReport/index.html" "success"
        fi
    fi
else
    print_status "Install 'dotnet tool install -g dotnet-reportgenerator-globaltool' for HTML coverage reports" "info"
fi

echo ""
print_status "Test run completed." "success"