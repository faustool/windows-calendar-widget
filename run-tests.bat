@echo off
echo M365 Calendar App - Test Runner
echo ===============================
echo.

REM Set test configuration
set DOTNET_ENVIRONMENT=Test
set ASPNETCORE_ENVIRONMENT=Test

echo Running unit tests...
echo.

REM Clean previous test results
if exist "TestResults" rmdir /s /q "TestResults"

REM Run all tests with coverage
dotnet test M365CalendarApp.Tests --configuration Release --logger "console;verbosity=normal" --logger "trx;LogFileName=TestResults.trx" --collect:"XPlat Code Coverage" --results-directory TestResults

if %errorlevel% neq 0 (
    echo.
    echo ❌ Tests failed!
    echo Check the test output above for details.
    pause
    exit /b %errorlevel%
)

echo.
echo ✅ All tests passed!
echo.

REM Check if coverage report exists
if exist "TestResults\*\coverage.cobertura.xml" (
    echo 📊 Code coverage report generated in TestResults folder
) else (
    echo ⚠️  Code coverage report not found
)

echo.
echo Test Results Summary:
echo - Test results: TestResults\TestResults.trx
echo - Coverage report: TestResults\[guid]\coverage.cobertura.xml
echo.

REM Optional: Open test results
set /p openResults="Open test results? (y/n): "
if /i "%openResults%"=="y" (
    if exist "TestResults\TestResults.trx" (
        start "" "TestResults\TestResults.trx"
    )
)

echo.
echo Test run completed.
pause