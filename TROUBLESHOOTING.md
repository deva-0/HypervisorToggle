# VMware VT-x Troubleshooting Guide

If VMware still shows "VT-x is not available" or "VT-x/AMD-V hardware acceleration is not available" after disabling Hyper-V, follow this guide.

**Updated for Windows 11 24H2** - includes fixes for nested virtualization (e.g., running Proxmox as a VM).

## Quick Fix Checklist

### 1. Use the Enhanced App
Run the updated app and click **"Enable VMware Mode"** - it now:
- ✓ Disables hypervisorlaunchtype
- ✓ Disables ALL Hyper-V Windows features (11 features)
- ✓ Disables Memory Integrity (Core Isolation / HVCI)
- ✓ Disables Device Guard (Virtualization Based Security)
- ✓ Disables Credential Guard (LSA protection)
- ✓ Disables Windows Hello VBS (Windows 11 24H2)
- ✓ Disables LSA Isolation (Windows 11 24H2)
- ✓ Provides full diagnostics

### 2. RESTART YOUR COMPUTER
**This is CRITICAL!** Changes only take effect after a full restart.

### 3. Manual Verification Steps

After restart, verify these are all DISABLED:

#### Check 1: BCD Hypervisor Setting
```cmd
bcdedit /enum
```
Look for: `hypervisorlaunchtype    Off`

#### Check 2: Windows Features
Open PowerShell as Administrator:
```powershell
Get-WindowsOptionalFeature -Online | Where-Object {$_.FeatureName -like "*Hyper-V*" -or $_.FeatureName -like "*Virtual*"} | Select FeatureName, State
```
All Hyper-V features should show `Disabled`

#### Check 3: Memory Integrity (Core Isolation)
1. Open **Windows Security**
2. Go to **Device Security** → **Core Isolation Details**
3. Turn OFF **Memory Integrity**

OR via Registry:
```cmd
reg query "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled
```
Should return: `0x0` (disabled) or not exist

#### Check 4: Device Guard / Credential Guard
Open PowerShell as Administrator:
```powershell
Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard
```
Check if any security services are running.

## Manual Disable Commands

If the app doesn't work, run these commands **as Administrator**:

### 1. Disable Hypervisor Launch
```cmd
bcdedit /set hypervisorlaunchtype off
```

### 2. Disable All Hyper-V Features
```cmd
dism.exe /Online /Disable-Feature /FeatureName:Microsoft-Hyper-V-All /NoRestart
dism.exe /Online /Disable-Feature /FeatureName:HypervisorPlatform /NoRestart
dism.exe /Online /Disable-Feature /FeatureName:VirtualMachinePlatform /NoRestart
dism.exe /Online /Disable-Feature /FeatureName:Containers /NoRestart
```

### 3. Disable Memory Integrity (HVCI)
```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity" /v Enabled /t REG_DWORD /d 0 /f
```

### 4. Disable Windows Hello VBS (Windows 11 24H2)
```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\WindowsHello" /v Enabled /t REG_DWORD /d 0 /f
```

### 5. Disable Device Guard / VBS
```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v EnableVirtualizationBasedSecurity /t REG_DWORD /d 0 /f
```

### 6. Disable Credential Guard
```cmd
reg add "HKLM\SYSTEM\CurrentControlSet\Control\Lsa" /v LsaCfgFlags /t REG_DWORD /d 0 /f
```

### 7. Disable LSA Isolation (Windows 11 24H2 fix)
```cmd
bcdedit /set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO
```
Note: This may fail on non-24H2 systems - that's OK.

### 8. RESTART
```cmd
shutdown /r /t 0
```

## Still Not Working?

### Check BIOS/UEFI Settings
1. Restart computer and enter BIOS/UEFI (usually Del, F2, F10, or F12 during boot)
2. Find virtualization settings (may be called):
   - Intel VT-x
   - Intel Virtualization Technology
   - AMD-V
   - SVM Mode
   - Virtualization Extensions
3. **Enable** the virtualization option
4. Save and exit BIOS

