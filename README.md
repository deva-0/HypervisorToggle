# HypervisorToggle

Windows utility to switch between Hyper-V mode and VMware Workstation mode by
fully disabling or restoring the Windows hypervisor stack and associated
security features.

Supports Windows 10, Windows 11, and Windows 11 24H2.  Handles nested
virtualization scenarios (e.g. running Proxmox as a guest, then running VMs
inside that guest).


## Overview

Windows takes exclusive control of CPU virtualization extensions (VT-x/AMD-V)
when the Hyper-V hypervisor is active.  This prevents VMware Workstation from
using hardware-accelerated virtualization.  A simple `bcdedit` toggle is not
sufficient -- the hypervisor persists through Windows Features, Virtualization
Based Security (VBS), Memory Integrity (HVCI), and related subsystems.

This tool disables the entire hypervisor stack when switching to VMware mode,
and restores it to its exact prior state when switching back.


## What Gets Disabled (VMware Mode)

    hypervisorlaunchtype          set to off via bcdedit
    Microsoft-Hyper-V-All         Windows Feature
    HypervisorPlatform            Windows Feature
    VirtualMachinePlatform        Windows Feature
    Microsoft-Hyper-V-*           all sub-features (11 total)
    Containers                    Windows Feature
    Memory Integrity (HVCI)       registry: DeviceGuard\Scenarios\HECI
    Windows Hello VBS             registry: DeviceGuard\Scenarios\WindowsHello
    Virtualization Based Security registry: DeviceGuard\EnableVBS
    Credential Guard              registry: Lsa\LsaCfgFlags
    LSA Isolation                 BCD: DISABLE-LSA-ISO (24H2)

State is saved to `hypervisor-state.json` before disabling.  Switching back to
Hyper-V mode reads this file and restores each value to its original state.
Settings that were already off before disabling are left off on restore.


## Requirements

- Windows 10 or Windows 11
- Administrator privileges
- .NET 8.0 runtime (or use the standalone executable from Releases)


## Installation

Download the latest release ZIP from the Releases page.  Extract all files to
a directory.  No installation required.


## Usage

### GUI Application

Run `HypervisorToggle.exe` as Administrator.

    Enable VMware Mode      Disables the full hypervisor stack.  Saves current
                            state to hypervisor-state.json before making any
                            changes.  Requires restart.

    Enable Hyper-V Mode     Restores hypervisor stack to pre-disable state by
                            reading hypervisor-state.json.  Re-enables core
                            Hyper-V Windows Features.  Requires restart.

    Run Full Diagnostics    Reports current state of all relevant settings:
                            BCD, Windows Features, registry keys, VBS status.

    Refresh Status          Reads current bcdedit configuration.

    Restart Computer Now    Initiates immediate system restart.

If no saved state file exists when switching back to Hyper-V mode, security
settings (HVCI, VBS, Credential Guard) are left unchanged.


### Batch Scripts

All scripts require Administrator privileges (right-click, Run as
administrator).

    disable-hyperv-manual.bat   Disables the full hypervisor stack.
                                Does not save state.

    enable-hyperv-manual.bat    Re-enables core Hyper-V features and sets
                                security settings back to enabled.  Always
                                re-enables security features regardless of
                                prior state -- use the GUI for state-aware
                                restore.

    check-status.bat            Reports current configuration without making
                                any changes.


## Building from Source

    dotnet build -c Release

Standalone single-file executable:

    dotnet publish -c Release -r win-x64 --self-contained true \
        -p:PublishSingleFile=true

Output: `bin/Release/net8.0-windows/win-x64/publish/HypervisorToggle.exe`


## Troubleshooting

See TROUBLESHOOTING.md for detailed diagnostics.

Common causes of VMware VT-x errors after running this tool:

- System not restarted after changes
- Memory Integrity re-enabled by Windows Update or OEM policy
- BIOS/UEFI virtualization (VT-x / AMD-V) disabled in firmware
- Device managed by Intune or Group Policy enforcing VBS

On Windows 11 24H2, VBS may be locked at the firmware level on some OEM
systems.  If disabling via registry has no effect after a restart, check
BIOS/UEFI for a Device Guard or Memory Protection setting.


## Modes

### Hyper-V Mode

hypervisorlaunchtype = auto, core Windows Features enabled, security features
restored to saved state.

    Hyper-V VMs             available
    WSL2                    available
    Windows Sandbox         available
    Docker Desktop          available (Hyper-V backend)
    VMware hardware virt.   not available
    Nested virtualization   not available


### VMware Mode

hypervisorlaunchtype = off, Windows Features disabled, security features
disabled.

    VMware hardware virt.   available
    Nested virtualization   available (e.g. Proxmox guest)
    VirtualBox              available
    Hyper-V VMs             not available
    WSL2                    not available (WSL1 works)
    Windows Sandbox         not available
    Docker Desktop          not available (Hyper-V backend)


## License

Public domain.  Use and modify freely.
