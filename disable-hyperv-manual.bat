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
echo  (Updated for Windows 11 24H2)
echo ========================================
echo.
echo This will completely disable Hyper-V to allow
echo VMware Workstation to use hardware virtualization,
echo including nested virtualization (e.g., Proxmox VMs).
echo.
echo Press any key to continue or Ctrl+C to cancel...
pause >nul

echo.
echo [1/5] Disabling Hypervisor Launch Type...
bcdedit /set hypervisorlaunchtype off
if %errorlevel% EQU 0 (
    echo     SUCCESS
) else (
    echo     FAILED - Error code: %errorlevel%
)

echo.
echo [2/5] Disabling Hyper-V Windows Features...
echo     This may take a few minutes...

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-All

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Tools-All /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Tools-All

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Management-PowerShell /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Management-PowerShell

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Hypervisor /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Hypervisor

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Services /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Services

dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-Management-Clients /NoRestart >nul 2>&1
echo     - Microsoft-Hyper-V-Management-Clients

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
echo [3/5] Disabling Memory Integrity (Core Isolation)...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
if %errorlevel% EQU 0 (
    echo     - Memory Integrity (HVCI): DISABLED
) else (
    echo     - Memory Integrity (HVCI): Already disabled or not present
)

:: Windows 11 24H2 specific - Windows Hello VBS
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello" /v Enabled /t REG_DWORD /d 0 /f >nul 2>&1
if %errorlevel% EQU 0 (
    echo     - Windows Hello VBS (24H2): DISABLED
) else (
    echo     - Windows Hello VBS (24H2): Already disabled or not present
)

echo.
echo [4/5] Disabling Device Guard / Credential Guard...
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f >nul 2>&1
echo     - Virtualization Based Security: DISABLED

reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f >nul 2>&1
echo     - Credential Guard (LSA): DISABLED

echo.
echo [5/5] Disabling LSA Isolation (Windows 11 24H2 fix)...
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO >nul 2>&1
if %errorlevel% EQU 0 (
    echo     - LSA Isolation: DISABLED
) else (
    echo     - LSA Isolation: Not present (normal for non-24H2 systems)
)

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
