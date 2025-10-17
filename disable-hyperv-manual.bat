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
echo  DISABLE ALL HYPER-V FEATURES FOR VMWARE
echo ========================================
echo.
echo This will completely disable Hyper-V to allow
echo VMware Workstation to use hardware virtualization.
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

echo.
echo [1/4] Disabling Hypervisor Launch Type...
bcdedit /set hypervisorlaunchtype off
if %errorlevel% EQU 0 (
    echo     SUCCESS
) else (
    echo     FAILED - Error code: %errorlevel%
)

echo.
echo [2/4] Disabling Hyper-V Windows Features...
echo     This may take a few minutes...

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-All

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Hypervisor /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Hypervisor

dism.exe /Online /Disable-Feature /FeatureName:HypervisorPlatform /NoRestart >nul 2>&1
echo     - HypervisorPlatform

dism.exe /Online /Disable-Feature /FeatureName:VirtualMachinePlatform /NoRestart >nul 2>&1
echo     - VirtualMachinePlatform

dism.exe /Online /Disable-Feature /FeatureName:Containers /NoRestart >nul 2>&1
echo     - Containers

dism.exe /Online /Disable-Feature /FeatureName:Containers-DisposableClientVM /NoRestart >nul 2>&1
echo     - Containers-DisposableClientVM

echo     COMPLETE

echo.
echo [3/4] Disabling Memory Integrity (Core Isolation)...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
if %errorlevel% EQU 0 (
    echo     SUCCESS
) else (
    echo     FAILED or already disabled
)

echo.
echo [4/4] Disabling Device Guard / Credential Guard...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f >nul 2>&1
echo     COMPLETE

echo.
echo ========================================
echo  ALL HYPER-V FEATURES DISABLED
echo ========================================
echo.
echo IMPORTANT: You MUST restart your computer now!
echo.
echo Changes will NOT take effect until you restart.
echo.
choice /C YN /M "Restart computer now"
if %errorlevel% EQU 1 (
    echo Restarting in 10 seconds...
    shutdown /r /t 10 /c "Restarting to apply Hyper-V disable changes"
) else (
    echo.
    echo Please restart your computer manually as soon as possible.
)

echo.
pause
