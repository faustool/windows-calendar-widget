@echo off
echo M365 Calendar App - Test Coverage Report
echo =========================================
echo.

REM Check if reportgenerator is installed
dotnet tool list -g | findstr reportgenerator >nul
if %errorlevel% neq 0 (
    echo Installing ReportGenerator...
    dotnet tool install -g dotnet-reportgenerator-globaltool
    if %errorlevel% neq 0 (
        echo ❌ Failed to install ReportGenerator
        pause
        exit /b 1
    )
)

echo Running tests with coverage...
echo.

REM Clean previous results
if exist "TestResults" rmdir /s /q "TestResults"
if exist "CoverageReport" rmdir /s /q "CoverageReport"

REM Run tests with coverage
dotnet test M365CalendarApp.Tests --configuration Release --collect:"XPlat Code Coverage" --results-directory TestResults

if %errorlevel% neq 0 (
    echo ❌ Tests failed!
    pause
    exit /b %errorlevel%
)

echo.
echo Generating coverage report...

REM Find coverage file
for /r TestResults %%f in (coverage.cobertura.xml) do set COVERAGE_FILE=%%f

if not defined COVERAGE_FILE (
    echo ❌ Coverage file not found!
    pause
    exit /b 1
)

REM Generate HTML report
reportgenerator -reports:"%COVERAGE_FILE%" -targetdir:"CoverageReport" -reporttypes:Html

if %errorlevel% neq 0 (
    echo ❌ Failed to generate coverage report
    pause
    exit /b 1
)

echo.
echo ✅ Coverage report generated successfully!
echo.
echo Opening coverage report...
start "" "CoverageReport\index.html"

echo.
echo Coverage report location: CoverageReport\index.html
pause