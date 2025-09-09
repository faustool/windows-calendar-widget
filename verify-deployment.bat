@echo off
echo M365 Calendar App - Deployment Verification
echo ==========================================
echo.

echo Checking project structure...
if exist "M365CalendarApp.sln" (
    echo ✅ Solution file found
) else (
    echo ❌ Solution file missing
)

if exist "M365CalendarApp.WPF" (
    echo ✅ WPF project folder found
) else (
    echo ❌ WPF project folder missing
)

if exist "M365CalendarApp.WPF\M365CalendarApp.WPF.csproj" (
    echo ✅ WPF project file found
) else (
    echo ❌ WPF project file missing
)

if exist "M365CalendarApp.WPF\Services" (
    echo ✅ Services folder found
) else (
    echo ❌ Services folder missing
)

if exist "README.md" (
    echo ✅ Documentation found
) else (
    echo ❌ Documentation missing
)

echo.
echo Checking .NET installation...
dotnet --version >nul 2>&1
if %errorlevel% == 0 (
    echo ✅ .NET SDK found
    dotnet --version
) else (
    echo ❌ .NET SDK not found
    echo Please install .NET 8.0 Desktop Runtime
)

echo.
echo Checking for Windows Desktop support...
dotnet --list-runtimes | findstr "Microsoft.WindowsDesktop.App" >nul 2>&1
if %errorlevel% == 0 (
    echo ✅ Windows Desktop Runtime found
) else (
    echo ❌ Windows Desktop Runtime missing
    echo Please install .NET 8.0 Desktop Runtime from:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
)

echo.
echo Testing build...
dotnet build M365CalendarApp.WPF --verbosity quiet >nul 2>&1
if %errorlevel% == 0 (
    echo ✅ Project builds successfully
    echo.
    echo 🚀 Ready to run! Execute:
    echo    dotnet run --project M365CalendarApp.WPF
) else (
    echo ❌ Build failed
    echo Run 'dotnet build M365CalendarApp.WPF' for details
)

echo.
echo Client ID configured: 9b0059d1-a22e-4ed9-854b-b3304df51816
echo.
pause