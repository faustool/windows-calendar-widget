@echo off
echo Building M365 Calendar App...

REM Clean previous builds
dotnet clean

REM Restore packages
echo Restoring NuGet packages...
dotnet restore

REM Build the application
echo Building application...
dotnet build -c Release

REM Publish self-contained executable
echo Publishing self-contained executable...
dotnet publish M365CalendarApp.WPF -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo.
echo Build completed successfully!
echo.
echo Executable location: M365CalendarApp.WPF\bin\Release\net8.0-windows\win-x64\publish\M365CalendarApp.WPF.exe
echo.
pause