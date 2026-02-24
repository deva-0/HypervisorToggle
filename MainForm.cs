using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Text.Json;

namespace HypervisorToggle
{
    public class HypervisorState
    {
        public int HvciEnabled { get; set; } = -1;
        public int WindowsHelloVbsEnabled { get; set; } = -1;
        public int VbsEnabled { get; set; } = -1;
        public int CredentialGuardEnabled { get; set; } = -1;
        public bool LsaIsoWasPresent { get; set; } = false;
    }

    public class MainForm : Form
    {
        private Button btnEnableHyperV;
        private Button btnDisableHyperV;
        private Button btnCheckStatus;
        private Button btnRestart;
        private Button btnFullDiagnostics;
        private Label lblStatus;
        private Label lblInfo;
        private TextBox txtOutput;

        private static string StateFilePath =>
            Path.Combine(AppContext.BaseDirectory, "hypervisor-state.json");

        public MainForm()
        {
            InitializeComponents();
            CheckCurrentStatus();
        }

        private void InitializeComponents()
        {
            this.Text = "Hypervisor Mode Toggle - Enhanced";
            this.Size = new Size(600, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Info Label
            lblInfo = new Label
            {
                Text = "Toggle between Hyper-V and VMware Workstation modes.\nDisables ALL Hyper-V features for VMware compatibility.",
                Location = new Point(20, 20),
                Size = new Size(540, 40),
                Font = new Font("Segoe UI", 9F)
            };

            // Status Label
            lblStatus = new Label
            {
                Text = "Current Status: Checking...",
                Location = new Point(20, 70),
                Size = new Size(540, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.Blue
            };

            // Enable Hyper-V Button
            btnEnableHyperV = new Button
            {
                Text = "Enable Hyper-V Mode\n(Disable VMware nested virtualization)",
                Location = new Point(20, 110),
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.LightGreen
            };
            btnEnableHyperV.Click += BtnEnableHyperV_Click;

            // Disable Hyper-V Button
            btnDisableHyperV = new Button
            {
                Text = "Enable VMware Mode\n(Disable ALL Hyper-V Features)",
                Location = new Point(310, 110),
                Size = new Size(250, 60),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.LightCoral
            };
            btnDisableHyperV.Click += BtnDisableHyperV_Click;

            // Full Diagnostics Button
            btnFullDiagnostics = new Button
            {
                Text = "Run Full Diagnostics",
                Location = new Point(20, 180),
                Size = new Size(170, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.LightBlue
            };
            btnFullDiagnostics.Click += BtnFullDiagnostics_Click;

            // Check Status Button
            btnCheckStatus = new Button
            {
                Text = "Refresh Status",
                Location = new Point(205, 180),
                Size = new Size(170, 40),
                Font = new Font("Segoe UI", 9F)
            };
            btnCheckStatus.Click += BtnCheckStatus_Click;

            // Restart Button
            btnRestart = new Button
            {
                Text = "Restart Computer Now",
                Location = new Point(390, 180),
                Size = new Size(170, 40),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Orange
            };
            btnRestart.Click += BtnRestart_Click;

            // Output TextBox
            txtOutput = new TextBox
            {
                Location = new Point(20, 230),
                Size = new Size(540, 240),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 8F)
            };

            // Add controls to form
            this.Controls.AddRange(new Control[] 
            { 
                lblInfo, 
                lblStatus, 
                btnEnableHyperV, 
                btnDisableHyperV, 
                btnFullDiagnostics,
                btnCheckStatus, 
                btnRestart, 
                txtOutput 
            });
        }

        private void BtnEnableHyperV_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will enable Hyper-V mode on next boot.\nVMware nested virtualization will not work.\n\nContinue?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ExecuteBcdEdit("auto", "Hyper-V Enabled");
            }
        }

        private void BtnDisableHyperV_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "This will COMPLETELY disable Hyper-V for VMware compatibility:\n\n" +
                "✓ Set hypervisorlaunchtype to OFF\n" +
                "✓ Disable all Hyper-V Windows Features\n" +
                "✓ Disable Virtual Machine Platform\n" +
                "✓ Disable Windows Hypervisor Platform\n" +
                "✓ Disable Containers & Windows Sandbox\n" +
                "✓ Disable Memory Integrity (Core Isolation)\n" +
                "✓ Disable Device Guard / Credential Guard\n" +
                "✓ Disable LSA Isolation (Windows 11 24H2 fix)\n\n" +
                "This ensures VMware Workstation will work with VT-x/AMD-V,\n" +
                "including nested virtualization (e.g., Proxmox VMs).\n\n" +
                "Continue?",
                "Confirm - Full Hyper-V Disable",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                txtOutput.Clear();
                txtOutput.AppendText("=== DISABLING ALL HYPER-V FEATURES ===\r\n\r\n");

