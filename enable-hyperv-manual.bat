@echo off
:: Run as Administrator
net session >nul 2>&1
if %errorLevel% NEQ 0 (
    echo This script requires Administrator privileges.
    echo Please right-click and select "Run as administrator"
    pause
    exit /b 1
)

echo ========================================
echo  RE-ENABLE HYPER-V MODE
echo  (Reverse of disable-hyperv-manual.bat)
echo ========================================
echo.
echo NOTE: This script always re-enables Memory Integrity and Device Guard.
echo       If you want to restore exact previous settings, use the GUI app
echo       (HypervisorToggle.exe) which saves and restores your prior state.
echo.
echo This will re-enable Hyper-V to allow WSL2, Windows Sandbox,
echo and Docker Desktop to work. VMware nested virtualization
echo will NOT work after this.
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

echo.
echo [1/4] Re-enabling Hypervisor Launch Type...
bcdedit /set hypervisorlaunchtype auto
if %errorlevel% EQU 0 (
    echo     SUCCESS
) else (
    echo     FAILED - Error code: %errorlevel%
)

echo.
echo [2/4] Re-enabling core Hyper-V Windows Features...
echo     This may take a few minutes...

dism.exe /Online /Enable-Feature /FeatureName:Microsoft-Hyper-V-All /All /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-All

dism.exe /Online /Enable-Feature /FeatureName:HypervisorPlatform /All /NoRestart >nul 2>&1
echo     - HypervisorPlatform

dism.exe /Online /Enable-Feature /FeatureName:VirtualMachinePlatform /All /NoRestart >nul 2>&1
echo     - VirtualMachinePlatform

echo     COMPLETE

echo.
echo [3/4] Re-enabling Memory Integrity and Device Guard...
echo NOTE: These are always re-enabled here. For state-aware restore, use HypervisorToggle.exe.

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 1 /f >nul 2>&1
echo     - Memory Integrity (HVCI): ENABLED

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello" /v Enabled /t REG_DWORD /d 1 /f >nul 2>&1
echo     - Windows Hello VBS: ENABLED

reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 1 /f >nul 2>&1
echo     - Virtualization Based Security: ENABLED

reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 1 /f >nul 2>&1
echo     - Credential Guard: ENABLED

echo.
echo [4/4] Removing LSA Isolation BCD entry (24H2 fix)...
bcdedit /deletevalue {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions >nul 2>&1
if %errorlevel% EQU 0 (
    echo     - DISABLE-LSA-ISO removed
) else (
    echo     - Entry not present (nothing to remove)
)

echo.
echo ========================================
echo  HYPER-V RE-ENABLED
echo ========================================
echo.
echo IMPORTANT: You MUST restart your computer now!
echo.
choice /C YN /M "Restart computer now"
if %errorlevel% EQU 1 (
    echo Restarting in 10 seconds...
    shutdown /r /t 10 /c "Restarting to apply Hyper-V enable changes"
) else (
    echo.
    echo Please restart your computer manually as soon as possible.
)

echo.
pause
