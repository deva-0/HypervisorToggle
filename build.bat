@echo off
echo ========================================
echo Building Hypervisor Toggle Application
echo ========================================
echo.

cd /d "%~dp0"

echo Building Release version...
dotnet build -c Release

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Build Successful!
    echo ========================================
    echo.
    echo Executable location:
    echo %~dp0bin\Release\net8.0-windows\HypervisorToggle.exe
    echo.
    echo To create a standalone executable, run:
    echo dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
    echo.
) else (
    echo.
    echo ========================================
    echo Build Failed!
    echo ========================================
    echo.
    echo Make sure you have .NET 8.0 SDK installed.
    echo Download from: https://dotnet.microsoft.com/download
    echo.
)

pause
