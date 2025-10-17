using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace HypervisorToggle
{
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
                "✓ Disable Hyper-V Windows Feature\n" +
                "✓ Disable Virtual Machine Platform\n" +
                "✓ Disable Windows Hypervisor Platform\n" +
                "✓ Disable Windows Sandbox\n" +
                "✓ Disable Containers\n\n" +
                "This ensures VMware Workstation will work with VT-x/AMD-V.\n\n" +
                "Continue?",
                "Confirm - Full Hyper-V Disable",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                txtOutput.AppendText("=== DISABLING ALL HYPER-V FEATURES ===\r\n\r\n");
                
                // Step 1: Disable hypervisor launch
                ExecuteBcdEdit("off", "Hyper-V Hypervisor Disabled");
                
                // Step 2: Disable all Hyper-V Windows features
                DisableWindowsFeatures();
                
                // Step 3: Disable Memory Integrity (Core Isolation)
                DisableMemoryIntegrity();
                
                txtOutput.AppendText("\r\n=== COMPLETE ===\r\n");
                txtOutput.AppendText("*** RESTART YOUR COMPUTER NOW FOR CHANGES TO TAKE EFFECT ***\r\n\r\n");
                
                MessageBox.Show(
                    "All Hyper-V features have been disabled!\n\n" +
                    "IMPORTANT: You MUST restart your computer now.\n\n" +
                    "After restart, VMware should work with hardware virtualization.",
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
            txtOutput.AppendText("Checking Memory Integrity (Core Isolation)...\r\n");
            
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name Enabled -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Enabled\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    if (output == "1")
                    {
                        txtOutput.AppendText("  Memory Integrity is ENABLED - attempting to disable...\r\n");
                        
                        // Disable via registry
                        ProcessStartInfo psiDisable = new ProcessStartInfo
                        {
                            FileName = "reg.exe",
                            Arguments = "add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v Enabled /t REG_DWORD /d 0 /f",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using (Process procDisable = Process.Start(psiDisable))
                        {
                            procDisable.WaitForExit();
                            if (procDisable.ExitCode == 0)
                            {
                                txtOutput.AppendText("  ✓ Memory Integrity disabled in registry\r\n");
                            }
                        }
                    }
                    else
                    {
                        txtOutput.AppendText("  Memory Integrity is already disabled or not configured\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                txtOutput.AppendText($"  ⚠ Could not check Memory Integrity: {ex.Message}\r\n");
            }
            
            txtOutput.AppendText("\r\n");
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

            // Check Memory Integrity
            txtOutput.AppendText("\r\n--- Memory Integrity (Core Isolation) ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-ItemProperty -Path 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity' -Name Enabled -ErrorAction SilentlyContinue\"", "Checking Memory Integrity");

            // Check Device Guard
            txtOutput.AppendText("\r\n--- Device Guard / Credential Guard ---\r\n");
            RunCommand("powershell.exe", "-Command \"Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\\Microsoft\\Windows\\DeviceGuard\"", "Checking Device Guard");

            // Check CPU Virtualization
            txtOutput.AppendText("\r\n--- CPU Virtualization Support ---\r\n");
            RunCommand("systeminfo", "", "Checking system info for virtualization");

            txtOutput.AppendText("\r\n=== DIAGNOSTICS COMPLETE ===\r\n");
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