                // Step 1: Disable hypervisor launch
                txtOutput.AppendText("[Step 1/5] Setting hypervisor launch type to OFF...\r\n");
                ExecuteBcdEdit("off", "Hyper-V Hypervisor Disabled");

                // Step 2: Disable all Hyper-V Windows features
                txtOutput.AppendText("[Step 2/5] Disabling Windows Features...\r\n");
                DisableWindowsFeatures();

                // Step 3: Disable Memory Integrity (Core Isolation)
                txtOutput.AppendText("[Step 3/5] Disabling Memory Integrity...\r\n");
                DisableMemoryIntegrity();

                // Step 4: Disable Device Guard / Credential Guard
                txtOutput.AppendText("[Step 4/5] Disabling Device Guard / Credential Guard...\r\n");
                DisableDeviceGuard();

                // Step 5: Disable LSA Isolation (Windows 11 24H2 fix)
                txtOutput.AppendText("[Step 5/5] Disabling LSA Isolation (24H2 fix)...\r\n");
                DisableLsaIso();

                txtOutput.AppendText("=== COMPLETE ===\r\n");
                txtOutput.AppendText("*** RESTART YOUR COMPUTER NOW FOR CHANGES TO TAKE EFFECT ***\r\n\r\n");

                MessageBox.Show(
                    "All Hyper-V features have been disabled!\n\n" +
                    "IMPORTANT: You MUST restart your computer now.\n\n" +
                    "After restart, VMware should work with hardware virtualization,\n" +
                    "including nested virtualization for Proxmox VMs.",
                    "Success - Restart Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                CheckCurrentStatus();
            }
        }

        private void BtnCheckStatus_Click(object sender, EventArgs e)
        {
            CheckCurrentStatus();
        }