### Check for Conflicting Software
Some security software can interfere:
- Windows Defender Application Guard
- Sandboxie
- Docker Desktop (if using Hyper-V backend)
- WSL2 (switch to WSL1 if needed)

### Verify CPU Supports Virtualization
```cmd
systeminfo
```
Look for:
```
Hyper-V Requirements:
    VM Monitor Mode Extensions: Yes
    Virtualization Enabled In Firmware: Yes
```

If "Virtualization Enabled In Firmware" shows **No**, you need to enable it in BIOS.

## Common Error Messages

### "VT-x is disabled in the BIOS for all CPU modes"
→ Enable VT-x/AMD-V in BIOS/UEFI settings

### "VT-x/AMD-V hardware acceleration is not available on your system"
→ Your CPU doesn't support virtualization (unlikely on modern CPUs)

### "This host supports Intel VT-x, but Intel VT-x is disabled"
→ Enable in BIOS or disable Hyper-V features

### "VMware Workstation and Hyper-V are not compatible"
→ Hyper-V is still active. Follow all disable steps above.

## Why This Happens

Windows 10/11 has multiple features that use Hyper-V's hypervisor:
- Hyper-V itself
- Windows Subsystem for Linux 2 (WSL2)
- Windows Sandbox
- Windows Defender Application Guard
- Containers
- Memory Integrity (Core Isolation)
- Device Guard / Credential Guard

When ANY of these are active, Windows takes exclusive control of VT-x/AMD-V, preventing VMware from using hardware virtualization.

## The Solution

You must disable:
1. ✓ Hypervisor launch type
2. ✓ ALL Windows features that use Hyper-V
3. ✓ Memory Integrity (HVCI)
4. ✓ Windows Hello VBS (Windows 11 24H2)
5. ✓ Device Guard / VBS
6. ✓ Credential Guard
7. ✓ LSA Isolation (Windows 11 24H2)
8. ✓ RESTART the computer

The enhanced app automates all of this!

## Testing After Changes

1. Open VMware Workstation
2. Create or open a VM
3. Go to VM → Settings → Processors
4. Check if "Virtualize Intel VT-x/EPT or AMD-V/RVI" is available and enabled
5. Try to power on the VM

## Need More Help?

1. Run **"Full Diagnostics"** in the app
2. Copy all output
3. Check each section for issues
4. Google specific error messages with "VMware"

## Quick Reference

| Feature | State for VMware | How to Check |
|---------|------------------|--------------|
| hypervisorlaunchtype | OFF | `bcdedit /enum` |
| Hyper-V Features | DISABLED | Windows Features dialog |
| Memory Integrity (HVCI) | OFF | Windows Security → Device Security → Core Isolation |
| Windows Hello VBS (24H2) | OFF | Registry check (see above) |
| Device Guard / VBS | OFF | Registry check (see above) |
| Credential Guard | OFF | Registry check (see above) |
| VT-x in BIOS | ENABLED | BIOS/UEFI settings |
| VMware VT-x option | ENABLED | VM Settings → Processors |

## Windows 11 24H2 Specific Issues

Windows 11 24H2 introduced stricter VBS (Virtualization Based Security) that can persist even after disabling traditional Hyper-V settings. If you've disabled everything above and VMware still doesn't work:

1. **Check for UEFI-locked VBS**: Some systems have VBS enabled at the firmware level
   - Check BIOS/UEFI for "Virtualization Based Security", "Device Guard", or similar settings
   - Some OEM systems enforce this and cannot be easily disabled

2. **Check for MDM/Intune policies**: Enterprise-managed devices may have policies that re-enable VBS

3. **Try Secure Boot toggle**: As a last resort, temporarily disable Secure Boot in BIOS, boot Windows, then re-enable Secure Boot. This can clear the UEFI variable that locks VBS.

4. **Nested Virtualization for Proxmox**: If you're running Proxmox as a VM and getting "vcpu-0 breakpoint error", ensure ALL the settings above are disabled, especially Memory Integrity.
