# QUICK START - Fix VMware "VT-x not available" Error

## The Problem
VMware Workstation shows: "VT-x/AMD-V hardware acceleration is not available on your system"

This happens because Windows Hyper-V features are taking exclusive control of your CPU's virtualization capabilities.

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

To make VMware work with hardware virtualization, these are disabled:
- ✗ Hyper-V hypervisor
- ✗ All Hyper-V Windows features
- ✗ Virtual Machine Platform
- ✗ Memory Integrity (Core Isolation)
- ✗ WSL2 (WSL1 still works)
- ✗ Windows Sandbox
- ✗ Hyper-V VMs

## Manual Commands (if scripts don't work)

Run these in Command Prompt (as Administrator):

```cmd
bcdedit /set hypervisorlaunchtype off
dism /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart
dism /Online /Disable-Feature /FeatureName:VirtualMachinePlatform /NoRestart
shutdown /r /t 0
```

## Need More Help?

- **Full troubleshooting**: Read `TROUBLESHOOTING.md`
- **Detailed guide**: Read `README.md`
