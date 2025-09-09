#!/bin/bash
echo "Building M365 Calendar App..."

# Clean previous builds
dotnet clean

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore

# Build the application
echo "Building application..."
dotnet build -c Release

# Publish self-contained executable
echo "Publishing self-contained executable..."
dotnet publish M365CalendarApp.WPF -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Build completed successfully!"
echo ""
echo "Executable location: M365CalendarApp.WPF/bin/Release/net8.0-windows/win-x64/publish/M365CalendarApp.WPF.exe"
echo ""