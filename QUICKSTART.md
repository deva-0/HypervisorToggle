# QUICK START - Fix VMware "VT-x not available" Error

**Updated for Windows 11 24H2** - Also fixes nested virtualization issues (e.g., Proxmox VM "vcpu-0 breakpoint error")

## The Problem
VMware Workstation shows: "VT-x/AMD-V hardware acceleration is not available on your system"

This happens because Windows Hyper-V and VBS (Virtualization Based Security) features are taking exclusive control of your CPU's virtualization capabilities.

## The Solution (3 Steps)

### Step 1: Extract the ZIP file
Extract all files from `HypervisorToggle.zip`

### Step 2: Run the batch script (Easiest!)

1. Right-click **`disable-hyperv-manual.bat`**
2. Select **"Run as administrator"**
3. Wait for it to complete (takes 1-2 minutes)
4. Press Y to restart when prompted

### Step 3: After Restart

1. Open VMware Workstation
2. Try to power on a virtual machine
3. VT-x should now be available!

## Alternative: Build the GUI App

If you prefer a graphical interface:

1. Install .NET 8 SDK: https://dotnet.microsoft.com/download
2. Double-click `build.bat`
3. Run the built .exe as administrator
4. Click "Enable VMware Mode"
5. Restart

## Still Not Working?

### 1. Check BIOS (Most Common Issue!)

Your CPU's virtualization might be disabled in BIOS:

1. Restart computer
2. Press Del/F2/F10/F12 during boot to enter BIOS
3. Look for "Intel VT-x" or "AMD-V" or "Virtualization Technology"
4. **ENABLE** it
5. Save and exit BIOS

### 2. Check Memory Integrity

1. Open **Windows Security**
2. Go to **Device Security** → **Core Isolation Details**
3. Turn **OFF** "Memory Integrity"
4. Restart

### 3. Run Diagnostics

```cmd
check-status.bat
```

This will show exactly what's preventing VMware from working.

## What Gets Disabled

To make VMware work with hardware virtualization (including nested virtualization), these are disabled:
- ✗ Hyper-V hypervisor
- ✗ All Hyper-V Windows features (11 features)
- ✗ Virtual Machine Platform
- ✗ Memory Integrity (Core Isolation / HVCI)
- ✗ Device Guard (Virtualization Based Security)
- ✗ Credential Guard (LSA protection)
- ✗ Windows Hello VBS (Windows 11 24H2)
- ✗ LSA Isolation (Windows 11 24H2)
- ✗ WSL2 (WSL1 still works)
- ✗ Windows Sandbox
- ✗ Hyper-V VMs

## Manual Commands (if scripts don't work)

Run these in Command Prompt (as Administrator):

```cmd
:: Disable hypervisor
bcdedit /set hypervisorlaunchtype off

:: Disable Hyper-V features
dism /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart
dism /Online /Disable-Feature /FeatureName:VirtualMachinePlatform /NoRestart

:: Disable Memory Integrity
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f

:: Disable VBS (Windows 11 24H2)
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello" /v Enabled /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f

:: Disable LSA Isolation (Windows 11 24H2 - may fail on older Windows, that's OK)
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO

:: Restart
shutdown /r /t 0
```

## Switching Back to Hyper-V Mode

To re-enable Hyper-V after switching to VMware mode:

**GUI (state-aware — restores your exact previous settings):**
1. Run `HypervisorToggle.exe` as Administrator
2. Click "Enable Hyper-V Mode"
3. Restart

**Batch script (always re-enables security features):**
1. Right-click `enable-hyperv-manual.bat`
2. Select "Run as administrator"
3. Press Y to restart

## Need More Help?

- **Full troubleshooting**: Read `TROUBLESHOOTING.md`
- **Detailed guide**: Read `README.md`
