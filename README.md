# Hypervisor Mode Toggle - Enhanced

A comprehensive GUI application to toggle between Hyper-V and VMware Workstation modes on Windows, with full feature disabling to ensure VMware compatibility.

## ⚠️ NEW: Enhanced VMware Compatibility

This version completely disables ALL Hyper-V features to ensure VMware Workstation can use hardware virtualization (VT-x/AMD-V):

- ✓ Disables hypervisorlaunchtype  
- ✓ Disables ALL Hyper-V Windows features
- ✓ Disables Memory Integrity (Core Isolation)
- ✓ Full diagnostics tool
- ✓ Manual batch scripts included

## Features

- **Enable Hyper-V Mode**: Sets `hypervisorlaunchtype` to `auto` (VMware nested virtualization disabled)
- **Enable VMware Mode**: Sets `hypervisorlaunchtype` to `off` (Hyper-V disabled)
- **Status Check**: Shows current hypervisor configuration
- **Quick Restart**: Restart computer button to apply changes
- **Administrator Privileges**: Automatically requests admin rights

## Requirements

- Windows 10/11
- .NET 8.0 SDK (or .NET 6.0+)
- Administrator privileges

## Building the Application

### Option 1: Using .NET CLI

```bash
cd HypervisorToggle
dotnet build -c Release
```

The executable will be in: `bin/Release/net8.0-windows/HypervisorToggle.exe`

### Option 2: Using Visual Studio

1. Open the folder in Visual Studio 2022
2. Build the solution (Ctrl+Shift+B)
3. Run the application (F5)

## Publishing a Standalone Executable

To create a single-file executable that doesn't require .NET runtime:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The standalone executable will be in: `bin/Release/net8.0-windows/win-x64/publish/HypervisorToggle.exe`

## Usage

1. **Run as Administrator**: The app will automatically request administrator privileges
2. **Check Current Status**: The current hypervisor mode is displayed at the top
3. **Switch Modes**:
   - Click "Enable Hyper-V Mode" to enable Hyper-V (disables VMware nested virtualization)
   - Click "Enable VMware Mode" to disable ALL Hyper-V features (enables VMware Workstation)
4. **Run Diagnostics**: Click "Run Full Diagnostics" to see detailed system information
5. **Restart**: Click "Restart Computer Now" to apply changes immediately, or restart manually later

## Manual Scripts (No .NET Required)

If you can't or don't want to build the GUI app, use these batch scripts:

### `check-status.bat`
Checks your current Hyper-V/VMware configuration. Run this first to diagnose issues.
```cmd
check-status.bat
```

### `disable-hyperv-manual.bat`
Completely disables all Hyper-V features for VMware compatibility.
**Must run as Administrator!**
```cmd
Right-click → Run as administrator
```

## Troubleshooting VMware "VT-x not available" Error

See **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)** for detailed help if VMware still shows VT-x errors after disabling Hyper-V.

Common issues:
- Memory Integrity (Core Isolation) still enabled
- Windows features not fully disabled
- BIOS virtualization disabled
- System not restarted after changes

**Quick fix:** Run the GUI app → Click "Enable VMware Mode" → Restart computer

## What Each Mode Does

### Hyper-V Mode (hypervisorlaunchtype = auto)
- Hyper-V is enabled
- Windows Subsystem for Linux 2 (WSL2) works
- Windows Sandbox works
- Docker Desktop (Hyper-V backend) works
- VMware Workstation **cannot** use hardware virtualization
- VirtualBox may have compatibility issues

### VMware Mode (hypervisorlaunchtype = off + features disabled)
**The app now disables:**
- Hyper-V hypervisor launch type
- ALL Hyper-V Windows features
- Virtual Machine Platform
- Windows Hypervisor Platform  
- Memory Integrity (Core Isolation)
- Containers and Sandbox features

**Result:**
- ✓ VMware Workstation works with full hardware virtualization
- ✓ VirtualBox works normally
- ✗ WSL2 will not work (WSL1 still works)
- ✗ Windows Sandbox will not work
- ✗ Docker Desktop (Hyper-V backend) will not work
- ✗ Hyper-V VMs will not run

## Troubleshooting

**"Access Denied" Error**: Make sure you right-click the application and select "Run as Administrator"

**Changes Don't Take Effect**: You must restart your computer for the hypervisor changes to apply

**Can't Find Current Status**: The app reads the boot configuration. If it shows "Unable to determine", you may need to set the mode manually first.

## License

Free to use and modify.
