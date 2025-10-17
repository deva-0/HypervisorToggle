@echo off
:: Check Administrator privileges
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo Warning: Some checks require Administrator privileges.
    echo For complete diagnostics, please right-click and "Run as administrator"
    echo.
    pause
)

echo ========================================
echo  HYPER-V / VMWARE COMPATIBILITY CHECK
echo ========================================
echo.

echo [1] Checking Hypervisor Launch Type...
echo.
bcdedit /enum {current} | findstr /i "hypervisorlaunchtype"
if %errorlevel% EQU 0 (
    echo.
    echo If it shows "Off" - VMware should work
    echo If it shows "Auto" - Hyper-V is active
) else (
    echo Not found or error
)

echo.
echo ========================================
echo [2] Checking Windows Features...
echo.
dism /Online /Get-Features /Format:Table | findstr /i "Hyper-V Virtual Container"

echo.
echo ========================================
echo [3] Checking Memory Integrity...
echo.
reg query "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled 2>nul
if %errorlevel% EQU 0 (
    echo If value is 0x1 - Memory Integrity is ENABLED (bad for VMware)
    echo If value is 0x0 - Memory Integrity is DISABLED (good for VMware)
) else (
    echo Memory Integrity not configured (good for VMware)
)

echo.
echo ========================================
echo [4] Checking Device Guard...
echo.
reg query "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity 2>nul
if %errorlevel% EQU 0 (
    echo Device Guard registry key found
) else (
    echo Device Guard not configured (good for VMware)
)

echo.
echo ========================================
echo [5] Checking CPU Virtualization...
echo.
systeminfo | findstr /i "Virtualization Hyper-V"

echo.
echo ========================================
echo  SUMMARY
echo ========================================
echo.
echo For VMware to work with hardware virtualization:
echo   1. hypervisorlaunchtype should be: OFF
echo   2. All Hyper-V features should be: Disabled
echo   3. Memory Integrity should be: OFF (0x0)
echo   4. Device Guard should be: Not configured or OFF
echo   5. BIOS virtualization should be: ENABLED
echo.
echo If anything is wrong, use the GUI app or run:
echo   disable-hyperv-manual.bat (as Administrator)
echo.
echo Then RESTART your computer!
echo.
pause