        private void BtnRestart_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to restart the computer now?",
                "Confirm Restart",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                Process.Start("shutdown", "/r /t 0");
            }
        }

        private void ExecuteBcdEdit(string launchType, string modeName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = $"/set hypervisorlaunchtype {launchType}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        txtOutput.AppendText($"[SUCCESS] {modeName}\r\n");
                        txtOutput.AppendText($"Command: bcdedit /set hypervisorlaunchtype {launchType}\r\n");
                        txtOutput.AppendText($"{output}\r\n");
                        txtOutput.AppendText("*** RESTART REQUIRED FOR CHANGES TO TAKE EFFECT ***\r\n\r\n");

                        MessageBox.Show(
                            $"Successfully set to {modeName}!\n\nPlease restart your computer for the changes to take effect.",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        CheckCurrentStatus();
                    }
                    else
                    {
                        txtOutput.AppendText($"[ERROR] Failed to set {modeName}\r\n");
                        txtOutput.AppendText($"{error}\r\n\r\n");

                        MessageBox.Show(
                            $"Failed to change mode:\n{error}\n\nMake sure you're running as Administrator.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"[EXCEPTION] {ex.Message}\r\n\r\n");
                MessageBox.Show(
                    $"Error: {ex.Message}\n\nMake sure you're running as Administrator.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CheckCurrentStatus()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = "/enum",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (output.Contains("hypervisorlaunchtype") && output.Contains("Auto"))
                    {
                        lblStatus.Text = "Current Status: Hyper-V Mode (VMware nested virtualization disabled)";
                        lblStatus.ForeColor = Color.Green;
                    }
                    else if (output.Contains("hypervisorlaunchtype") && output.Contains("Off"))
                    {
                        lblStatus.Text = "Current Status: VMware Mode (Hyper-V disabled)";
                        lblStatus.ForeColor = Color.DarkRed;
                    }
                    else
                    {
                        lblStatus.Text = "Current Status: Unable to determine (may need to set manually)";
                        lblStatus.ForeColor = Color.Orange;
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Current Status: Error - {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void DisableWindowsFeatures()
        {
            txtOutput.AppendText("Disabling Hyper-V Windows Features...\r\n");

            string[] featuresToDisable = new string[]
            {
                "Microsoft-Hyper-V-All",
                "Microsoft-Hyper-V",
                "Microsoft-Hyper-V-Tools-All",
                "Microsoft-Hyper-V-Management-PowerShell",
                "Microsoft-Hyper-V-Hypervisor",
                "Microsoft-Hyper-V-Services",
                "Microsoft-Hyper-V-Management-Clients",
                "HypervisorPlatform",
                "VirtualMachinePlatform",
                "Containers",
                "Containers-DisposableClientVM"
            };

            foreach (string feature in featuresToDisable)
            {
                try
                {
                    txtOutput.AppendText($"  → Disabling {feature}...\r\n");
                    
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "dism.exe",
                        Arguments = $"/Online /Disable-Feature /FeatureName:{feature} /NoRestart",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(psi))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0 || output.Contains("completed successfully"))
                        {
                            txtOutput.AppendText($"    ✓ Success\r\n");
                        }
                        else if (output.Contains("The specified feature is not in the image") || 
                                 output.Contains("not enabled"))
                        {
                            txtOutput.AppendText($"    - Not installed/already disabled\r\n");
                        }
                        else
                        {
                            txtOutput.AppendText($"    ⚠ Warning: {process.ExitCode}\r\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    txtOutput.AppendText($"    ✗ Error: {ex.Message}\r\n");
                }
            }
            
            txtOutput.AppendText("\r\n");
        }

        private void DisableMemoryIntegrity()
        {
            txtOutput.AppendText("Disabling Memory Integrity (Core Isolation)...\r\n");

            try
            {
                // Disable Memory Integrity (HVCI)
                SetRegistryValue(
                    "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity",
                    "Enabled", "0", "Memory Integrity (HVCI)");

                // Disable Windows Hello VBS scenario (Windows 11 24H2)
                SetRegistryValue(
                    "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\WindowsHello",
                    "Enabled", "0", "Windows Hello VBS (24H2)");
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not disable Memory Integrity: {ex.Message}\r\n");
            }

            txtOutput.AppendText("\r\n");
        }

        private void DisableDeviceGuard()
        {
            txtOutput.AppendText("Disabling Device Guard / Credential Guard...\r\n");

            try
            {
                // Disable Virtualization Based Security
                SetRegistryValue(
                    "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard",
                    "EnableVirtualizationBasedSecurity", "0", "Virtualization Based Security");

                // Disable Credential Guard
                SetRegistryValue(
                    "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa",
                    "LsaCfgFlags", "0", "Credential Guard");
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not disable Device Guard: {ex.Message}\r\n");
            }

            txtOutput.AppendText("\r\n");
        }

        private void DisableLsaIso()
        {
            txtOutput.AppendText("Disabling LSA Isolation (Windows 11 24H2 fix)...\r\n");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = "/set {0cb3b571-2f2e-4343-a879-d86a476d7215} loadoptions DISABLE-LSA-ISO",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        txtOutput.AppendText("  ✓ LSA Isolation disabled via BCD\r\n");
                    }
                    else
                    {
                        // This is expected to fail if the entry doesn't exist (non-24H2 systems)
                        txtOutput.AppendText("  - LSA ISO entry not present (normal for non-24H2 systems)\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not configure LSA ISO: {ex.Message}\r\n");
            }

            txtOutput.AppendText("\r\n");
        }

        private void SetRegistryValue(string keyPath, string valueName, string value, string friendlyName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"add \"{keyPath}\" /v {valueName} /t REG_DWORD /d {value} /f",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        txtOutput.AppendText($"  ✓ {friendlyName} disabled\r\n");
                    }
                    else
                    {
                        txtOutput.AppendText($"  ⚠ {friendlyName}: could not set (may already be disabled)\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ✗ {friendlyName}: {ex.Message}\r\n");
            }
        }

        private int ReadRegistryDword(string keyPath, string valueName)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"query \"{keyPath}\" /v {valueName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string _ = process.StandardError.ReadToEnd();  // drain to prevent deadlock
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        return -1;

                    // Output format: "    ValueName    REG_DWORD    0x1"
                    foreach (string line in output.Split('\n'))
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith(valueName, StringComparison.OrdinalIgnoreCase))
                        {
                            string[] parts = trimmed.Split(new char[]{' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 3)
                            {
                                string hex = parts[parts.Length - 1].Replace("0x", "").Replace("0X", "");
                                if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int val))
                                    return val;
                            }
                        }
                    }
                }
            }
            catch { }
            return -1;
        }

        private void SaveState()
        {
            try
            {
                var state = new HypervisorState
                {
                    HvciEnabled = ReadRegistryDword(
                        "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity",
                        "Enabled"),
                    WindowsHelloVbsEnabled = ReadRegistryDword(
                        "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\WindowsHello",
                        "Enabled"),
                    VbsEnabled = ReadRegistryDword(
                        "HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard",
                        "EnableVirtualizationBasedSecurity"),
                    CredentialGuardEnabled = ReadRegistryDword(
                        "HKLM\\SYSTEM\\CurrentControlSet\\Control\\Lsa",
                        "LsaCfgFlags"),
                    LsaIsoWasPresent = LsaIsoEntryExists()
                };

                string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StateFilePath, json);
                txtOutput.AppendText($"  ✓ Current state saved to hypervisor-state.json\r\n");
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not save state: {ex.Message}\r\n");
            }
        }

        private HypervisorState LoadState()
        {
            try
            {
                if (File.Exists(StateFilePath))
                {
                    string json = File.ReadAllText(StateFilePath);
                    return JsonSerializer.Deserialize<HypervisorState>(json) ?? new HypervisorState();
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not load state file: {ex.Message}\r\n");
            }
            return new HypervisorState(); // defaults (-1) mean "was not configured — leave unchanged"
        }

        private bool LsaIsoEntryExists()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "bcdedit",
                    Arguments = "/enum {0cb3b571-2f2e-4343-a879-d86a476d7215}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string _ = process.StandardError.ReadToEnd();  // drain to prevent deadlock
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }

        private void BtnFullDiagnostics_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
            txtOutput.AppendText("=== FULL SYSTEM DIAGNOSTICS ===\r\n\r\n");

            // Check BCD settings
            txtOutput.AppendText("--- Boot Configuration (BCD) ---\r\n");
            RunCommand("bcdedit", "/enum {current}", "Checking hypervisor settings");

            // Check Windows Features
            txtOutput.AppendText("\r\n--- Hyper-V Related Features ---\r\n");
            RunCommand("dism.exe", "/Online /Get-Features /Format:Table | findstr /i \"Hyper-V Virtual Container\"", "Checking installed features");

            // Check Memory Integrity (HVCI)
            txtOutput.AppendText("\r\n--- Memory Integrity (Core Isolation / HVCI) ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name Enabled -ErrorAction SilentlyContinue\"", "Checking Memory Integrity");

            // Check Windows Hello VBS (24H2)
            txtOutput.AppendText("\r\n--- Windows Hello VBS (24H2) ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\WindowsHello' -Name Enabled -ErrorAction SilentlyContinue\"", "Checking Windows Hello VBS");

            // Check Device Guard / VBS
            txtOutput.AppendText("\r\n--- Device Guard / VBS ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard' -Name EnableVirtualizationBasedSecurity -ErrorAction SilentlyContinue\"", "Checking VBS");

            // Check Credential Guard
            txtOutput.AppendText("\r\n--- Credential Guard (LSA) ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Lsa' -Name LsaCfgFlags -ErrorAction SilentlyContinue\"", "Checking Credential Guard");

            // Check Device Guard CIM instance
            txtOutput.AppendText("\r\n--- Device Guard Status (CIM) ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\\Microsoft\\Windows\\DeviceGuard | Select-Object -Property VirtualizationBasedSecurityStatus, SecurityServicesRunning\"", "Checking Device Guard CIM");

            // Check CPU Virtualization
            txtOutput.AppendText("\r\n--- CPU Virtualization Support ---\r\n");
            RunCommand("systeminfo", "| findstr /i \"Hyper-V\"", "Checking virtualization support");

            txtOutput.AppendText("\r\n=== DIAGNOSTICS COMPLETE ===\r\n");
            txtOutput.AppendText("\r\nExpected values for VMware mode:\r\n");
            txtOutput.AppendText("  • hypervisorlaunchtype: Off\r\n");
            txtOutput.AppendText("  • Memory Integrity (Enabled): 0 or not present\r\n");
            txtOutput.AppendText("  • Windows Hello VBS (Enabled): 0 or not present\r\n");
            txtOutput.AppendText("  • EnableVirtualizationBasedSecurity: 0 or not present\r\n");
            txtOutput.AppendText("  • LsaCfgFlags: 0 or not present\r\n");
            txtOutput.AppendText("  • VirtualizationBasedSecurityStatus: 0 (Off)\r\n");
        }

        private void RunCommand(string fileName, string arguments, string description)
        {
            try
            {
                txtOutput.AppendText($"{description}...\r\n");
                
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output))
                    {
                        txtOutput.AppendText(output);
                    }
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        txtOutput.AppendText($"Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"Exception: {ex.Message}\r\n");
            }
            
            txtOutput.AppendText("\r\n");
        }
    }
}
