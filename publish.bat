@echo off
echo ========================================
echo Publishing Standalone Executable
echo ========================================
echo.

cd /d "%~dp0"

echo Creating standalone single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Publish Successful!
    echo ========================================
    echo.
    echo Standalone executable location:
    echo %~dp0bin\Release\net8.0-windows\win-x64\publish\HypervisorToggle.exe
    echo.
    echo This executable can be run on any Windows 10/11 PC without installing .NET!
    echo.
    explorer.exe "%~dp0bin\Release\net8.0-windows\win-x64\publish\"
) else (
    echo.
    echo ========================================
    echo Publish Failed!
    echo ========================================
    echo.
    echo Make sure you have .NET 8.0 SDK installed.
    echo Download from: https://dotnet.microsoft.com/download
    echo.
)

pause
